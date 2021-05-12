namespace JavaVirtualMachine
{
    public interface IInfoObject { }

    public class Record<T> : IInfoObject
    {
        public T Instance { get; }
        
        public Record(T record)
        {
            Instance = record;
        }
    }

    public class Reference : IInfoObject
    {
        public ushort Instance { get; }

        public Reference(ushort reference)
        {
            Instance = reference;
        }
    }
}