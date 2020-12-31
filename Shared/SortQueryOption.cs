namespace Zebble.Data
{
    public class SortQueryOption : QueryOption
    {
        /// <summary>
        /// Creates a new SortQueryOption instance.
        /// </summary>
        internal SortQueryOption() { }

        #region Property
        /// <summary>
        /// Gets or sets the Property of this SortQueryOption.
        /// </summary>
        public string Property { get; set; }
        #endregion

        #region Descending
        /// <summary>
        /// Gets or sets the Descending of this SortQueryOption.
        /// </summary>
        public bool Descending { get; set; }
        #endregion

    }
}
