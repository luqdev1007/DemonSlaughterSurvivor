namespace Game.Core
{
    public interface ISaveStorage
    {
        bool TryRead(string fileName, out string content);

        void Write(string fileName, string content);

        void Delete(string fileName);
    }
}
