namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    partial class Database
    {
        static object DataProviderSyncLock = new object();

        static Database()
        {
            AssemblyProviderFactories = new Dictionary<Assembly, IDataProviderFactory>();
            TypeProviderFactories = new Dictionary<Type, IDataProviderFactory>();

            // TODO: Load...
        }

        /// <summary>
        /// It's raised when any record is saved or deleted in the system.
        /// </summary>
        public static event EventHandler<EventArgs<IEntity>> Updated;
        static void OnUpdated(EventArgs<IEntity> e) => Updated?.Invoke(e.Data, e);

        public static void RegisterDataProviderFactory(DataProviderFactoryInfo factoryInfo)
        {
            if (factoryInfo == null) throw new ArgumentNullException(nameof(factoryInfo));

            lock (DataProviderSyncLock)
            {
                var type = factoryInfo.GetMappedType();
                var assembly = factoryInfo.GetAssembly();

                // var providerFactoryType = Type.GetType(factoryInfo.ProviderFactoryType); HAS A PROIBLEM WITH VERSIONING
                var providerFactoryType = assembly.DefinedTypes
                    .FirstOrDefault(t => t.AssemblyQualifiedName == factoryInfo.ProviderFactoryType)?.AsType();
                if (providerFactoryType == null) providerFactoryType = assembly.GetType(factoryInfo.ProviderFactoryType);
                if (providerFactoryType == null) providerFactoryType = Type.GetType(factoryInfo.ProviderFactoryType);

                if (providerFactoryType == null)
                    throw new Exception("Could not find the type " + factoryInfo.ProviderFactoryType + " as specified in configuration.");

                var providerFactory = (IDataProviderFactory)Activator.CreateInstance(providerFactoryType, factoryInfo);

                if (type != null)
                {
                    TypeProviderFactories[type] = providerFactory;
                }
                else if (assembly != null && providerFactory != null)
                {
                    AssemblyProviderFactories[assembly] = providerFactory;
                }
            }
        }

        internal static Dictionary<Assembly, IDataProviderFactory> AssemblyProviderFactories;
        static Dictionary<Type, IDataProviderFactory> TypeProviderFactories;

        /// <summary>
        /// Gets the assemblies for which a data provider factory has been registered in the current domain.
        /// </summary>
        public static IEnumerable<Assembly> GetRegisteredAssemblies()
        {
            return TypeProviderFactories.Keys.Select(t => t.GetAssembly()).Concat(AssemblyProviderFactories.Keys).Distinct().ToArray();
        }

        public static IDataProvider GetProvider<T>() where T : IEntity => GetProvider(typeof(T));

        public static IDataProvider GetProvider(IEntity item) => GetProvider(item.GetType());

        public static IDataProvider GetProvider(Type type)
        {
            if (TypeProviderFactories.ContainsKey(type))
                return TypeProviderFactories[type].GetProvider(type);

            // Strange bug: 
            if (AssemblyProviderFactories.Any(x => x.Key == null))
                AssemblyProviderFactories = new Dictionary<Assembly, IDataProviderFactory>();

            if (!AssemblyProviderFactories.ContainsKey(type.GetAssembly()))
                throw new InvalidOperationException("There is no registered 'data provider' for the assembly: " + type.GetAssembly().FullName);

            return AssemblyProviderFactories[type.GetAssembly()].GetProvider(type);
        }

        /// <summary>
        /// Creates a transaction scope.
        /// </summary>
        public static ITransactionScope CreateTransactionScope(DbTransactionScopeOption option = DbTransactionScopeOption.Required)
        {
            var typeName = Config.Get<string>("Default.TransactionScope.Type");

            if (typeName.HasValue())
            {
                var type = Type.GetType(typeName);
                if (type == null) throw new Exception("Cannot load type: " + typeName);

                return (ITransactionScope)type.CreateInstance(new object[] { option });
            }

            return new DbTransactionScope(option);
        }
    }
}