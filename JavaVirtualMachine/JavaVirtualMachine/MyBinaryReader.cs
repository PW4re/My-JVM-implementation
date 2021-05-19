using System;
using System.IO;
using System.Text;

namespace JavaVirtualMachine
{
    public class MyBinaryReader : BinaryReader  // read some BE
    {
        public MyBinaryReader(Stream input) : base(input) { }

        public MyBinaryReader(Stream input, Encoding encoding) : base(input, encoding) { }

        public MyBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen) { }

        public override ushort ReadUInt16() => BitConverter.ToUInt16(ReadDataAndReverse(2));

        public override uint ReadUInt32() => BitConverter.ToUInt32(ReadDataAndReverse(4));
        
        public override int ReadInt32() => BitConverter.ToInt32(ReadDataAndReverse(4));

        public override float ReadSingle() => BitConverter.ToSingle(ReadDataAndReverse(4));

        // Тут нет уверенности
        public override long ReadInt64() => BitConverter.ToInt64(ReadDataAndReverse(8));

        // И тут
        public override double ReadDouble() => BitConverter.ToDouble(ReadDataAndReverse(8));

        private byte[] ReadDataAndReverse(int length)
        {
            var data = ReadBytes(length);
            Array.Reverse(data);

            return data;
        }
    }
}