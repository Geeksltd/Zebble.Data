namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Zebble.Data;
    using Olive;

    partial class Database
    {
        /// <summary>
        /// Deletes the specified record from the data repository.
        /// </summary>
        public static void Delete(IEntity instance)
        {
            Delete(instance, DeleteBehaviour.Default);
        }

        static void DoDelete(Entity entity, DeleteBehaviour behaviour)
        {
            // Raise deleting event
            if (!IsSet(behaviour, DeleteBehaviour.BypassDeleting))
            {
                var deletingArgs = new System.ComponentModel.CancelEventArgs();
                EntityManager.RaiseOnDeleting(entity, deletingArgs);

                if (deletingArgs.Cancel)
                {
                    Cache.Current.Remove(entity);
                    return;
                }
            }

            GetProvider(entity).Delete(entity);
        }

        /// <summary>
        /// Deletes the specified record from the data repository.
        /// </summary>
        public static void Delete(IEntity instance, DeleteBehaviour behaviour)
        {
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var entity = instance as Entity;

            if (entity == null)
                throw new ArgumentException("The type of the specified object to delete does not inherit from {0} class.".FormatWith(typeof(Entity).FullName));

            EnlistOrCreateTransaction(() => DoDelete(entity, behaviour));

            Cache.Current.Remove(entity);

            if (DbTransactionScope.Root != null)
                DbTransactionScope.Root.OnTransactionCompleted(() => Cache.Current.Remove(entity));

            OnUpdated(new EventArgs<IEntity>(entity));

            if (!IsSet(behaviour, DeleteBehaviour.BypassDeleted))
            {
                EntityManager.RaiseOnDeleted(entity);
            }
        }

        /// <summary>
        /// Deletes the specified instances from the data repository.        
        /// The operation will be done in a transaction.
        /// </summary>
        public static void Delete<T>(IEnumerable<T> instances) where T : IEntity
        {
            if (instances == null)
                throw new ArgumentNullException(nameof(instances));

            if (instances.None()) return;

            EnlistOrCreateTransaction(() =>
            {
                foreach (T obj in instances.ToArray()) Delete(obj);
            });
        }

        /// <summary>
        /// Deletes all objects of the specified type.
        /// </summary>
        public static void DeleteAll<T>() where T : IEntity => Delete(GetList<T>());

        /// <summary>
        /// Deletes all objects of the specified type matching the given criteria.
        /// </summary>
        public static void DeleteAll<T>(Expression<Func<T, bool>> criteria) where T : IEntity => Delete(GetList<T>(criteria));

        /// <summary>
        /// Updates all records in the database with the specified change.
        /// </summary>
        public static void UpdateAll<T>(Action<T> change) where T : IEntity
        {
            var records = GetList<T>();
            Update(records, change);
        }
    }
}