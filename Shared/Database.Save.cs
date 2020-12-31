namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Olive;

    partial class Database
    {
        /// <summary>
        /// Inserts or updates an object in the database.
        /// </summary>
        public static T Save<T>(T entity) where T : IEntity
        {
            Save(entity as IEntity, SaveBehaviour.Default);
            return entity;
        }

        /// <summary>
        /// Inserts or updates an object in the database.
        /// </summary>        
        public static void Save(IEntity entity, SaveBehaviour behaviour)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var mode = entity.IsNew ? SaveMode.Insert : SaveMode.Update;

            var asEntity = entity as Entity;
            if (mode == SaveMode.Update && (asEntity._ClonedFrom?.IsStale == true) && AnyOpenTransaction())
            {
                throw new InvalidOperationException("This " + entity.GetType().Name + " instance in memory is out-of-date. " +
                    "A clone of it is already updated in the transaction. It is not allowed to update the same instance multiple times in a transaction, because then the earlier updates would be overwriten by the older state of the instance in memory. \r\n\r\n" +
                    @"BAD: 
Database.Update(myObject, x=> x.P1 = ...); // Note: this could also be nested inside another method that's called here instead.
Database.Update(myObject, x=> x.P2 = ...);

GOOD: 
Database.Update(myObject, x=> x.P1 = ...);
myObject = Database.Reload(myObject);
Database.Update(myObject, x=> x.P2 = ...);");
            }

            if (EntityManager.IsImmutable(entity))
                throw new ArgumentException("An immutable record must be cloned before any modifications can be applied on it. Type=" +
                    entity.GetType().FullName + ", Id=" + entity.GetId() + ".");

            var dataProvider = GetProvider(entity);

            if (!IsSet(behaviour, SaveBehaviour.BypassValidation))
            {
                EntityManager.RaiseOnValidating(entity as Entity, EventArgs.Empty);

                entity.Validate();
            }
            else if (!dataProvider.SupportValidationBypassing())
            {
                throw new ArgumentException(dataProvider.GetType().Name + " does not support bypassing validation.");
            }

            #region Raise saving event

            if (!IsSet(behaviour, SaveBehaviour.BypassSaving))
            {
                var savingArgs = new System.ComponentModel.CancelEventArgs();
                EntityManager.RaiseOnSaving(entity, savingArgs);

                if (savingArgs.Cancel)
                {
                    Cache.Current.Remove(entity);
                    return;
                }
            }
            #endregion

            dataProvider.Save(entity);

            if (mode == SaveMode.Update && asEntity?._ClonedFrom != null && AnyOpenTransaction())
            {
                asEntity._ClonedFrom.IsStale = true;
                asEntity.IsStale = false;
            }

            if (mode == SaveMode.Insert)
                EntityManager.SetSaved(entity);

            Cache.Current.Remove(entity);

            if (DbTransactionScope.Root != null)
                DbTransactionScope.Root.OnTransactionCompleted(() => Cache.Current.Remove(entity));

            OnUpdated(new EventArgs<IEntity>(entity));

            if (!IsSet(behaviour, SaveBehaviour.BypassSaved))
            {
                EntityManager.RaiseOnSaved(entity, new SaveEventArgs(mode));
            }

            // OnSaved event handler might have read the object again and put it in the cache, which would
            // create invalid CachedReference objects.
            Cache.Current.Remove(entity);
        }

        /// <summary>
        /// Saves the specified records in the data repository.
        /// The operation will run in a Transaction.
        /// </summary>
        public static IEnumerable<T> Save<T>(List<T> records) where T : IEntity => Save(records as IEnumerable<T>);

        /* ===================== Update ========================*/

        /// <summary>
        /// Runs an update command on a list of given objects and persists the updated objects in database.
        /// It returns the updated instances.
        /// </summary>
        /// <param name="items">The objects to be updated in database.</param>
        /// <param name="action">Update action. For example: o=>o.Property = "Value"</param>
        public static List<T> Update<T>(IEnumerable<T> items, Action<T> action) where T : IEntity
        {
            return Update<T>(items, action, SaveBehaviour.Default);
        }

        /// <summary>
        /// Runs an update command on a list of given objects and persists the updated objects in database.
        /// It returns the updated instances.
        /// </summary>
        /// <param name="items">The objects to be updated in database.</param>
        /// <param name="action">Update action. For example: o=>o.Property = "Value"</param>
        public static List<T> Update<T>(IEnumerable<T> items, Action<T> action, SaveBehaviour behaviour) where T : IEntity
        {
            var result = new List<T>();

            EnlistOrCreateTransaction(() =>
            {
                foreach (var item in items)
                    result.Add(Update(item, action, behaviour));
            });

            return result;
        }

        /// <summary>
        /// Runs an update command on a given object's clone and persists the updated object in database. It returns the updated instance.
        /// </summary>
        /// <param name="item">The object to be updated in database.</param>
        /// <param name="action">Update action. For example: o=>o.Property = "Value"</param>
        public static T Update<T>(T item, Action<T> action) where T : IEntity
        {
            return Update<T>(item, action, SaveBehaviour.Default);
        }

        /// <summary>
        /// Runs an update command on a given object's clone and persists the updated object in database. It returns the updated instance.
        /// </summary>
        /// <param name="item">The object to be updated in database.</param>
        /// <param name="action">Update action. For example: o=>o.Property = "Value"</param>
        public static T Update<T>(T item, Action<T> action, SaveBehaviour behaviour) where T : IEntity
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (item.IsNew)
                throw new ArgumentException("New instances cannot be updated using the Update method.");

            if (!(item is Entity))
                throw new ArgumentException("Database.Update() method accepts a type inheriting from {0}. So {1} is not supported.".FormatWith(typeof(Entity).FullName, typeof(T).FullName));

            if ((item as Entity)._ClonedFrom?.IsStale == true && AnyOpenTransaction())
            {
                // No need for an error. We can just get the fresh version here.
                item = Reload(item);
            }

            if (EntityManager.IsImmutable(item as Entity))
            {
                var clone = (T)((IEntity)item).Clone();

                action(clone);

                Save(clone as Entity, behaviour);

                if (!AnyOpenTransaction())
                {
                    action(item);
                }

                return clone;
            }
            else
            {
                action(item);
                Save(item, behaviour);

                return item;
            }
        }

        /// <summary>
        /// Inserts the specified objects in bulk. None of the object events will be triggered.
        /// </summary>
        public static void BulkInsert(Entity[] objects, int batchSize = 10, bool bypassValidation = false)
        {
            if (!bypassValidation)
                objects.Do(o => o.Validate());

            var objectTypes = objects.GroupBy(o => o.GetType()).ToArray();

            try
            {
                foreach (var group in objectTypes)
                {
                    var records = group.ToArray();
                    GetProvider(group.Key).BulkInsert(records, batchSize);
                }

                foreach (var type in objectTypes)
                {
                    Cache.Current.Remove(type.Key);
                }
            }
            catch
            {
                Refresh();
                throw;
            }
        }

        /// <summary>
        /// Updates the specified objects in bulk. None of the object events will be triggered.
        /// </summary>
        public static void BulkUpdate(Entity[] objects, int batchSize = 10, bool bypassValidation = false)
        {
            if (!bypassValidation)
                objects.Do(o => o.Validate());

            var objectTypes = objects.GroupBy(o => o.GetType()).ToArray();

            try
            {
                foreach (var group in objectTypes)
                {
                    var records = group.ToArray();
                    GetProvider(group.Key).BulkUpdate(records, batchSize);
                }

                foreach (var type in objectTypes)
                {
                    Cache.Current.Remove(type.Key);
                }
            }
            catch
            {
                Refresh();
                throw;
            }
        }
    }
}