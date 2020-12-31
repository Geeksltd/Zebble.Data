namespace Zebble.Data
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Represents an M# Entity.
    /// </summary>
    public interface IEntity : ICloneable, IComparable
    {
        /// <summary>
        /// Determines whether this object has just been instantiated as a new object, or represent an already persisted instance.
        /// </summary>
        bool IsNew { get; }

        /// <summary>
        /// Validates this instance and throws ValidationException if necessary.
        /// </summary>
        void Validate(ValidationResult result);

        /// <summary>
        /// Gets the id of this entity.
        /// </summary>        
        object GetId();

        /// <summary>
        /// Invalidates all its cached references.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void InvalidateCachedReferences();
    }

    /// <summary>
    /// A persistent object in the application.
    /// </summary>
    public interface IEntity<T> : IEntity
    {
        /// <summary>
        /// Gets the ID.
        /// </summary>
        T ID { get; set; }
    }
}