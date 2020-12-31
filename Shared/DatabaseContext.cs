using System;

namespace Zebble.Data
{
    public class DatabaseContext : IDisposable
    {
        #region ConnectionString
        /// <summary>
        /// Gets or sets the ConnectionString of this DatabaseContext.
        /// </summary>
        public string ConnectionString { get; set; }

        public int? CommandTimeout { get; set; }

        #endregion

        DatabaseContext Parent;

        public DatabaseContext(string connectionString)
        {
            ConnectionString = connectionString;

            if (Current != null)
                Parent = Current;

            Current = this;
        }

        [ThreadStatic]
        public static DatabaseContext Current = null;

        public void Dispose()
        {
            Current = Parent;
        }

    }
}
