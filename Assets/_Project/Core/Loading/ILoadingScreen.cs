using Cysharp.Threading.Tasks;

namespace DemonSlaughter.Core.Loading
{
    public interface ILoadingScreen
    {
        UniTask ShowAsync();
        UniTask HideAsync();
        void SetProgress(float progress); 
    }
}