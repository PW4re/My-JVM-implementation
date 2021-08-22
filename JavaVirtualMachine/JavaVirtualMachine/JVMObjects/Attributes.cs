namespace JavaVirtualMachine.JVMObjects
{
    public class Attribute
    {
        ushort AttributeNameIndex { get; }

        uint AttributeLength { get; }
        // u1 info[attribute_length]

        protected Attribute(ushort attributeNameIndex, uint attributeLength)
        {
            AttributeNameIndex = attributeNameIndex;
            AttributeLength = attributeLength;
        }
    }

    public class ConstantValueAttribute : Attribute
    {
        public uint Index { get; } // valid index into the constant_pool

        public ConstantValueAttribute(ushort attributeNameIndex, uint attributeLength, uint index)
            : base(attributeNameIndex, attributeLength)
        {
            Index = index;
        }
    }

    public class CodeAttribute : Attribute
    {
        public CodeAttribute(ushort attributeNameIndex, uint attributeLength) 
            : base(attributeNameIndex, attributeLength)
        {
            
        }
    }
}