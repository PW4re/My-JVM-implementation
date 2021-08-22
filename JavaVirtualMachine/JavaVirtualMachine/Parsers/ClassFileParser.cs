using System;
using System.Collections.Generic;
using System.IO;
using JavaVirtualMachine.JVMObjects;

namespace JavaVirtualMachine.Parsers
{
    public interface IPoolParser<T>
    {
        List<T> Parse(MyBinaryReader binaryReader);
    }
    
    public class ClassFileParser
    {
        //private byte[] content;
        private MyBinaryReader _bReader;
        private int constantPoolByteCount;
        private const uint Magic = 0xCAFEBABE;
        private ushort _minorVersion, _majorVersion, _constantPoolCount;
        private List<IConstant> _constantPool;
        private ushort _accessFlags, _thisClass,_superClass, _interfacesCount;
        private List<ushort> _interfaces;
        private ushort _fieldsCount, _methodsCount, _attributesCount;

        public ClassFileParser(string fileName)
        {
            _bReader = ReadClassFile(fileName);
            constantPoolByteCount = 0;
        }

        private MyBinaryReader ReadClassFile(string fileName) 
            => new MyBinaryReader(File.Open(fileName, FileMode.Open));

        private uint GetUIntFromBytes(byte b0, byte b1, byte b2, byte b3)
            => (uint) (b0 << 24) + (uint) (b1 << 16) + (ushort) (b2 << 8) + b3;

        public ParsedClassFile Parse()
        {
            if (_bReader.ReadUInt32() != Magic)
                throw new Exception("MagicException"); // Ошибочку нужно создать
            
            var minorVersion = _bReader.ReadUInt16();
            var majorVersion = _bReader.ReadUInt16();
            
            var constantPoolCount = _bReader.ReadUInt16();
            var constantPool = GetConstantPool(constantPoolCount);
            
            var acessFlags = _bReader.ReadUInt16();
            var thisClass = _bReader.ReadUInt16();
            var superClass = _bReader.ReadUInt16();
            var interfacesCount = _bReader.ReadUInt16();
            var interfaces = GetInterfaces(_interfacesCount);

            return new ParsedClassFile(minorVersion, majorVersion, constantPoolCount, constantPool);
        }
        
        
        private List<ushort> GetInterfaces(ushort count)
        {
            var parser = new InterfacesParser(count);
            
            return parser.Parse(_bReader);
        }

        private List<IConstant> GetConstantPool(ushort count)
        {
            var parser = new ConstantPoolParser(count);
            
            return  parser.Parse(_bReader);
        }
    }

    internal class InterfacesParser : IPoolParser<ushort>
    {
        private ushort interfacesCount;

        public InterfacesParser(ushort poolCount)
        {
            interfacesCount = poolCount;
        }
        
        public List<ushort> Parse(MyBinaryReader reader)
        {
            var pool = new List<ushort>(interfacesCount);
            // int index;
            // for (index = 0; index < fragment.Length - 1; index += 2) 
            //     Pool.Add((ushort) (fragment[index] << 8 + fragment[index + 1]));
            //
            // if (index != fragment.Length / 2) throw new Exception("Ошибка с интерфейсами");
            
            // return index;

            for (var _ = 0; _ < interfacesCount; _++)
                pool.Add(reader.ReadUInt16());

            return pool;
        }

    }

    internal class FieldsParser
    {
        
    }
}