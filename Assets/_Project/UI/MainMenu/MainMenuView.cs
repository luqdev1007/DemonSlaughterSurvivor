using UnityEngine;
using UnityEngine.UI;

namespace DemonSlaughter.UI.MainMenu
{
    public sealed class MainMenuView : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;

        public Button NewGameButton => _newGameButton;
    }
}