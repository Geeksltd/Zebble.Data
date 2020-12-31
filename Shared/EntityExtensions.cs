namespace System
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Zebble.Data;
    using Olive;

    public static class ZebbleDataExtensions
    {
        /// <summary>
        /// Determines if this item is in a specified list of specified items.
        /// </summary>
        public static bool IsAnyOf<T>(this T item, params T[] options) where T : IEntity
        {
            if (item == null) return options.Contains(default(T));

            return options.Contains(item);
        }

        public static void Validate(this IEntity entity)
        {
            var validationResult = new ValidationResult();
            entity.Validate(validationResult);
            if (validationResult.Any()) throw new ValidationException(validationResult);
        }

        /// <summary>
        /// Determines if this item is in a specified list of specified items.
        /// </summary>
        public static bool IsAnyOf<T>(this T item, IEnumerable<T> options) where T : IEntity
        {
            return options.Contains(item);
        }

        /// <summary>
        /// Determines if this item is none of a list of specified items.
        /// </summary>
        public static bool IsNoneOf<T>(this T item, params T[] options) where T : IEntity
        {
            if (item == null) return !options.Contains(default(T));

            return !options.Contains(item);
        }

        /// <summary>
        /// Determines if this item is none of a list of specified items.
        /// </summary>
        public static bool IsNoneOf<T>(this T item, IEnumerable<T> options) where T : IEntity
        {
            if (item == null) return !options.Contains(default(T));

            return !options.Contains(item);
        }

        /// <summary>
        /// Clones all items of this collection.
        /// </summary>
        public static List<T> CloneAll<T>(this IEnumerable<T> list) where T : IEntity
        {
            return list.Select(i => (T)i.Clone()).ToList();
        }

        /// <summary>
        /// Gets the id of this entity.
        /// </summary>
        internal static string GetFullIdentifierString(this IEntity entity)
        {
            if (entity == null) return null;

            return entity.GetType().GetRootEntityType().FullName + "/" + entity.GetId();
        }

        /// <summary>
        /// Validates all entities in this collection.
        /// </summary>
        public static void ValidateAll<TEntity>(this IEnumerable<TEntity> entities) where TEntity : Entity
        {
            foreach (var entity in entities) entity.Validate();
        }

        /// <summary>
        /// Returns this Entity only if the given predicate evaluates to true and this is not null.
        /// </summary>        
        public static T OnlyWhen<T>(this T entity, Func<T, bool> criteria) where T : Entity
        {
            return entity != null && criteria(entity) ? entity : null;
        }

        /// <summary>
        /// Returns all entity Guid IDs for this collection.
        /// </summary>
        public static IEnumerable<TId> IDs<TId>(this IEnumerable<IEntity<TId>> entities)
        {
            return entities.Select(entity => entity.ID);
        }

        public static IEnumerable<T> ApplySearch<T>(this IEnumerable<T> list, string keywords) where T : Entity
        {
            var words = keywords.OrEmpty().Split(' ').Trim().ToArray();
            if (words.None()) return list;

            var result = list.Select(i => new { Item = i, Text = i.ToString(), Full = i.ToString("F") }).ToList();

            return result
                .Where(x => x.Full.ContainsAll(words, caseSensitive: false))
                .OrderByDescending(x => words.Count(w => x.Text.Contains(w, caseSensitive: false)))
                .ThenByDescending(x => words.Count(w => x.Full.Contains(w, caseSensitive: false)))
                .Select(x => x.Item)
                .ToList();
        }

        public static int? GetResultsToFetch(this IEnumerable<QueryOption> options)
        {
            return options.OfType<ResultSetSizeQueryOption>().FirstOrDefault()?.Number;
        }

        /// <summary>
        /// This will use Database.Get() to load the specified entity type with this ID.
        /// </summary>
        public static T To<T>(this Guid? guid) where T : IEntity
        {
            if (guid == null) return default(T);

            return guid.Value.To<T>();
        }

        /// <summary>
        /// This will use Database.Get() to load the specified entity type with this ID.
        /// </summary>
        public static T To<T>(this Guid guid) where T : IEntity
        {
            if (guid == Guid.Empty) return default(T);

            return Database.Get<T>(guid);
        }

        /// <summary>
        /// Gets the root entity type of this type.
        /// If this type inherits directly from Entity&lt;T&gt; then it will be returned, otherwise its parent...
        /// </summary>
        public static Type GetRootEntityType(this Type objectType)
        {
            if (objectType.BaseType == null)
                throw new NotSupportedException(objectType.FullName + " not recognised. It must be a subclass of Zebble.Framework.Entity.");

            if (objectType.BaseType.Name == "GuidEntity") return objectType;
            if (objectType.BaseType == typeof(Entity<int>)) return objectType;
            if (objectType.BaseType == typeof(Entity<long>)) return objectType;
            if (objectType.BaseType == typeof(Entity<string>)) return objectType;

            return GetRootEntityType(objectType.BaseType);
        }
    }
}