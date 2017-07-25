namespace Zebble.Data
{
    /// <summary>
    /// Provides an abstraction for database query criteria.
    /// </summary>
    public interface ICriterion
    {
        string PropertyName { get; }

        FilterFunction FilterFunction { get; set; }
        string Value { get; }

        string SqlCondition { get; }
    }
}