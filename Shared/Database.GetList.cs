namespace Zebble.Data
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Olive;

    partial class Database
    {
        static List<T> GetConcreteList<T>(IEnumerable<ICriterion> conditions, params QueryOption[] options) where T : IEntity
        {
            #region Load the instances
            List<T> rawObjects;
            var objectType = typeof(T);

            rawObjects = GetProvider<T>().GetList(objectType, conditions, options).Cast<T>().ToList();

            #endregion

            var result = new List<T>();

            foreach (var item in rawObjects)
            {
                // In-session objects has higher priority:

                var inCache = default(T);

                if (inCache == null)
                    inCache = (T)(object)Cache.Current.Get(item.GetType(), item.GetId().ToString());

                if (inCache == null)
                {
                    var asEntity = item as Entity;
                    EntityManager.RaiseOnLoaded(asEntity);

                    if (!AnyOpenTransaction()) Cache.Current.Add(asEntity);

                    result.Add(item);
                }
                else result.Add(inCache);
            }

            if (options.OfType<SortQueryOption>().None() && options.OfType<PagingQueryOption>().None())
            {
                // Sort the collection if T is a generic IComparable:
                if (typeof(T).Implements<IComparable<T>>() || typeof(T).Implements<IComparable>())
                    // Note: T is always IComparable! 
                    result.Sort();
            }

            return result;
        }

        /// <summary>
        /// Returns a list of entities with the specified type.
        /// </summary>
        public static IEnumerable<T> GetList<T>(IEnumerable<ICriterion> criteria) where T : IEntity => GetList<T>(criteria, null);

        /// <summary>
        /// Gets a list of entities of the given type from the database.
        /// </summary>
        public static IEnumerable<T> GetList<T>() where T : IEntity => GetList<T>(new ICriterion[0]);

        /// <summary>
        /// Returns a list of entities with the specified type.
        /// </summary>
        public static IEnumerable<T> GetList<T>(params QueryOption[] options) where T : IEntity
        {
            return GetList<T>(new ICriterion[0], options);
        }

        /// <summary>
        /// Returns a list of entities with the specified type.
        /// </summary>
        public static IEnumerable<T> GetList<T>(IEnumerable<ICriterion> criteria, params QueryOption[] options) where T : IEntity
        {
            List<T> result = null;
            string cacheKey = null;

            options = options ?? new QueryOption[0];

            var numberOfRecords = options.GetResultsToFetch();

            var canCache = options.None() || (options.IsSingle() && numberOfRecords == 1);

            canCache &= criteria.OfType<DirectDatabaseCriterion>().All(x => x.IsCacheSafe);

            if (criteria.Except(typeof(DirectDatabaseCriterion)).Any(c => c.PropertyName.Contains(".")))
            {
                // This doesn't work with cache expiration rules.
                canCache = false;
            }

            if (canCache)
            {
                // Standard query, try the cache first:
                try
                {
                    cacheKey = Cache.BuildQueryKey(typeof(T), criteria, numberOfRecords);

                }
                catch { throw; }

                if (result != null) return result;

                result = Cache.Current.GetList(typeof(T), cacheKey) as List<T>;
                if (result != null) return result;
            }

            result = GetConcreteList<T>(criteria, options);

            if (canCache)
            {
                // Don't cache the result if it is fetched in a transaction.
                if (!AnyOpenTransaction())
                {
                    Cache.Current.AddList(typeof(T), cacheKey, result);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets the list of objects with the specified type matching the specified criteria.
        /// If no criteria is specified, all instances will be returned.
        /// </summary>        
        public static IEnumerable<T> GetList<T>(params Criterion[] criteria) where T : IEntity => GetList<T>(criteria.OfType<ICriterion>());

        /// <summary>
        /// Gets a list of entities of the given type from the database.
        /// </summary>
        public static IEnumerable<T> GetList<T>(Expression<Func<T, bool>> criteria) where T : IEntity => GetList<T>(criteria, null);

        /// <summary>
        /// Gets a list of entities of the given type from the database.
        /// </summary>
        /// <param name="orderBy">The order by expression to run at the database level. It supports only one property.</param>
        /// <param name="desc">Specified whether the order by is descending.</param>
        public static IEnumerable<T> GetList<T>(Expression<Func<T, bool>> criteria, Expression<Func<T, object>> orderBy, bool desc = false) where T : IEntity
        {
            return GetList<T>(criteria, QueryOption.OrderBy<T>(orderBy, desc));
        }

        /// <summary>
        /// Gets a list of entities of the given type from the database.
        /// </summary>
        public static IEnumerable<T> GetList<T>(Expression<Func<T, bool>> criteria, params QueryOption[] options) where T : IEntity
        {
            options = options ?? new QueryOption[0];

            var runner = ExpressionRunner<T>.CreateRunner(criteria);

            if (runner.DynamicCriteria == null)
            {
                return GetList<T>(runner.Conditions.Cast<ICriterion>(), options);
            }
            else
            {
                var result = GetList<T>(runner.Conditions);
                result = result.Where(r => runner.DynamicCriteria(r)).ToArray();

                var resultsToFetch = options.GetResultsToFetch();
                if (resultsToFetch.HasValue && resultsToFetch.HasValue && result.Count() > resultsToFetch)
                    result = result.Take(resultsToFetch.Value).ToArray();

                return result;
            }
        }

        /// <summary>
        /// Returns a list of entities with the specified type.
        /// </summary>
        public static IEnumerable<T> GetList<T>(IEnumerable<Criterion> criteria) where T : IEntity => GetList<T>(criteria.ToArray());

        public static IEnumerable<Entity> GetList(Type type, QueryOption[] queryOptions) => GetList(type, new Criterion[0], queryOptions);

        public static IEnumerable<Entity> GetList(Type type, IEnumerable<ICriterion> criteria, QueryOption[] queryOptions)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            if (criteria == null) criteria = new ICriterion[0];

            var method = typeof(Database).GetMethod("GetList", new[] { typeof(IEnumerable<ICriterion>), typeof(QueryOption[]) })
                .MakeGenericMethod(type);

            var result = new List<Entity>();

            foreach (Entity item in (IEnumerable)method.Invoke(null, new object[] { criteria, queryOptions }))
                result.Add(item);

            return result;
        }

        public static IEnumerable<Entity> GetList(Type type, params Criterion[] conditions)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            var method = typeof(Database).GetMethod("GetList", new[] { typeof(Criterion[]) }).MakeGenericMethod(type);

            var result = new List<Entity>();

            foreach (Entity item in (IEnumerable)method.Invoke(null, new object[] { conditions }))
                result.Add(item);

            return result;
        }

        /// <summary>
        /// Gets the list of T objects from their specified IDs.
        /// </summary>
        public static IEnumerable<T> GetList<T>(IEnumerable<Guid> ids) where T : IEntity => ids.Select(Get<T>);

        /// <summary>
        /// Gets the list of objects from their specified IDs.
        /// </summary>
        public static IEnumerable<IEntity> GetList(Type type, IEnumerable<Guid> ids)
        {
            return ids.Select(id => Get(id, type));
        }
    }
}