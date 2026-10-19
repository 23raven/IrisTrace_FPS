using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using IrisTrace.Editor.Generators;
using IrisTrace.Editor.UI;
using IrisTrace.Runtime.Characters;
using IrisTrace.Runtime.Enemies;
using IrisTrace.Runtime.Weapons;

namespace IrisTrace.Editor.Windows
{
    /// <summary>Lightweight editor diagnostics for common IrisTrace setup issues.</summary>
    public sealed class IrisTraceProjectValidationWindow : EditorWindow
    {
        private const string CharacterRoot = "Assets/IrisTraceFPS_Generated/Configurations/Characters";
        private const string WeaponRoot = "Assets/IrisTraceFPS_Generated/Configurations/Weapons";
        private const string EnemyRoot = "Assets/IrisTraceFPS_Generated/Configurations/Enemies";
        private const string CharacterPrefabRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Characters";
        private const string EnemyPrefabRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Enemies";
        private const string TestScenePath = "Assets/IrisTraceFPS_Generated/Scenes/IrisTrace_TestArena.unity";

        private readonly List<CheckResult> results = new List<CheckResult>();
        private VisualElement resultsContainer;
        private Label summaryLabel;

        private enum CheckStatus { Pass, Warning, Fail }

        private sealed class CheckResult
        {
            public string Title;
            public string Details;
            public CheckStatus Status;
        }

        [MenuItem("Window/IrisTrace FPS/Project Validation")]
        public static void Open()
        {
            IrisTraceProjectValidationWindow window = GetWindow<IrisTraceProjectValidationWindow>("Project Validation");
            window.minSize = new Vector2(420f, 520f);
            window.Show();
        }

        private void OnEnable() => IrisTraceTheme.ThemeChanged += RebuildForTheme;
        private void OnDisable() => IrisTraceTheme.ThemeChanged -= RebuildForTheme;
        private void RebuildForTheme() => CreateGUI();

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = IrisTraceTheme.Background;

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.paddingLeft = 16f;
            scroll.style.paddingRight = 16f;
            scroll.style.paddingTop = 14f;
            scroll.style.paddingBottom = 16f;
            scroll.style.backgroundColor = IrisTraceTheme.Background;
            rootVisualElement.Add(scroll);

            Label title = new Label("Project Validation");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 19f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 3f;
            scroll.Add(title);

            Label subtitle = new Label("QUICK CHECKS FOR COMMON SETUP ISSUES");
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 9f;
            subtitle.style.letterSpacing = 0.8f;
            subtitle.style.marginBottom = 14f;
            scroll.Add(subtitle);

            Button runButton = new Button(RunChecks) { text = "Run Checks" };
            StyleButton(runButton, true);
            scroll.Add(runButton);

            Button arenaButton = new Button(IrisTraceTestArenaBuilder.CreateTestArena) { text = "Create Test Arena" };
            StyleButton(arenaButton, false);
            arenaButton.style.marginTop = 6f;
            scroll.Add(arenaButton);

            summaryLabel = new Label("Run checks to inspect the project.");
            summaryLabel.style.color = IrisTraceTheme.SecondaryText;
            summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            summaryLabel.style.marginTop = 14f;
            summaryLabel.style.marginBottom = 10f;
            scroll.Add(summaryLabel);

            resultsContainer = new VisualElement();
            scroll.Add(resultsContainer);
            DrawResults();
        }

        private void RunChecks()
        {
            results.Clear();
            CheckInputSystem();
            CheckConfigurationAssets();
            CheckGeneratedFolders();
            CheckActiveScene();
            CheckTestScene();
            DrawResults();
        }

        private void CheckInputSystem()
        {
            bool available = Type.GetType("UnityEngine.InputSystem.InputAction, Unity.InputSystem") != null;
            AddResult("Input System package",
                available ? "New Input System type is available." : "Install the Input System package through Package Manager.",
                available ? CheckStatus.Pass : CheckStatus.Fail);
        }

        private void CheckConfigurationAssets()
        {
            List<CharacterDefinition> characters = LoadAssets<CharacterDefinition>(CharacterRoot, "t:CharacterDefinition");
            List<WeaponDefinition> weapons = LoadAssets<WeaponDefinition>(WeaponRoot, "t:WeaponDefinition");
            List<EnemyDefinition> enemies = LoadAssets<EnemyDefinition>(EnemyRoot, "t:EnemyDefinition");

            AddResult("Character configurations",
                characters.Count > 0 ? $"Found {characters.Count} character configuration(s)." : "No saved characters yet; create one from the Hero wizard.",
                characters.Count > 0 ? CheckStatus.Pass : CheckStatus.Warning);
            AddResult("Weapon configurations",
                weapons.Count > 0 ? $"Found {weapons.Count} weapon configuration(s)." : "No saved weapons yet; generate a hero or create one in Weapon Library.",
                weapons.Count > 0 ? CheckStatus.Pass : CheckStatus.Warning);
            AddResult("Enemy configurations",
                enemies.Count > 0 ? $"Found {enemies.Count} enemy configuration(s)." : "No saved enemies yet; create one from the Enemy wizard.",
                enemies.Count > 0 ? CheckStatus.Pass : CheckStatus.Warning);

            HashSet<string> weaponIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool duplicateWeaponIds = false;
            bool missingWeaponIds = false;
            foreach (WeaponDefinition weapon in weapons)
            {
                if (weapon == null || string.IsNullOrWhiteSpace(weapon.Id))
                {
                    missingWeaponIds = true;
                    continue;
                }
                if (!weaponIds.Add(weapon.Id)) duplicateWeaponIds = true;
            }

            AddResult("Weapon IDs",
                duplicateWeaponIds ? "Duplicate weapon IDs found. Duplicate the asset through Weapon Library to create a fresh ID." :
                missingWeaponIds ? "One or more weapons have no ID." : "Weapon IDs are present and unique.",
                duplicateWeaponIds || missingWeaponIds ? CheckStatus.Fail : CheckStatus.Pass);

            bool brokenWeaponLinks = false;
            bool missingCharacterIds = false;
            bool duplicateCharacterIds = false;
            HashSet<string> characterIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CharacterDefinition character in characters)
            {
                if (character == null) continue;
                if (string.IsNullOrWhiteSpace(character.Id)) missingCharacterIds = true;
                else if (!characterIds.Add(character.Id)) duplicateCharacterIds = true;

                if (string.IsNullOrWhiteSpace(character.PrimaryWeaponId) || !weaponIds.Contains(character.PrimaryWeaponId))
                    brokenWeaponLinks = true;
            }

            AddResult("Character IDs",
                duplicateCharacterIds ? "Duplicate character IDs found." :
                missingCharacterIds ? "One or more characters have no ID." :
                characters.Count == 0 ? "No character IDs to inspect yet." : "Character IDs are present and unique.",
                duplicateCharacterIds || missingCharacterIds ? CheckStatus.Fail : characters.Count == 0 ? CheckStatus.Warning : CheckStatus.Pass);

            AddResult("Character → weapon links",
                brokenWeaponLinks ? "At least one character points to a missing weapon ID. Check the Character and Weapon Libraries." :
                characters.Count == 0 ? "No character links to inspect yet." : "All saved characters reference an existing weapon ID.",
                brokenWeaponLinks ? CheckStatus.Warning : characters.Count == 0 ? CheckStatus.Warning : CheckStatus.Pass);

            AddResult("Enemy IDs",
                HasMissingOrDuplicateIds(enemies, x => x != null ? x.Id : null)
                    ? "One or more enemies have a missing or duplicate ID."
                    : enemies.Count == 0 ? "No enemy IDs to inspect yet." : "Enemy IDs are present and unique.",
                enemies.Count == 0 ? CheckStatus.Warning : HasMissingOrDuplicateIds(enemies, x => x != null ? x.Id : null) ? CheckStatus.Fail : CheckStatus.Pass);
        }

        private void CheckGeneratedFolders()
        {
            bool characterPrefabs = AssetDatabase.IsValidFolder(CharacterPrefabRoot);
            bool enemyPrefabs = AssetDatabase.IsValidFolder(EnemyPrefabRoot);
            AddResult("Generated prefab folders",
                characterPrefabs && enemyPrefabs ? "Character and enemy prefab folders exist." : "Some prefab folders will be created automatically during generation.",
                characterPrefabs && enemyPrefabs ? CheckStatus.Pass : CheckStatus.Warning);
        }

        private void CheckActiveScene()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            int mainCameraCount = 0;
            Camera[] cameras = Resources.FindObjectsOfTypeAll<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.enabled || camera.gameObject.scene != activeScene) continue;
                if (camera.CompareTag("MainCamera")) mainCameraCount++;
            }

            int audioListenerCount = 0;
            AudioListener[] listeners = Resources.FindObjectsOfTypeAll<AudioListener>();
            foreach (AudioListener listener in listeners)
                if (listener != null && listener.enabled && listener.gameObject.scene == activeScene)
                    audioListenerCount++;

            AddResult("Active scene camera",
                mainCameraCount > 1 ? $"Found {mainCameraCount} enabled Main Cameras. Keep only one active camera." :
                mainCameraCount == 0 ? "No enabled Main Camera found in the active scene." : "Exactly one enabled Main Camera is present.",
                mainCameraCount > 1 ? CheckStatus.Fail : mainCameraCount == 0 ? CheckStatus.Warning : CheckStatus.Pass);

            AddResult("Audio Listener",
                audioListenerCount > 1 ? $"Found {audioListenerCount} enabled Audio Listeners. Keep only one active listener." :
                audioListenerCount == 0 ? "No enabled Audio Listener found in the active scene." : "Exactly one enabled Audio Listener is present.",
                audioListenerCount > 1 ? CheckStatus.Fail : audioListenerCount == 0 ? CheckStatus.Warning : CheckStatus.Pass);
        }

        private void CheckTestScene()
        {
            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null;
            AddResult("Integration test scene",
                exists ? "IrisTrace_TestArena.unity is available." : "Test arena has not been created yet.",
                exists ? CheckStatus.Pass : CheckStatus.Warning);
        }

        private static List<T> LoadAssets<T>(string root, string filter) where T : UnityEngine.Object
        {
            List<T> assets = new List<T>();
            if (!AssetDatabase.IsValidFolder(root)) return assets;
            foreach (string guid in AssetDatabase.FindAssets(filter, new[] { root }))
            {
                T asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) assets.Add(asset);
            }
            return assets;
        }

        private static bool HasMissingOrDuplicateIds<T>(IList<T> items, Func<T, string> idSelector)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (T item in items)
            {
                string id = idSelector(item);
                if (string.IsNullOrWhiteSpace(id) || !ids.Add(id)) return true;
            }
            return false;
        }

        private void AddResult(string title, string details, CheckStatus status)
        {
            results.Add(new CheckResult { Title = title, Details = details, Status = status });
        }

        private void DrawResults()
        {
            if (resultsContainer == null || summaryLabel == null) return;
            resultsContainer.Clear();
            if (results.Count == 0)
            {
                summaryLabel.text = "Run checks to inspect the project.";
                return;
            }

            int passed = 0, warnings = 0, failed = 0;
            foreach (CheckResult result in results)
            {
                if (result.Status == CheckStatus.Pass) passed++;
                else if (result.Status == CheckStatus.Warning) warnings++;
                else failed++;

                VisualElement card = new VisualElement();
                card.style.backgroundColor = IrisTraceTheme.Surface;
                card.style.paddingLeft = 11f;
                card.style.paddingRight = 11f;
                card.style.paddingTop = 9f;
                card.style.paddingBottom = 9f;
                card.style.marginBottom = 6f;
                SetRadius(card, 8f);
                SetBorder(card, IrisTraceTheme.Border);

                Label heading = new Label(StatusText(result.Status) + "  " + result.Title);
                heading.style.color = StatusColor(result.Status);
                heading.style.fontSize = 11f;
                heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                heading.style.marginBottom = 3f;

                Label detail = new Label(result.Details);
                detail.style.color = IrisTraceTheme.SecondaryText;
                detail.style.fontSize = 10f;
                detail.style.whiteSpace = WhiteSpace.Normal;
                card.Add(heading);
                card.Add(detail);
                resultsContainer.Add(card);
            }

            summaryLabel.text = $"{passed} passed   •   {warnings} warnings   •   {failed} failed";
            summaryLabel.style.color = failed > 0 ? StatusColor(CheckStatus.Fail) : warnings > 0 ? StatusColor(CheckStatus.Warning) : IrisTraceTheme.Accent;
        }

        private static string StatusText(CheckStatus status)
        {
            switch (status)
            {
                case CheckStatus.Pass: return "PASS";
                case CheckStatus.Warning: return "WARN";
                default: return "FAIL";
            }
        }

        private static Color StatusColor(CheckStatus status)
        {
            switch (status)
            {
                case CheckStatus.Pass: return IrisTraceTheme.Accent;
                case CheckStatus.Warning: return new Color(0.95f, 0.67f, 0.22f, 1f);
                default: return new Color(0.90f, 0.31f, 0.28f, 1f);
            }
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.style.height = 34f;
            button.style.marginBottom = 7f;
            button.style.backgroundColor = primary ? IrisTraceTheme.Accent : IrisTraceTheme.SurfaceStrong;
            button.style.color = primary ? IrisTraceTheme.AccentText : IrisTraceTheme.PrimaryText;
            button.style.fontSize = 11f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetRadius(button, 8f);
            SetBorder(button, primary ? Color.clear : IrisTraceTheme.Border);
        }

        private static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private static void SetBorder(VisualElement element, Color color)
        {
            float width = color.a <= 0f ? 0f : 1f;
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }
    }
}
