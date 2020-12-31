using System;
using System.Collections.Generic;
using Olive;

namespace Zebble.Data
{
    partial class Database
    {
        /// <summary>
        /// Saves the specified records in the data repository.
        /// The operation will run in a Transaction.
        /// </summary>
        public static IEnumerable<T> Save<T>(T[] records) where T : IEntity
        {
            return Save(records as IEnumerable<T>);
        }

        /// <summary>
        /// Saves the specified records in the data repository.
        /// The operation will run in a Transaction.
        /// </summary>
        public static IEnumerable<T> Save<T>(IEnumerable<T> records) where T : IEntity
        {
            return Save<T>(records, SaveBehaviour.Default);
        }

        /// <summary>
        /// Saves the specified records in the data repository.
        /// The operation will run in a Transaction.
        /// </summary>
        public static IEnumerable<T> Save<T>(IEnumerable<T> records, SaveBehaviour behaviour) where T : IEntity
        {
            if (records == null)
                throw new ArgumentNullException(nameof(records));

            if (records.None()) return records;

            EnlistOrCreateTransaction(() =>
            {
                foreach (var record in records)
                    Save(record as Entity, behaviour);
            });

            return records;
        }
    }
}