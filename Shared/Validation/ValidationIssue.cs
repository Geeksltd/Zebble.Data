namespace Zebble.Data
{
    public class ValidationIssue
    {
        public string Error { get; set; }
        public string PropertyName { get; set; }

        public ValidationIssue() { }

        public ValidationIssue(string error) { Error = error; }

        public ValidationIssue(string propertyName, string error) { Error = error; PropertyName = propertyName; }

        public override string ToString() => Error;
    }
}