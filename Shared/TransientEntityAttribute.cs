using System;
using Olive;

namespace Zebble.Data
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TransientEntityAttribute : Attribute
    {
        public static bool IsTransient(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.Defines<TransientEntityAttribute>();
        }
    }
}
