namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Enables adding a direct SQL WHERE criteria to the database query.
    /// </summary>
    public class DirectDatabaseCriterion : Criterion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DirectDatabaseCriterion" /> class.
        /// </summary>
        public DirectDatabaseCriterion(string sqlCriteria)
            : base("N/A", "N/A")
        {
            this.SqlCriteria = sqlCriteria;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectDatabaseCriterion" /> class.
        /// </summary>
        /// <param name="parameters">Item1 = Parameter name (without the @ character). Item2 = parameter value.</param>
        public DirectDatabaseCriterion(string sqlCriteria, params Tuple<string, object>[] parameters)
            : this(sqlCriteria)
        {
            foreach (var p in parameters)
                Parameters.Add(p.Item1, p.Item2);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectDatabaseCriterion" /> class.
        /// </summary>
        public DirectDatabaseCriterion(string sqlCriteria, Dictionary<string, object> parameters)
            : this(sqlCriteria)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");

            Parameters = parameters;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DirectDatabaseCriterion" /> class.
        /// </summary>
        /// <param name="parameters">Example: new {Parameter1 = SomeValue(), Parameter2 = AnotherValue()}</param>
        public DirectDatabaseCriterion(string sqlCriteria, object parameters)
            : this(sqlCriteria)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");

            Parameters = parameters.GetType().GetProperties().ToDictionary(x => x.Name, x => x.GetValue(parameters));
        }

        /// <summary>
        /// Gets the parameters used in the specified custom SQL criteria.
        /// </summary>
        public Dictionary<string, object> Parameters = new Dictionary<string, object>();

        /// <summary>
        /// Gets or sets the SQL criteria.
        /// </summary>
        public string SqlCriteria { get; set; }

        /// <summary>
        /// N/A.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new object Value
        {
            get => null;
            set => throw new NotSupportedException();
        }

        /// <summary>
        /// Specifies whether this criteria is compatible with normal caching.
        /// </summary>
        public bool IsCacheSafe { get; set; }

        /// <summary>
        /// Returns a string that represents this instance.
        /// </summary>
        public override string ToString()
        {
            return SqlCriteria + "|" + Parameters.Select(x => "{0}={1}".FormatWith(x.Key, x.Value)).ToString("|");
        }

        /// <summary>
        /// N/A.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public new FilterFunction FilterFunction
        {
            get { throw new NotSupportedException(); }
            set { throw new NotSupportedException(); }
        }

        internal string MapSqlCriteria(Dictionary<string, string> propertyMappings)
        {
            if (PropertyName.LacksValue()) return SqlCriteria;

            return SqlCriteria.Replace($"${{{{{PropertyName}}}}}", propertyMappings[PropertyName]);
        }
    }
}