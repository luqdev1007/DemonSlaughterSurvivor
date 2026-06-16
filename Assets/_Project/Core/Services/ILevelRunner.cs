using Cysharp.Threading.Tasks;

namespace DemonSlaughter.Core.Services
{
    public interface ILevelRunner
    {
        UniTask RunLevel(int levelId);
    }
}