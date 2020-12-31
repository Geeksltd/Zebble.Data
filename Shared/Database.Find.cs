namespace Zebble.Data
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Olive;

    partial class Database
    {
        /// <summary>
        /// Finds the object with maximum value of the specified property.
        /// </summary>
        public static T FindWithMax<T>(Func<T, object> member) where T : IEntity
        {
            return GetList<T>().WithMax<T, object>(member);
        }

        /// <summary>
        /// Finds the object with minimum value of the specified property.
        /// </summary>
        public static T FindWithMin<T>(Func<T, object> member) where T : IEntity
        {
            return GetList<T>().WithMin<T, object>(member);
        }

        /// <summary>
        /// Find an object with the specified type from the database.
        /// When used with no criteria, returns the first object found of the specified type.
        /// If not found, it returns null.
        /// </summary>
        public static T Find<T>(params Criterion[] criteria) where T : IEntity
        {
            return GetList<T>(criteria, QueryOption.Take(1)).FirstOrDefault();
        }

        /// <summary>
        /// Find an object with the specified type from the database.
        /// When used with no criteria, returns the first object found of the specified type.
        /// If not found, it returns null.
        /// </summary>
        /// <param name="orderBy">The order by expression to run at the database level. It supports only one property.</param>
        /// <param name="desc">Specified whether the order by is descending.</param>
        public static T Find<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> orderBy, bool desc = false) where T : IEntity
        {
            return GetList<T>(criteria, QueryOption.OrderBy<T>(orderBy, desc), QueryOption.Take(1)).FirstOrDefault();
        }

        /// <summary>
        /// Finds an object with the specified type matching the specified criteria.
        /// If not found, it returns null.
        /// </summary>
        public static T Find<T>(Expression<Func<T, bool>> criteria, params QueryOption[] options) where T : IEntity
        {
            options = options ?? new QueryOption[0];
            options = options.Concat(QueryOption.Take(1)).ToArray();
            return GetList<T>(criteria, options).FirstOrDefault();
        }
    }
}