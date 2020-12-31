namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using Olive;

    #region Standard Providers


    #endregion

    /// <summary>
    /// Provides a DataProvider for accessing data from the database using ADO.NET.
    /// </summary>
    public abstract class DataProvider<TConnection, TDataParameter> : IDataProvider
        where TConnection : IDbConnection, new()
        where TDataParameter : IDbDataParameter, new()
    {
        static string[] ExtractIdsSeparator = new[] { "</Id>", "<Id>" };
        string ConnectionString = DataAccessor.GetCurrentConnectionString();
        protected DataProvider() { }

        public virtual void BulkInsert(IEntity[] entities, int batchSize) => entities.Do(i => Database.Save(i, SaveBehaviour.BypassAll));

        public void BulkUpdate(IEntity[] entities, int batchSize) => entities.Do(i => Database.Save(i, SaveBehaviour.BypassAll));

        public abstract int Count(Type type, IEnumerable<ICriterion> criteria, params QueryOption[] options);

        public static List<string> ExtractIds(string idsXml)
        {
            return idsXml.Split(ExtractIdsSeparator, StringSplitOptions.RemoveEmptyEntries).ToList();
        }

        public bool SupportValidationBypassing() => true;

        /// <summary>
        /// Executes the specified command text as nonquery.
        /// </summary>
        public int ExecuteNonQuery(string command) => ExecuteNonQuery(command, CommandType.Text);

        /// <summary>
        /// Executes the specified command text as nonquery.
        /// </summary>
        public int ExecuteNonQuery(string command, CommandType commandType, params IDataParameter[] @params)
        {
            using (new DatabaseContext(ConnectionString))
            {
                return DataAccessor<TConnection>.ExecuteNonQuery(command, commandType, @params);
            }
        }

        /// <summary>
        /// Executes the specified command text as nonquery.
        /// </summary>
        public int ExecuteNonQuery(CommandType commandType, List<KeyValuePair<string, IDataParameter[]>> commands)
        {
            using (new DatabaseContext(ConnectionString))
            {
                return DataAccessor<TConnection>.ExecuteNonQuery(commandType, commands);
            }
        }

        /// <summary>
        /// Executes the specified command text against the database connection of the context and builds an IDataReader.  Make sure you close the data reader after finishing the work.
        /// </summary>
        public IDataReader ExecuteReader(string command, CommandType commandType, params IDataParameter[] @params)
        {
            using (new DatabaseContext(ConnectionString))
            {
                return DataAccessor<TConnection>.ExecuteReader(command, commandType, @params);
            }
        }

        /// <summary>
        /// Executes the specified command text against the database connection of the context and returns the single value.
        /// </summary>
        public object ExecuteScalar(string command) => ExecuteScalar(command, CommandType.Text);

        /// <summary>
        /// Executes the specified command text against the database connection of the context and returns the single value.
        /// </summary>
        public object ExecuteScalar(string command, CommandType commandType, params IDataParameter[] @params)
        {
            using (new DatabaseContext(ConnectionString))
            {
                return DataAccessor<TConnection>.ExecuteScalar(command, commandType, @params);
            }
        }

        public IEnumerable<object> GetIdsList(Type type, IEnumerable<ICriterion> criteria) => GetList(type, criteria).Select(i => i.GetId());

        public IDictionary<string, Tuple<string, string>> FindUpdatedValues(IEntity original, IEntity updated)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            var result = new Dictionary<string, Tuple<string, string>>();
            var type = original.GetType();

            var propertyNames = type.GetProperties().Except(x => x.PropertyType.IsA<IEntity>()).Select(p => p.Name).Distinct().Trim().ToArray();

            PropertyInfo getProperty(string name)
            {
                return type.GetProperties().Except(p => p.IsSpecialName || p.GetGetMethod().IsStatic)
                       .Where(p => p.GetSetMethod() != null && p.GetGetMethod().IsPublic)
                       .OrderByDescending(x => x.DeclaringType == type).FirstOrDefault(p => p.Name == name);
            }

            var dataProperties = propertyNames.Select(getProperty).ExceptNull().Except(p => CalculatedAttribute.IsCalculated(p)).ToArray();

            foreach (var p in dataProperties)
            {
                var propertyType = p.PropertyType;
                // Get the original value:
                string originalValue, updatedValue = null;
                if (propertyType == typeof(IList<Guid>))
                {
                    try
                    {
                        originalValue = (p.GetValue(original) as IList<Guid>).ToString(",");
                        if (updated != null)
                            updatedValue = (p.GetValue(updated) as IList<Guid>).ToString(",");
                    }
                    catch
                    {
                        // No logging is needed
                        continue;
                    }
                }
                else
                {
                    try
                    {
                        originalValue = $"{p.GetValue(original)}";
                        if (updated != null)
                            updatedValue = $"{p.GetValue(updated)}";
                    }
                    catch
                    {
                        // No logging is needed
                        continue;
                    }
                }

                if (updated == null || originalValue != updatedValue)
                    result.Add(p.Name, new Tuple<string, string>(originalValue, updatedValue));
            }

            return result;
        }

        /// <summary>
        /// Creates a data parameter with the specified name and value.
        /// </summary>
        public IDataParameter CreateParameter(string parameterName, object value)
        {
            if (value == null) value = DBNull.Value;

            if (value is Guid) value = value.ToString();
            if (value is Document) value = (value as Document).FileName;

            return new TDataParameter { ParameterName = parameterName.Remove(" "), Value = value };
        }

        /// <summary>
        /// Creates a data parameter with the specified name and value and type.
        /// </summary>
        public IDataParameter CreateParameter(string parameterName, object value, DbType columnType)
        {
            if (value == null)
                value = DBNull.Value;
            return new TDataParameter { ParameterName = parameterName.Remove(" "), Value = value, DbType = columnType };
        }

        /// <summary>
        /// Deletes the specified record.
        /// </summary>
        public abstract void Delete(IEntity record);

        /// <summary>
        /// Gets the specified record by its type and ID.
        /// </summary>
        public abstract IEntity Get(Type type, object objectID);

        /// <summary>
        /// Gets the list of specified records.
        /// </summary>
        public abstract IEnumerable<IEntity> GetList(Type type, IEnumerable<ICriterion> criteria, params QueryOption[] options);

        /// <summary>
        /// Reads the many to many relation.
        /// </summary>
        public abstract IEnumerable<string> ReadManyToManyRelation(IEntity instance, string property);

        /// <summary>
        /// Saves the specified record.
        /// </summary>
        public abstract void Save(IEntity record);

        /// <summary>
        /// Generates data provider specific parameters for the specified data items.
        /// </summary>
        public IDataParameter[] GenerateParameters(Dictionary<string, object> parametersData) => parametersData.Select(GenerateParameter).ToArray();

        /// <summary>
        /// Generates a data provider specific parameter for the specified data.
        /// </summary>
        public IDataParameter GenerateParameter(KeyValuePair<string, object> data)
        {
            return new TDataParameter { Value = data.Value, ParameterName = data.Key.Remove(" ") };
        }
    }
}