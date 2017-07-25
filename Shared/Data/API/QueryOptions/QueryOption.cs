using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Zebble.Data.QueryOptions;

namespace Zebble.Data
{
    public abstract class QueryOption
    {
        public static QueryOption Take(int number)
        {
            return new ResultSetSizeQueryOption { Number = number };
        }

        /// <summary>
        /// Takes a range of records. The startIndex is 0 based.
        /// </summary>
        public static QueryOption Range(int startRecordIndex, int numberOfRecords)
        {
            return new RangeQueryOption { From = startRecordIndex, Number = numberOfRecords };
        }

        //public static QueryOption TakePercent(double percent)
        //{
        //    return new ResultSetSizeQueryOption { Percent = percent };
        //}

        #region OrderBy(string)

        public static SortQueryOption OrderBy(string property)
        {
            return OrderBy(property, descending: false);
        }

        public static SortQueryOption OrderByDescending(string property)
        {
            return OrderBy(property, descending: true);
        }

        public static SortQueryOption OrderBy(string property, bool descending)
        {
            return new SortQueryOption { Property = property, Descending = descending };
        }

        #endregion

        #region OrderBy<T>(Expression<Func<T, object>>)

        public static SortQueryOption OrderBy<T>(Expression<Func<T, object>> property)
        {
            return OrderBy(property, descending: false);
        }

        public static SortQueryOption OrderByDescending<T>(Expression<Func<T, object>> property)
        {
            return OrderBy(property, descending: true);
        }

        public static SortQueryOption OrderBy<T>(Expression<Func<T, object>> property, bool descending)
        {
            var propertyExpression = (property.Body as UnaryExpression)?.Operand as MemberExpression;
            if (propertyExpression == null || !(propertyExpression.Expression is ParameterExpression))

                throw new Exception("Unsupported OrderBy expression. The only supported format is \"x => x.Property\". You provided: {0}"
                    .FormatWith(property.ToString()));

            return OrderBy(propertyExpression.Member.Name, descending);
        }

        #endregion

        #region OrderBy(Expression<Func<object>>)

        public static SortQueryOption OrderBy<T>(Expression<Func<T>> property)
        {
            return OrderBy<T>(property, descending: false);
        }

        public static SortQueryOption OrderByDescending<T>(Expression<Func<T>> property)
        {
            return OrderBy<T>(property, descending: true);
        }

        public static SortQueryOption OrderBy<T>(Expression<Func<T>> property, bool descending)
        {
            var propertyExpression = (property.Body as UnaryExpression)?.Operand as MemberExpression;
            if (propertyExpression == null)
                throw new Exception("Unsupported OrderBy expression. The only supported format is \"() => x.Property\". You provided: {0}"
                    .FormatWith(property.ToString()));

            return OrderBy(propertyExpression.Member.Name, descending);
        }

        #endregion

        #region FullTextSearch

        /// <summary>
        /// Creates a FullTextSearch option for the search query.
        /// </summary>
        public static FullTextSearchQueryOption FullTextSearch(string keyword, params string[] properties)
        {
            if (keyword.LacksValue())
                throw new ArgumentNullException("keyword");

            if (properties == null || properties.None())
                throw new ArgumentNullException("properties");

            return new FullTextSearchQueryOption { Keyword = keyword, Properties = properties };
        }

        public static FullTextSearchQueryOption FullTextSearch<T>(string keyword, params Expression<Func<T>>[] properties)
        {
            if (properties == null || properties.None())
                throw new ArgumentNullException("properties");

            var propertyNames = new List<string>();

            foreach (var property in properties)
            {
                var propertyExpression = (property.Body as UnaryExpression)?.Operand as MemberExpression;
                if (propertyExpression == null)
                    throw new Exception("Unsupported FullTextSearch expression. The only supported format is \"() => x.Property\". You provided: {0}"
                        .FormatWith(property.ToString()));

                propertyNames.Add(propertyExpression.Member.Name);
            }

            return FullTextSearch(keyword, propertyNames.ToArray());
        }

        public static FullTextSearchQueryOption FullTextSearch<T>(string keyword, params Expression<Func<T, object>>[] properties)
        {
            if (properties == null || properties.None())
                throw new ArgumentNullException("properties");

            var propertyNames = new List<string>();

            foreach (var property in properties)
            {
                var propertyExpression = (property.Body as UnaryExpression)?.Operand as MemberExpression;
                if (propertyExpression == null || !(propertyExpression.Expression is ParameterExpression))

                    throw new Exception("Unsupported OrderBy expression. The only supported format is \"x => x.Property\". You provided: {0}"
                        .FormatWith(property.ToString()));

                propertyNames.Add(propertyExpression.Member.Name);
            }

            return FullTextSearch(keyword, propertyNames.ToArray());
        }

        #endregion

        public static PagingQueryOption Paging(string orderBy, int startIndex, int pageSize)
        {
            return new PagingQueryOption(orderBy, startIndex, pageSize);
        }
    }
}