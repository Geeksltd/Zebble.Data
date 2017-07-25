namespace Zebble.Data
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class ValidationResult
    {
        public readonly List<ValidationIssue> Issues = new List<ValidationIssue>();

        public void Add(ValidationIssue issue) => Issues.Add(issue);

        public void Add(string error) => Add(new ValidationIssue(error));
        public void Add(string propertyName, string error) => Add(new ValidationIssue(propertyName, error));

        public bool Any() => Issues.Any();

        public override string ToString()
        {
            if (Issues.IsSingle()) return Issues.Single().ToString();

            return Issues.Select(x => "- " + x).ToLinesString();
        }
    }
}
