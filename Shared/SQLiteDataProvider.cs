namespace Zebble.Data
{
#if IOS || ANDROID
    using Mono.Data.Sqlite;
    public abstract class SQLiteDataProvider : DataProvider<SqliteConnection, SqliteParameter> { }
    public class DataAccessor : DataAccessor<SqliteConnection> { }
#endif
#if UWP
    using Microsoft.Data.Sqlite;
    public abstract class SQLiteDataProvider : DataProvider<SqliteConnection, SqliteParameter> { }
    public class DataAccessor : DataAccessor<SqliteConnection> { }
#endif
}