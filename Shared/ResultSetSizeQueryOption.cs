
namespace Zebble.Data
{
    public class ResultSetSizeQueryOption : QueryOption
    {
        /// <summary>
        /// Creates a new ResultSetSizeQueryOption instance.
        /// </summary>
        internal ResultSetSizeQueryOption()
        {
        }

        #region Number
        /// <summary>
        /// Gets or sets the Number of this ResultSetSizeQueryOption.
        /// </summary>
        public int? Number { get; set; }
        #endregion

        //#region Percent
        ///// <summary>
        ///// Gets or sets the Percent of this ResultSetSizeQueryOption.
        ///// </summary>
        //public double? Percent { get; set; }
        //#endregion



    }
}
