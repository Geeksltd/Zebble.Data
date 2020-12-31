namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Zebble;
    using Config = Zebble.Config;
    using Olive;

    /// <summary>
    /// ADO.NET Facade for submitting single method commands.
    /// </summary>
    public class DataAccessor<TConnection> where TConnection : IDbConnection, new()
    {
        internal static IDbConnection CreateActualConnection() => CreateConnection();

        /// <summary>
        /// Creates a connection object.
        /// </summary>
        public static TConnection CreateConnection(string connectionString = null)
        {
            if (connectionString.IsEmpty()) connectionString = GetCurrentConnectionString();

            return new TConnection { ConnectionString = connectionString };
        }

        public static TConnection GetOrCreateCurrentConnection()
        {
            var result = DbTransactionScope.Root?.GetDbConnection();
            if (result != null) return (TConnection)result;
            else return CreateConnection();
        }

        public static string GetCurrentConnectionString()
        {
            string result;

            if (DatabaseContext.Current != null) result = DatabaseContext.Current.ConnectionString;
            else result = "Data Source=" + Device.IO.File(Config.Get("Database.File"));

            if (result.IsEmpty())
                throw new Exception("No 'AppDatabase.ConnectionString' connection string is specified in the application config file.");

            return result;
        }

        static IDbCommand CreateCommand(CommandType type, string commandText, params IDataParameter[] @params)
        {
            return CreateCommand(type, commandText, default(TConnection), @params);
        }

        static IDbCommand CreateCommand(CommandType type, string commandText, TConnection connection, params IDataParameter[] @params)
        {
            if (connection == null) connection = GetOrCreateCurrentConnection();

            if (connection.State != ConnectionState.Open) connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.CommandType = type;

            command.Transaction = DbTransactionScope.Root?.GetDbTransaction() ?? command.Transaction;

            command.CommandTimeout = DatabaseContext.Current?.CommandTimeout ?? (Config.TryGet<int?>("Sql.Command.TimeOut")) ?? command.CommandTimeout;

            foreach (var param in @params) command.Parameters.Add(param);

            return command;
        }

        /// <summary>
        /// Executes the specified command text as nonquery.
        /// </summary>
        public static int ExecuteNonQuery(string commandText) => ExecuteNonQuery(commandText, CommandType.Text);

        /// <summary>
        /// Executes the specified command text as nonquery.
        /// </summary>
        public static int ExecuteNonQuery(string command, CommandType commandType, params IDataParameter[] @params)
        {
            var dbCommand = CreateCommand(commandType, command, @params);

            try
            {
                return dbCommand.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                var error = new Exception("Error in running Non-Query SQL command.", ex).AddData("Command", command)
                    .AddData("Parameters", @params.Get(l => l.Select(p => p.ParameterName + "=" + p.Value).ToString(" | ")))
                    .AddData("ConnectionString", dbCommand.Connection.ConnectionString);

                Device.Log.Error(error);
                throw error;
            }
            finally
            {
                dbCommand.Parameters.Clear();
                CloseConnection(dbCommand.Connection);
            }
        }

        static void CloseConnection(IDbConnection connection)
        {
            if (DbTransactionScope.Root == null)
            {
                if (connection.State != ConnectionState.Closed)
                    connection.Close();
            }
        }

        /// <summary>
        /// Executes the specified command text as nonquery.
        /// </summary>
        public static int ExecuteNonQuery(CommandType commandType, List<KeyValuePair<string, IDataParameter[]>> commands)
        {
            var connection = GetOrCreateCurrentConnection();
            var result = 0;

            try
            {
                foreach (var c in commands)
                {
                    IDbCommand dbCommand = null;
                    try
                    {
                        dbCommand = CreateCommand(commandType, c.Key, connection, c.Value);
                        result += dbCommand.ExecuteNonQuery();
                    }
                    catch (Exception ex)
                    {
                        var error = new Exception("Error in executing SQL command.", ex).AddData("Command", c.Key)
                            .AddData("Parameters", c.Value.Get(l => l.Select(p => p.ParameterName + "=" + p.Value).ToString(" | ")));

                        Device.Log.Error(error);
                        throw error;
                    }
                    finally
                    {
                        dbCommand?.Parameters.Clear();
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception("Error in running Non-Query SQL commands.", ex).AddData("ConnectionString", connection.ConnectionString);
            }
            finally
            {
                CloseConnection(connection);
            }
        }

        /// <summary>
        /// Executes the specified command text against the database connection of the context and builds an IDataReader.
        /// Make sure you close the data reader after finishing the work.
        /// </summary>
        public static IDataReader ExecuteReader(string command, CommandType commandType, params IDataParameter[] @params)
        {
            var dbCommand = CreateCommand(commandType, command, @params);

            try
            {
                if (DbTransactionScope.Root != null) return dbCommand.ExecuteReader();
                else return dbCommand.ExecuteReader(CommandBehavior.CloseConnection);
            }
            catch (Exception ex)
            {
                var error = new Exception("Error in running SQL Query.", ex).AddData("Command", command)
                    .AddData("Parameters", @params.Get(l => l.Select(p => p.ParameterName + "=" + p.Value).ToString(" | ")))
                    .AddData("ConnectionString", dbCommand.Connection.ConnectionString);

                Device.Log.Error(error);
                throw error;
            }
            finally
            {
                dbCommand.Parameters.Clear();
            }
        }

        /// <summary>
        /// Executes the specified command text against the database connection of the context and returns the single value of the type specified.
        /// </summary>
        public static T ExecuteScalar<T>(string commandText) => (T)ExecuteScalar(commandText);

        /// <summary>
        /// Executes the specified command text against the database connection of the context and returns the single value.
        /// </summary>
        public static object ExecuteScalar(string commandText) => ExecuteScalar(commandText, CommandType.Text);

        /// <summary>
        /// Executes the specified command text against the database connection of the context and returns the single value.
        /// </summary>
        public static object ExecuteScalar(string command, CommandType commandType, params IDataParameter[] @params)
        {
            var dbCommand = CreateCommand(commandType, command, @params);

            try
            {
                return dbCommand.ExecuteScalar();
            }
            catch (Exception ex)
            {
                var error = new Exception("Error in running Scalar SQL Command.", ex).AddData("Command", command)
                    .AddData("Parameters", @params.Get(l => l.Select(p => p.ParameterName + "=" + p.Value).ToString(" | ")))
                    .AddData("ConnectionString", dbCommand.Connection.ConnectionString);

                Device.Log.Error(error);
                throw error;
            }
            finally
            {
                dbCommand.Parameters.Clear();

                CloseConnection(dbCommand.Connection);
            }
        }
    }
}