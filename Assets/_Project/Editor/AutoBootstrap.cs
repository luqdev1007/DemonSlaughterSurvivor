using UnityEditor;
using UnityEditor.SceneManagement;

namespace DemonSlaughter.Editor
{
    [InitializeOnLoad]
    public static class AutoBootstrap
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string MenuItemPath = "DemonSlaughter/Auto Bootstrap";
        private const string EditorPrefKey = "AutoBootstrap_Enabled";

        static AutoBootstrap()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem(MenuItemPath)]
        private static void ToggleAutoBootstrap()
        {
            var enabled = IsEnabled();
            SetEnabled(!enabled);
        }

        [MenuItem(MenuItemPath, true)]
        private static bool ToggleAutoBootstrapValidate()
        {
            Menu.SetChecked(MenuItemPath, IsEnabled());
            return true;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!IsEnabled()) return;

            if (state == PlayModeStateChange.ExitingEditMode)
            {
                EditorPrefs.SetString("AutoBootstrap_PreviousScene",
                    EditorSceneManager.GetActiveScene().path);

                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene(BootstrapScenePath);
            }

            if (state == PlayModeStateChange.EnteredEditMode)
            {
                var previousScene = EditorPrefs.GetString("AutoBootstrap_PreviousScene");

                if (!string.IsNullOrEmpty(previousScene))
                    EditorSceneManager.OpenScene(previousScene);
            }
        }

        private static bool IsEnabled() =>
            EditorPrefs.GetBool(EditorPrefKey, true);

        private static void SetEnabled(bool value) =>
            EditorPrefs.SetBool(EditorPrefKey, value);
    }
}