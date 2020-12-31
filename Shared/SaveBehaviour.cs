using System;

namespace Zebble.Data
{
    [Flags]
    public enum SaveBehaviour
    {
        Default = 1,
        BypassValidation = 2,
        BypassSaving = 4,
        BypassSaved = 8,
        BypassLogging = 16,
        BypassAll = 30,
    }

    [Flags]
    public enum DeleteBehaviour
    {
        Default = 1,
        BypassDeleting = 2,
        BypassDeleted = 4,
        BypassAll = 14,
    }
}
