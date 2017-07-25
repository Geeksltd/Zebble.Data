namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;

    public interface IDataProvider
    {
        IEntity Get(Type type, object objectID);
        void Save(IEntity record);
        void Delete(IEntity record);
        IEnumerable<object> GetIdsList(Type type, IEnumerable<ICriterion> criteria);

        IEnumerable<IEntity> GetList(Type type, IEnumerable<ICriterion> criteria, params QueryOption[] options);
        int Count(Type type, IEnumerable<ICriterion> conditions, params QueryOption[] options);

        /// <summary>
        /// Reads the many to many relation and returns the IDs of the associated objects.
        /// </summary>
        IEnumerable<string> ReadManyToManyRelation(IEntity instance, string property);

        IDictionary<string, Tuple<string, string>> FindUpdatedValues(IEntity original, IEntity updated);

        int ExecuteNonQuery(string command);
        object ExecuteScalar(string command);

        bool SupportValidationBypassing();

        void BulkInsert(IEntity[] entities, int batchSize);
        void BulkUpdate(IEntity[] entities, int batchSize);
    }
}