namespace JavaVirtualMachine
{
    public interface IConstant { }

    public class Record<T> : IConstant
    {
        public T Instance { get; }
        
        public Record(T record)
        {
            Instance = record;
        }
    }

    public class ClassInfo : IConstant
    {
        public ushort NameIndex { get; }

        public ClassInfo(ushort nameIndex)
        {
            NameIndex = nameIndex;
        }
    }

    public class MethodType : IConstant
    {
        public ushort DescriptorIndex { get; }

        public MethodType(ushort descriptorIndex)
        {
            DescriptorIndex = descriptorIndex;
        }
    }


    /// <summary>
    /// This class represents:
    /// Fieldref_info,
    /// Methodref_info,
    /// InterfaceMethodref_info
    /// </summary>
    public class FiMeInRef : IConstant
    {
        public ushort ClassIndex { get; }
        public ushort NameAndTypeIndex { get; }

        public FiMeInRef(ushort classIndex, ushort nameAndTypeIndex)
        {
            ClassIndex = classIndex;
            NameAndTypeIndex = nameAndTypeIndex;
        }
    }

    public class NameAndTypeInfo : IConstant
    {
        public ushort NameIndex { get; }
        public ushort DescriptorIndex { get; }

        public NameAndTypeInfo(ushort nameIndex, ushort descriptorIndex)
        {
            NameIndex = nameIndex;
            DescriptorIndex = descriptorIndex;
        }
    }

    public class MethodHandle : IConstant
    {
        public byte ReferenceKind { get; }
        public ushort ReferenceIndex { get; }

        public MethodHandle(byte referenceKind, ushort referenceIndex)
        {
            ReferenceKind = referenceKind;
            ReferenceIndex = referenceIndex;
        }
    }

    public class InvokeDynamic : IConstant
    {
        public ushort BootstrapMethodAttrIndex { get; }
        public ushort NameAndTypeIndex { get; }

        public InvokeDynamic(ushort bootstrapMethodAttrIndex, ushort nameAndTypeIndex)
        {
            BootstrapMethodAttrIndex = bootstrapMethodAttrIndex;
            NameAndTypeIndex = nameAndTypeIndex;
        }
    }

    public class StringRef : IConstant
    {
        public ushort StringIndex { get; }

        public StringRef(ushort stringIndex)
        {
            StringIndex = stringIndex;
        }
    }
}