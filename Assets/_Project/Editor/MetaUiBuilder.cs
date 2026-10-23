using Game.UI;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace Game.Editor
{
    public static class MetaUiBuilder
    {
        private const string FontSource = "Assets/_Project/Art/ThirdParty/GoogleFonts/AlegreyaSans/AlegreyaSans-Regular.ttf";
        private const string FontAssetPath = "Assets/_Project/Art/Fonts/AlegreyaSans SDF.asset";
        private const string TitleFontPath = "Assets/_Project/Art/Fonts/CormorantSC-Bold SDF.asset";
        private const string MetaSprites = "Assets/_Project/Art/Sprites/Meta/";
        private const string GuiParts = "Assets/GUI_Parts/Gui_parts/";
        private const string StylePath = "Assets/_Project/Prefabs/UI/MetaUiStyle.asset";
        private const string HudPath = "Assets/_Project/Prefabs/UI/Hud.prefab";
        private const int RunEndSortingOrder = 1100;
        private const int HubSortingOrder = 500;
        private const string MainMenuPath = "Assets/_Project/Scenes/MainMenu.unity";

        private const string Characters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "«»—–…·×−±→←↑↓©®°№ ◆■□▲";

        [MenuItem("Game/Meta/Build Body Font")]
        public static void BuildFont()
        {
            Font font = AssetDatabase.LoadAssetAtPath<Font>(FontSource);

            if (font == null)
            {
                Debug.LogError("Meta UI: font source missing at " + FontSource);

                return;
            }

            TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 72, 8, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, false);
            asset.name = Path.GetFileNameWithoutExtension(FontAssetPath);

            if (asset.TryAddCharacters(Characters, out string missing) == false)
                Debug.LogWarning("Meta UI: glyphs missing in Alegreya Sans: " + missing);

            asset.atlasPopulationMode = AtlasPopulationMode.Static;

            AssetDatabase.DeleteAsset(FontAssetPath);
            AssetDatabase.CreateAsset(asset, FontAssetPath);

            Texture2D atlas = asset.atlasTextures[0];
            atlas.name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(atlas, asset);

            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();

            Debug.Log($"Meta UI: built {FontAssetPath} with {asset.characterTable.Count} characters.");
        }

        [MenuItem("Game/Meta/Build UI Style")]
        public static void BuildStyle()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { MetaSprites.TrimEnd('/') }))
                MakeSprite(AssetDatabase.GUIDToAssetPath(guid));

            MetaUiStyle style = AssetDatabase.LoadAssetAtPath<MetaUiStyle>(StylePath);

            if (style == null)
            {
                style = ScriptableObject.CreateInstance<MetaUiStyle>();
                AssetDatabase.CreateAsset(style, StylePath);
            }

            SerializedObject serialized = new SerializedObject(style);
            Set(serialized, "_titleFont", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TitleFontPath));
            Set(serialized, "_bodyFont", AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath));
            Set(serialized, "_panelBack", Gui("mid_background"));
            Set(serialized, "_panelFrame", Gui("Frame_mid"));
            Set(serialized, "_cardBack", Gui("Mini_background"));
            Set(serialized, "_cardFrame", Gui("Mini_frame0"));
            Set(serialized, "_button", Gui("button_ready_on"));
            Set(serialized, "_buttonOff", Gui("button_ready_off"));
            Set(serialized, "_roundFrame", Gui("lil_roundframe"));
            Set(serialized, "_roundBack", Gui("lil_roundbackground"));
            Set(serialized, "_bar", Gui("name_bar"));
            Set(serialized, "_memoryIcon", Meta("icon_memory"));
            Set(serialized, "_magicIcon", Meta("icon_magic"));
            Set(serialized, "_relicIcon", Meta("icon_relic"));
            Set(serialized, "_relicOffIcon", Meta("icon_relic_off"));
            Set(serialized, "_redGem", Meta("gem_red"));
            Set(serialized, "_blueGem", Meta("gem_blue"));
            Set(serialized, "_greenGem", Meta("gem_green"));
            Set(serialized, "_softGlow", Meta("soft_glow"));
            Set(serialized, "_circle", Meta("shape_circle"));
            Set(serialized, "_circleRing", Meta("shape_circle_ring"));
            Set(serialized, "_square", Meta("shape_square"));
            Set(serialized, "_squareRing", Meta("shape_square_ring"));
            Set(serialized, "_diamond", Meta("shape_diamond"));
            Set(serialized, "_diamondRing", Meta("shape_diamond_ring"));
            Set(serialized, "_bladeArt", Meta("weapon_blade"));
            Set(serialized, "_axeArt", Meta("weapon_axe"));
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();

            Debug.Log("Meta UI: style built, wired = " + style.IsWired);
        }

        [MenuItem("Game/Meta/Build Run End Screen")]
        public static void BuildRunEnd()
        {
            MetaUiStyle style = AssetDatabase.LoadAssetAtPath<MetaUiStyle>(StylePath);
            GameObject hud = PrefabUtility.LoadPrefabContents(HudPath);

            try
            {
                Transform old = hud.transform.Find("RunEnd");

                if (old != null)
                    Object.DestroyImmediate(old.gameObject);

                GameObject root = CreateOverlay("RunEnd", hud.transform, RunEndSortingOrder);

                RunEndView view = root.AddComponent<RunEndView>();
                UiFade fade = root.AddComponent<UiFade>();

                SerializedObject fadeSerialized = new SerializedObject(fade);
                Set(fadeSerialized, "_group", root.GetComponent<CanvasGroup>());
                fadeSerialized.ApplyModifiedPropertiesWithoutUndo();

                SerializedObject viewSerialized = new SerializedObject(view);
                Set(viewSerialized, "_style", style);
                Set(viewSerialized, "_fade", fade);
                viewSerialized.ApplyModifiedPropertiesWithoutUndo();

                HudView hudView = hud.GetComponent<HudView>();
                SerializedObject hudSerialized = new SerializedObject(hudView);
                Set(hudSerialized, "_runEnd", view);
                hudSerialized.ApplyModifiedPropertiesWithoutUndo();

                root.SetActive(false);

                PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hud);
            }

            Debug.Log("Meta UI: run end screen built into " + HudPath);
        }

        [MenuItem("Game/Meta/Build Hub In Main Menu")]
        public static void BuildHub()
        {
            UnityEngine.SceneManagement.Scene scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(MainMenuPath);
            MetaUiStyle style = AssetDatabase.LoadAssetAtPath<MetaUiStyle>(StylePath);

            MainMenuView menu = Object.FindAnyObjectByType<MainMenuView>(FindObjectsInactive.Include);
            Transform play = menu.transform.Find("PlayButton");
            Transform upgrade = menu.transform.Find("UpgradeButton");

            if (upgrade == null)
            {
                upgrade = Object.Instantiate(play.gameObject, menu.transform).transform;
                upgrade.name = "UpgradeButton";
                upgrade.SetSiblingIndex(play.GetSiblingIndex() + 1);
            }

            upgrade.GetComponentInChildren<TMP_Text>(true).text = "ПРОКАЧКА";

            string[] order = { "PlayButton", "UpgradeButton", "SettingsButton", "QuitButton" };

            for (int index = 0; index < order.Length; index++)
            {
                RectTransform button = (RectTransform)menu.transform.Find(order[index]);
                button.anchoredPosition = new Vector2((index - 1.5f) * 221f, button.anchoredPosition.y);
            }

            foreach (MetaHubView existing in Object.FindObjectsByType<MetaHubView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Object.DestroyImmediate(existing.gameObject);

            GameObject root = CreateOverlay("MetaHub", null, HubSortingOrder);
            MetaHubView hub = root.AddComponent<MetaHubView>();
            UiFade fade = root.AddComponent<UiFade>();

            SerializedObject fadeSerialized = new SerializedObject(fade);
            Set(fadeSerialized, "_group", root.GetComponent<CanvasGroup>());
            fadeSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject hubSerialized = new SerializedObject(hub);
            Set(hubSerialized, "_style", style);
            Set(hubSerialized, "_fade", fade);
            hubSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject menuSerialized = new SerializedObject(menu);
            Set(menuSerialized, "_upgradeButton", upgrade.GetComponent<Button>());
            Set(menuSerialized, "_hub", hub);
            menuSerialized.ApplyModifiedPropertiesWithoutUndo();

            root.SetActive(false);

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);

            Debug.Log("Meta UI: hub built into " + MainMenuPath);
        }

        public static GameObject CreateOverlay(string name, Transform parent, int sortingOrder)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));

            if (parent != null)
                root.transform.SetParent(parent, false);

            root.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.overrideSorting = true;
            canvas.sortingOrder = sortingOrder;

            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            return root;
        }

        private static void MakeSprite(string path)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null || importer.textureType == TextureImporterType.Sprite)
                return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        private static Sprite Gui(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(GuiParts + name + ".png");
        }

        private static Sprite Meta(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>(MetaSprites + name + ".png");
        }

        private static void Set(SerializedObject serialized, string property, Object value)
        {
            SerializedProperty found = serialized.FindProperty(property);

            if (found == null)
            {
                Debug.LogError($"Meta UI: no property {property} on {serialized.targetObject.GetType().Name}");

                return;
            }

            found.objectReferenceValue = value;
        }
    }
}
