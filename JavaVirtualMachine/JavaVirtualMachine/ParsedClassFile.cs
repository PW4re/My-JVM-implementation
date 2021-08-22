using System.Collections.Generic;
using JavaVirtualMachine.JVMObjects;

namespace JavaVirtualMachine
{
    public readonly struct ParsedClassFile
    {
        // Long:
        // ((long) high_bytes << 32) + low_bytes
        
        // Double:
        // int s = ((bits >> 63) == 0) ? 1 : -1;
        // int e = (int)((bits >> 52) & 0x7ffL);
        // long m = (e == 0) ?
        //     (bits & 0xfffffffffffffL) << 1 :
        //     (bits & 0xfffffffffffffL) | 0x10000000000000L;
        
        private const uint Magic = 0xCAFEBABE;
        public ushort MinorVersion { get; }
        public ushort MajorVersion { get; }
        public ushort ConstantPoolCount { get; }
        
        public List<IConstant> ConstantPoolTable { get; } // indexed from 1 to constantPoolCount - 1

        public ParsedClassFile(ushort minorVersion, ushort majorVersion, ushort constantPoolCount,
            List<IConstant> constantPoolTable)
        {
            MinorVersion = minorVersion;
            MajorVersion = majorVersion;
            ConstantPoolCount = constantPoolCount;
            ConstantPoolTable = constantPoolTable;
        }
    }
}