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

    public class Reference : IConstant
    {
        public ushort Instance { get; }

        public Reference(ushort reference)
        {
            Instance = reference;
        }
    }
}