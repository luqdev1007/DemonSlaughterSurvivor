namespace DemonSlaughter.Core.Save
{
    public interface ISaveService
    {
        SaveData Load();

        void Save(SaveData saveData);

        bool HasSave();
    }
}