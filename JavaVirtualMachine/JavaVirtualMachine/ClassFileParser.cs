using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace JavaVirtualMachine
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
            //content = ReadClassFile(fileName);
            _bReader = ReadClassFile(fileName);
            constantPoolByteCount = 0;
        }

        private MyBinaryReader ReadClassFile(string fileName)
        {
            return new MyBinaryReader(File.Open(fileName, FileMode.Open));
            // byte[] bytes = null;
            // try
            // {
            //     using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            //     bytes = new byte[fStream.Length];
            //     var numBytesToRead = (int) fStream.Length;
            //     var numBytesRead = 0;
            //     while (numBytesToRead > 0)
            //     {
            //         var n = fStream.Read(bytes, numBytesRead, numBytesToRead);
            //
            //         if (n == 0)
            //             break;
            //
            //         numBytesRead += n;
            //         numBytesToRead -= n;
            //     }
            // }
            // catch ( FileNotFoundException )
            // {
            //     Console.WriteLine($"Не удалось найти класс {fileName}");
            //     Environment.Exit(7);
            // }
            //
            // return bytes;
        }

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

        // private ushort GetU2Value(byte b1, byte b2) 
        //     => 
    }

    public class ConstantPoolParser : IPoolParser<IConstant>
    {
        private readonly ushort _poolCount;
        
        public ConstantPoolParser(ushort count)
        {
            _poolCount = count;
        }
        
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        private enum Tags : byte
        {
            CONSTANT_Class = 7,
            CONSTANT_Fieldref = 9,
            CONSTANT_Methodref = 10,
            CONSTANT_InterfaceMethodref = 11,
            CONSTANT_String = 8,
            CONSTANT_Integer = 3,
            CONSTANT_Float = 4,
            CONSTANT_Long = 5,
            CONSTANT_Double = 6,
            CONSTANT_NameAndType = 12,
            CONSTANT_Utf8 = 1,
            CONSTANT_MethodHandle = 15,
            CONSTANT_MethodType = 16,
            CONSTANT_InvokeDynamic = 18
        }

        private static Record<string> ParseUtfStrings(IReadOnlyList<byte> fragment, int length)
        {
            var builder = new StringBuilder();
            for (var i = 0; i < length; ) // Тут нужно бросать ошибки при встрече непонятных байтов
            {
                var codePoint = (char) fragment[i];
                if (codePoint >= '\u0001' && codePoint <= '\u007F')
                {
                    builder.Append(codePoint); // Возможно, нужно сделать <<1
                    i++;
                    continue;
                }

                codePoint = (char) (((fragment[i] & 0x1f) << 6) + (fragment[i + 1] & 0x3f));
                if (codePoint == '\u0000' || codePoint >= '\u0080' && codePoint <= '\u07FF')
                {
                    builder.Append(codePoint);
                    i += 2;
                    continue;
                }

                codePoint = (char) (((fragment[i] & 0xf) << 12) + ((fragment[i + 1] & 0x3f) << 6) +
                                    (fragment[i + 2] & 0x3f));

                if (codePoint >= '\u0800')
                {
                    builder.Append(codePoint);
                    i += 3;
                    continue;
                }

                codePoint = (char) (0x10000 + ((fragment[i + 1] & 0x0f) << 16) +
                                    ((fragment[i + 2] & 0x3f) << 10) +
                                    ((fragment[i + 4] & 0x0f) << 6) + (fragment[i + 5] & 0x3f));
                builder.Append(codePoint);
                i += 6;
            } // u1 bytes[length]

            return new Record<string>(builder.ToString());
        }

        protected Reference ParseReference(BinaryReader reader)  // Это неверно
            => new Reference(reader.ReadUInt16());

        private Record<int> ParseIntegerConstant(MyBinaryReader reader)
            => new Record<int>(reader.ReadInt32());

        private Record<float> ParseFloatConstant(MyBinaryReader reader)
            => new Record<float>(reader.ReadSingle());

        private Tuple<Record<int>, Record<int>> ParseLongOrDoubleConstant(MyBinaryReader reader)
            => Tuple.Create(ParseIntegerConstant(reader), ParseIntegerConstant(reader));

        private Record<long> ParseLongConstant(MyBinaryReader reader)
        {
            // var highBytes = reader.ReadInt32();
            // var lowBytes = reader.ReadInt32();
            return new Record<long>(reader.ReadInt64());
        }

        private Record<double> ParseDoubleConstant(MyBinaryReader reader)
        {
            // var bits = reader.ReadInt64();
            // var s = bits >> 63 == 0 ? 1 : -1;
            // var e = (int)((bits >> 52) & 0x7ffL);
            // var m = e == 0 ? (bits & 0xfffffffffffffL) << 1 : (bits & 0xfffffffffffffL) | 0x10000000000000L;
            //
            return new Record<double>(reader.ReadDouble());
        }
        
        public List<IConstant> Parse(MyBinaryReader reader)
        {
            var pool = new List<IConstant>(_poolCount);
            //int index;
            //for (index = 0; index < fragment.Length && _poolCount - Pool.Count > 0; index++) 
            while (_poolCount - pool.Count > 0)
            {
                switch ((Tags) reader.ReadByte())
                { // Здесь всем методам будем передавать index + 1
                    case Tags.CONSTANT_Class:
                        // parse 2byte name_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                    case Tags.CONSTANT_Fieldref:
                    case Tags.CONSTANT_Methodref:
                    case Tags.CONSTANT_InterfaceMethodref:
                        // u2 class_index
                        // u2 name_and_type_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                    case Tags.CONSTANT_String:
                        // u2 string_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                    case Tags.CONSTANT_Integer:
                        // u4 bytes
                        pool.Add(ParseIntegerConstant(reader));
                        //index += 4;
                        break;
                    case Tags.CONSTANT_Float: // Не знаю, что будет с float, попробую не пользоваться формулой s * m * 2**(e-150)
                        // u4 bytes
                        pool.Add(ParseFloatConstant(reader));
                        //index += 4;
                        break;
                    case Tags.CONSTANT_Long:
                    case Tags.CONSTANT_Double:
                        var pair = ParseLongOrDoubleConstant(reader);
                        pool.Add(pair.Item1);
                        pool.Add(pair.Item2);
                        //index += 8;
                        break;
                    case Tags.CONSTANT_NameAndType:
                        // u2 name_index
                        // u2 descriptor_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                    case Tags.CONSTANT_Utf8:
                        var length = reader.ReadUInt16(); // u2 length
                        // Здесь получим строку для записи в пул констант
                        var fragment = reader.ReadBytes(length);
                        pool.Add(ParseUtfStrings(fragment, length)); 
                        // Прочитали длину и саму строку
                        //index += 2 + length;
                        break;
                    case Tags.CONSTANT_MethodHandle:
                        // u1 reference_kind in range(1 to 9) 
                        // u2 reference_index
                        pool.Add(new Record<byte>(reader.ReadByte())); // Надо бы валидацию
                        //index++;
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                    case Tags.CONSTANT_MethodType:
                        // u2 descriptor_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                    case Tags.CONSTANT_InvokeDynamic:
                        // u2 bootstrap_method_attr_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        // u2 name_and_type_index
                        pool.Add(ParseReference(reader));
                        //index += 2;
                        break;
                }
            }

            return pool;
        }
    }

    public class InterfacesParser : IPoolParser<ushort>
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

    public class AttributesParser
    {
        
    }
}