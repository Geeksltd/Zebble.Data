namespace Zebble.Data
{
    using System;
    using System.Linq;
    using System.Reflection;

    public class Setup
    {
        public static void RegisterDataProvider(Type factoryType, Assembly assembly = null)
        {
            Database.RegisterDataProviderFactory(new DataProviderFactoryInfo
            {
                Assembly = assembly ?? factoryType.GetAssembly(),
                ConnectionString = DataAccessor.GetCurrentConnectionString(),
                ProviderFactoryType = factoryType.AssemblyQualifiedName
            });
        }

        public static void CreateDatabase()
        {
            var dbFile = Device.IO.File(Config.Get("Database.File"));
            if (dbFile.Exists()) dbFile.Delete();

            var tablesDirectory = Device.IO.Directory("tables");
            if (!tablesDirectory.Exists()) return;

            var scriptsToRun = tablesDirectory.GetFiles()
                .OrderByDescending(s => s.Name.EndsWith("create.sql"))
                .ThenByDescending(s => s.Name.EndsWith("data.sql"))
                .ToList();

            foreach (var script in scriptsToRun)
            {
                var code = script.ReadAllText();
                try
                {
                    DataAccessor.ExecuteNonQuery(code);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not run the SQL file " + script, ex);
                }
            }
        }
    }
}