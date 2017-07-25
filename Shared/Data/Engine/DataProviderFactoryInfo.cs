namespace Zebble.Data
{
    using System;
    using System.Reflection;

    public class DataProviderFactoryInfo
    {
        public string MappingResource { get; set; }

        public string AssemblyName { get; set; }
        public string TypeName { get; set; }
        public string ProviderFactoryType { get; set; }

        public string MappingDirectory { get; set; }

        public string ConnectionStringKey { get; set; }

        public string ConnectionString { get; set; }

        public Assembly Assembly { get; set; }
        public Type Type { get; set; }

        public Assembly GetAssembly()
        {
            if (Assembly == null) Assembly = Assembly.Load(new AssemblyName(AssemblyName));

            return Assembly;
        }

        public Type GetMappedType()
        {
            if (Type != null) return Type;

            if (TypeName.HasValue()) Type = GetAssembly().GetType(TypeName);

            return Type;
        }

        public string LoadMappingText(string resourceName)
        {
            try
            {
                return GetAssembly().ReadEmbeddedTextFile(resourceName);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not load the manifest resource text for '{0}'".FormatWith(resourceName), ex);
            }
        }
    }
}
