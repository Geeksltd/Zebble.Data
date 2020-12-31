using System;
using Olive;

namespace Zebble.Data
{
    public class PagingQueryOption : QueryOption
    {
        /// <summary>
        /// Creates a new ResultSetSizeQueryOption instance.
        /// </summary>
        internal PagingQueryOption() { }

        /// <summary>
        /// Creates a new ResultSetSizeQueryOption instance.
        /// </summary>
        public PagingQueryOption(string orderBy, int startIndex, int pageSize)
        {
            if (orderBy.IsEmpty())
                throw new ArgumentException("Invalid PagingQueryOption specified. OrderBy is mandatory.");

            if (pageSize < 1)
                throw new ArgumentException("Invalid PagingQueryOption specified. PageSize should be a positive number.");

            OrderBy = orderBy;
            PageSize = pageSize;
            StartIndex = startIndex;
        }

        public int StartIndex { get; internal set; }

        public int PageSize { get; internal set; }

        /// <summary>
        /// The direct SQL sort expression. E.g: MyColumn DESC, Something 
        /// </summary>
        public new string OrderBy { get; set; }
    }
}
