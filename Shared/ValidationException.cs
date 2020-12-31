namespace Zebble.Data
{
    using System;

    public class ValidationException : Exception
    {
        public ValidationException() { }

        public ValidationException(string messageFormat, params object[] arguments) : base(string.Format(messageFormat, arguments)) { }

        public ValidationException(string message) : base(message) { }

        public ValidationException(string message, Exception inner) : base(message, inner) { }

        public ValidationException(ValidationResult result) : this(result.ToString()) { Result = result; }

        public ValidationResult Result { get; private set; }
    }
}