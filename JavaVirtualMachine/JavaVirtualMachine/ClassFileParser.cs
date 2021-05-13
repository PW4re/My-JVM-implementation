using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.IO;
using System.Text;

namespace JavaVirtualMachine
{
    public interface IPoolParser<T>
    {
        int Parse(byte[] fragment);
        List<T> Pool { get; }
    }
    
    public class ClassFileParser
    {
        private byte[] content;
        private int constantPoolByteCount;
        private const uint Magic = 0xCAFEBABE;
        private ushort _minorVersion, _majorVersion, _constantPoolCount;
        private List<IInfoObject> _constantPool;
        private ushort _accessFlags, _thisClass,_superClass, _interfacesCount;
        private List<ushort> _interfaces;
        private ushort _fieldsCount, _methodsCount, _attributesCount;

        public ClassFileParser(string fileName)
        {
            content = ReadClassFile(fileName);
            constantPoolByteCount = 0;
        }

        private byte[] ReadClassFile(string fileName)
        {
            byte[] bytes = null;
            try
            {
                using var fStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                bytes = new byte[fStream.Length];
                var numBytesToRead = (int) fStream.Length;
                var numBytesRead = 0;
                while (numBytesToRead > 0)
                {
                    var n = fStream.Read(bytes, numBytesRead, numBytesToRead);

                    if (n == 0)
                        break;

                    numBytesRead += n;
                    numBytesToRead -= n;
                }
            }
            catch ( FileNotFoundException )
            {
                Console.WriteLine($"Не удалось найти класс {fileName}");
                Environment.Exit(7);
            }

            return bytes;
        }

        public ParsedClassFile Parse()
        {
            Console.WriteLine(BitConverter.ToUInt32(new ArraySegment<byte>(content, 0, 4).Array));
            Console.WriteLine(Magic);
            if ((uint)(content[0] << 24) + (uint)(content[1] << 16) + (ushort)(content[2] << 8) + content[3] != Magic)
                throw new Exception(); // Ошибочку нужно создать
            
            ParseVersions();
            ParseConstantPool();
            var afterConstantPoolIndex = 10 + constantPoolByteCount;
            ParseFlagsAndThisAndSuper(afterConstantPoolIndex);
            _interfacesCount = ParseTwoByteValue(afterConstantPoolIndex + 6);
            _interfaces = GetInterfaces(_interfacesCount, afterConstantPoolIndex + 8);

            return new ParsedClassFile(_minorVersion, _majorVersion, _constantPoolCount, _constantPool);
        }


        private void ParseVersions()
        {
            _minorVersion = ParseTwoByteValue(4);
            _majorVersion = ParseTwoByteValue(6);
        }

        private void ParseConstantPool()
        {
            _constantPoolCount = ParseTwoByteValue(8);
            _constantPool = GetConstantPool(_constantPoolCount);   
        }

        private void ParseFlagsAndThisAndSuper(int startIndex)
        {
            _accessFlags = ParseTwoByteValue(startIndex);
            _thisClass = ParseTwoByteValue(startIndex + 2);
            _superClass = ParseTwoByteValue(startIndex + 4);
        } 
        
        private List<ushort> GetInterfaces(ushort count, int startIndex)
        {
            var parser = new InterfacesParser(count);
            var fragment = new ArraySegment<byte>(content, startIndex, count * 2).Array;
            parser.Parse(fragment);

            return parser.Pool;
        }

        private List<IInfoObject> GetConstantPool(ushort count)
        {
            var parser = new ConstantPoolParser(count);
            var fragment = new ArraySegment<byte>(content, 10, content.Length - 11).Array;
            constantPoolByteCount = parser.Parse(fragment);
            
            return parser.Pool;
        }

        private ushort ParseTwoByteValue(int start) 
            => (ushort) (content[start] << 8 + content[start + 1]);
    }

    public class ConstantPoolParser : IPoolParser<IInfoObject>
    {
        private readonly ushort _poolCount;
        public List<IInfoObject> Pool { get; }
        

        public ConstantPoolParser(ushort count)
        {
            _poolCount = count;
            Pool = new List<IInfoObject>(_poolCount);
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

        private static Record<string> ParseUtfStrings(IReadOnlyList<byte> fragment, int index, int length)
        {
            index += 2;
            var builder = new StringBuilder();
            for (var i = index; i < index + length;) // Тут нужно бросать ошибки при встрече непонятных байтов
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

        private Reference ParseReference(byte[] fragment, int index)
            => new Reference((ushort) (fragment[index] << 8 + fragment[index + 1]));

        private Record<int> ParseIntegerConstant(byte[] fragment, int index) // Точно ли правильно парсится?
            => new Record<int>(fragment[index] << 24 + fragment[index + 1] << 16 +
                fragment[index + 2] << 8 + fragment[index + 3]);

        private Record<float> ParseFloatConstant(byte[] fragment, int index) // Точно ли правильно парсится?
            => new Record<float>(fragment[index] << 24 + fragment[index + 1] << 16 +
                fragment[index + 2] << 8 + fragment[index + 3]);

        private Tuple<Record<int>, Record<int>> ParseLongOrDoubleConstant(byte[] fragment, int index)
            => Tuple.Create(ParseIntegerConstant(fragment, index), ParseIntegerConstant(fragment, index + 4));

        public int Parse(byte[] fragment)
        {
            int index;
            for (index = 0; index < fragment.Length || _poolCount - Pool.Count > 0; index++) 
            {
                switch ((Tags) fragment[index])
                { // Здесь всем методам будем передавать index + 1
                    case Tags.CONSTANT_Class:
                        // parse 2byte name_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                    case Tags.CONSTANT_Fieldref:
                    case Tags.CONSTANT_Methodref:
                    case Tags.CONSTANT_InterfaceMethodref:
                        // u2 class_index
                        // u2 name_and_type_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                    case Tags.CONSTANT_String:
                        // u2 string_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                    case Tags.CONSTANT_Integer:
                        // u4 bytes
                        Pool.Add(ParseIntegerConstant(fragment, index + 1));
                        index += 4;
                        break;
                    case Tags.CONSTANT_Float: // Не знаю, что будет с float, попробую не пользоваться формулой s * m * 2**(e-150)
                        // u4 bytes
                        Pool.Add(ParseFloatConstant(fragment, index + 1));
                        index += 4;
                        break;
                    case Tags.CONSTANT_Long:
                    case Tags.CONSTANT_Double:
                        var pair = ParseLongOrDoubleConstant(fragment, index + 1);
                        Pool.Add(pair.Item1);
                        Pool.Add(pair.Item2);
                        index += 8;
                        break;
                    case Tags.CONSTANT_NameAndType:
                        // u2 name_index
                        // u2 descriptor_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                    case Tags.CONSTANT_Utf8:
                        var length = (ushort) (fragment[index + 1] << 8 + fragment[index + 2]); // u2 length
                        // Здесь получим строку для записи в пул констант
                        Pool.Add(ParseUtfStrings(fragment, index + 3, length)); 
                        // Прочитали длину и саму строку
                        index += 2 + length;
                        break;
                    case Tags.CONSTANT_MethodHandle:
                        // u1 reference_kind in range(1 to 9) 
                        // u2 reference_index
                        Pool.Add(new Record<byte>(fragment[index + 1])); // Надо бы валидацию
                        index++;
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                    case Tags.CONSTANT_MethodType:
                        // u2 descriptor_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                    case Tags.CONSTANT_InvokeDynamic:
                        // u2 bootstrap_method_attr_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        // u2 name_and_type_index
                        Pool.Add(ParseReference(fragment, index + 1));
                        index += 2;
                        break;
                }
            }

            return index;
        }
    }

    public class InterfacesParser : IPoolParser<ushort>
    {
        public List<ushort> Pool { get; }

        public InterfacesParser(ushort poolCount)
        {
            Pool = new List<ushort>(poolCount);
        }
        
        public int Parse(byte[] fragment)
        {
            int index;
            for (index = 0; index < fragment.Length - 1; index += 2) 
                Pool.Add((ushort) (fragment[index] << 8 + fragment[index + 1]));

            if (index != fragment.Length / 2) throw new Exception("Ошибка с интерфейсами");
            
            return index;
        }

    }

    public class AttributesParser
    {
        
    }
}