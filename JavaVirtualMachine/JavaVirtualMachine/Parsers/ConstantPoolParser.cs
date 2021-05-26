using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace JavaVirtualMachine.Parsers
{
    internal class ConstantPoolParser : IPoolParser<IConstant>
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

                codePoint = (char) (0x10000 + ((fragment[i] & 0x0f) << 16) +
                                    ((fragment[i + 1] & 0x3f) << 10) +
                                    ((fragment[i + 2] & 0x0f) << 6) + (fragment[i + 3] & 0x3f));
                builder.Append(codePoint);
                i += 6;
            } // u1 bytes[length]

            return new Record<string>(builder.ToString());
        }

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
            
            return new Record<double>(reader.ReadDouble());
        }
        
        public List<IConstant> Parse(MyBinaryReader reader)
        {
            // заполняем первый индекс заглушкой, т.к. здесь нумерация с единицы
            var pool = new List<IConstant>(_poolCount) { null }; 
            while (_poolCount - pool.Count > 0)
            {
                switch ((Tags) reader.ReadByte())
                { // Здесь всем методам будем передавать index + 1
                    case Tags.CONSTANT_Class:
                        // parse 2byte name_index
                        pool.Add(new ClassInfo(reader.ReadUInt16()));
                        break;
                    case Tags.CONSTANT_Fieldref:
                    case Tags.CONSTANT_Methodref:
                    case Tags.CONSTANT_InterfaceMethodref:
                        // u2 class_index
                        // u2 name_and_type_index
                        pool.Add(new FiMeInRef(reader.ReadUInt16(), reader.ReadUInt16()));
                        break;
                    case Tags.CONSTANT_String:
                        // u2 string_index
                        pool.Add(new StringRef(reader.ReadUInt16()));
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
                        pool.Add(new NameAndTypeInfo(reader.ReadUInt16(), reader.ReadUInt16()));
                        break;
                    case Tags.CONSTANT_Utf8:
                        var length = reader.ReadUInt16();
                        var fragment = reader.ReadBytes(length);
                        pool.Add(ParseUtfStrings(fragment, length));
                        break;
                    case Tags.CONSTANT_MethodHandle:
                        // u1 reference_kind in range(1 to 9) 
                        // u2 reference_index
                        pool.Add(new MethodHandle(reader.ReadByte(), reader.ReadUInt16())); // Надо бы валидацию
                        break;
                    case Tags.CONSTANT_MethodType:
                        // u2 descriptor_index
                        pool.Add(new MethodType(reader.ReadUInt16()));
                        break;
                    case Tags.CONSTANT_InvokeDynamic:
                        // u2 bootstrap_method_attr_index
                        // u2 name_and_type_index
                        pool.Add(new InvokeDynamic(reader.ReadUInt16(), reader.ReadUInt16()));
                        break;
                }
            }

            return pool;
        }
    }
}