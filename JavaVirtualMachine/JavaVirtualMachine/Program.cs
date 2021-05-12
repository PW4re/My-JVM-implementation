using System;

namespace JavaVirtualMachine
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new ClassFileParser(@"G:\МАТМЕХ\My JVM implementation\JVMTemplates\out\production\JVMTemplates\MainTemplate.class");
            var res = parser.Parse();
            Console.WriteLine(res.MinorVersion);
            Console.WriteLine(res.MajorVersion);
            Console.WriteLine(res.ConstantPoolCount);
            Console.WriteLine(res.ConstantPoolTable);
        }
    }
}