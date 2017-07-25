namespace Zebble.Data
{
    using System;
    using System.ComponentModel;
    using System.Reflection;

    /// <summary>
    /// Provides services for Entity objects.
    /// </summary>
    public static class EntityManager
    {
        /// <summary>
        /// Determines whether the specified record is immutable, or closed for changes.        
        /// An object marked as immutable is shared in the application cache. Therefore it must not be changed.        
        /// </summary>
        public static bool IsImmutable(IEntity entity)
        {
            var item = entity as Entity;
            if (item == null)
                throw new ArgumentNullException("entity must be a non-null instance inheriting from Entity.");
            return item.IsImmutable && !entity.IsNew;
        }

        /// <summary>
        /// Marks the specified object as immutable.
        /// </summary>
        public static void MarkImmutable(IEntity record)
        {
            if (record == null)
                throw new ArgumentNullException("entity");
            (record as Entity).IsImmutable = true;
        }

        #region Entity static events
        /// <summary>
        /// This event is raised for the whole Entity type before "any" object is saved in the database.
        /// You can handle this to provide global functionality/event handling scenarios.
        /// </summary>
        public static event EventHandler<CancelEventArgs> InstanceSaving;
        /// <summary>
        /// This event is raised for the whole Entity type after "any" object is saved in the database.
        /// You can handle this to provide global functionality/event handling scenarios.
        /// </summary>
        public static event EventHandler<SaveEventArgs> InstanceSaved;
        /// <summary>
        /// This event is raised for the whole Entity type before "any" object is deleted from the database.
        /// You can handle this to provide global functionality/event handling scenarios.
        /// </summary>
        public static event EventHandler<CancelEventArgs> InstanceDeleting;
        /// <summary>
        /// This event is raised for the whole Entity type before "any" object is validated.
        /// You can handle this to provide global functionality/event handling scenarios.
        /// This will be called as the first line of the base Entity's OnValidating method.
        /// </summary>
        public static event EventHandler<EventArgs> InstanceValidating;
        /// <summary>
        /// This event is raised for the whole Entity type after "any" object is deleted from the database.
        /// You can handle this to provide global functionality/event handling scenarios.
        /// </summary>
        public static event EventHandler<EventArgs> InstanceDeleted;
        #endregion
        #region Raise events
        internal static void RaiseStaticOnSaved(IEntity record, SaveEventArgs args)
        {
            InstanceSaved?.Invoke(record, args);
        }

        internal static void RaiseStaticOnDeleted(IEntity record, EventArgs args)
        {
            InstanceDeleted?.Invoke(record, args);
        }

        public static void RaiseOnDeleting(IEntity record, CancelEventArgs args)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            InstanceDeleting?.Invoke(record, args);
            if (args.Cancel)
                return;
            (record as Entity).OnDeleting(args);
        }

        public static void RaiseOnValidating(IEntity record, EventArgs args)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            InstanceValidating?.Invoke(record, args);
            (record as Entity).OnValidating(args);
        }

        public static void RaiseOnDeleted(IEntity record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            (record as Entity).OnDeleted(EventArgs.Empty);
        }

        public static void RaiseOnLoaded(IEntity record)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            (record as Entity).OnLoaded(EventArgs.Empty);
        }

        public static void RaiseOnSaving(IEntity record, CancelEventArgs e)
        {
            if (record == null) throw new ArgumentNullException(nameof(record));

            InstanceSaving?.Invoke(record, e);
            if (e.Cancel) return;
            (record as Entity).OnSaving(e);
        }

        public static void RaiseOnSaved(IEntity record, SaveEventArgs e)
        {
            if (record == null)
                throw new ArgumentNullException(nameof(record));
            (record as Entity).OnSaved(e);
        }

        #endregion
        /// <summary>
        /// Sets the state of an entity instance to saved.
        /// </summary>
        public static void SetSaved(IEntity entity, bool saved = true)
        {
            (entity as Entity).IsNew = !saved;
            entity.GetType().GetProperty("OriginalId").SetValue(entity, entity.GetId());
        }

        /// <summary>
        /// Creates a new clone of an entity. This will work in a polymorphic way.
        /// </summary>        
        public static T CloneAsNew<T>(T entity) where T : Entity, ICloneable => CloneAsNew<T>(entity, null);

        /// <summary>
        /// Creates a new clone of an entity. This will work in a polymorphic way.
        /// </summary>        
        public static T CloneAsNew<T>(T entity, Action<T> changes) where T : Entity, ICloneable
        {
            var result = (T)entity.Clone();
            result.IsNew = true;
            if (result is GuidEntity)
                (result as GuidEntity).ID = GuidEntity.NewGuidGenerator(result.GetType());
            if (result is IntEntity)
                (result as IntEntity).ID = IntEntity.NewIdGenerator(result.GetType());
            // Setting the value of AutoNumber properties to zero
            foreach (var propertyInfo in result.GetType().GetProperties())
            {
                if (AutoNumberAttribute.IsAutoNumber(propertyInfo))
                {
                    propertyInfo.SetValue(result, 0);
                }
            }

            result.Initialize();
            // Re attach Documents:
            changes?.Invoke(result);
            return result;
        }

        /// <summary>
        /// Sets the ID of an object explicitly.
        /// </summary>
        public static void RestsetOriginalId<T>(IEntity<T> entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            entity.GetType().GetProperty("OriginalId").SetValue(entity, entity.ID);
        }

        public static void SetSaved<T>(IEntity<T> entity, T id)
        {
            entity.GetType().GetProperty("IsNew").SetValue(entity, false);
            entity.ID = id;
            RestsetOriginalId(entity);
        }

        /// <summary>
        /// Read the value of a specified property from a specified object.
        /// </summary>
        public static object ReadProperty(object @object, string propertyName)
        {
            if (@object == null)
                throw new ArgumentNullException("@object");
            var property = FindProperty(@object.GetType(), propertyName);
            try
            {
                return property.GetValue(@object, null);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read the value of the property " + propertyName + " from the given " + @object.GetType().FullName + " object.", ex);
            }
        }

        public static System.Reflection.PropertyInfo FindProperty(Type type, string propertyName)
        {
            if (propertyName.LacksValue())
                throw new ArgumentNullException(nameof(propertyName));
            var result = type.GetProperty(propertyName);
            if (result == null) // Try inherited properties.
                result = type.GetProperty(propertyName);
            if (result == null)
                throw new ArgumentException(type + " does not have a property named " + propertyName);
            return result;
        }

        public static void WriteProperty(object @object, string propertyName, object value)
        {
            if (@object == null)
                throw new ArgumentNullException("@object");
            var property = FindProperty(@object.GetType(), propertyName);
            try
            {
                property.SetValue(@object, value, null);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Could not set the value of the property " + propertyName + " from the given " + @object.GetType().FullName + " object.", ex);
            }
        }
    }
}