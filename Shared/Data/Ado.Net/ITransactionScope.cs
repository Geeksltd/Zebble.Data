using System;

namespace Zebble.Data
{
    public interface ITransactionScope : IDisposable
    {
        void Complete();
        Guid ID { get; }
    }
}
