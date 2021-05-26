using System.Diagnostics.CodeAnalysis;

namespace JavaVirtualMachine
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum AccessFlags
    {
        ACC_PUBLIС = 0x001,
        ACC_PRIVATE = 0x002,
        ACC_PROTECTED = 0x004,
        ACC_STATIC = 0x0008,
        ACC_FINAL = 0x0010,
        ACC_VOLATILE = 0x0040,
        ACC_TRANCIENT = 0x0080,
        ACC_SYNTHETIC = 0x1000,
        ACC_ENUM = 0x4000
    }
}