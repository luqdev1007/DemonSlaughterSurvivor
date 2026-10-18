namespace Game.Core
{
    public interface IHeroSelection
    {
        string SelectedCharacterId { get; }

        void SelectCharacter(string characterId);
    }
}
