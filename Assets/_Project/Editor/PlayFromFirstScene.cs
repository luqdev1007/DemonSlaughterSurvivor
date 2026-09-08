using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Game.Editor
{
    [InitializeOnLoad]
    public static class PlayFromFirstScene
    {
        private const string MenuPath = "Game/Play From First Scene";
        private const string PrefKeyPrefix = "Game.PlayFromFirstScene.";

        static PlayFromFirstScene()
        {
            EditorApplication.delayCall += Apply;
            EditorBuildSettings.sceneListChanged += Apply;
        }

        [MenuItem(MenuPath, false, 100)]
        private static void Toggle()
        {
            SetEnabled(IsEnabled == false);
        }

        [MenuItem(MenuPath, true, 100)]
        private static bool ToggleValidate()
        {
            Menu.SetChecked(MenuPath, IsEnabled);

            return true;
        }

        private static bool IsEnabled
        {
            get => EditorPrefs.GetBool(PrefKey, true);
            set => EditorPrefs.SetBool(PrefKey, value);
        }

        private static string PrefKey => PrefKeyPrefix + Application.dataPath;

        private static void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            Apply();

            Debug.Log(enabled
                ? $"Play mode will start from '{FirstScenePath()}'."
                : "Play mode will start from the currently open scene.");
        }

        private static void Apply()
        {
            if (IsEnabled == false)
            {
                EditorSceneManager.playModeStartScene = null;

                return;
            }

            string path = FirstScenePath();

            if (string.IsNullOrEmpty(path))
            {
                EditorSceneManager.playModeStartScene = null;

                Debug.LogWarning(
                    $"{nameof(PlayFromFirstScene)}: Build Settings contain no enabled scene, " +
                    "play mode will start from the currently open scene.");

                return;
            }

            SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);

            if (scene == null)
            {
                EditorSceneManager.playModeStartScene = null;

                Debug.LogWarning(
                    $"{nameof(PlayFromFirstScene)}: first Build Settings scene '{path}' is missing, " +
                    "play mode will start from the currently open scene.");

                return;
            }

            EditorSceneManager.playModeStartScene = scene;
        }

        private static string FirstScenePath()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.enabled == false)
                {
                    continue;
                }

                return scene.path;
            }

            return string.Empty;
        }
    }
}
