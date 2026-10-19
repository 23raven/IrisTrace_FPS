using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using IrisTrace.Editor.Generators;
using IrisTrace.Editor.UI;
using IrisTrace.Runtime.Enemies;

namespace IrisTrace.Editor.Windows
{
    /// <summary>Browse, edit, duplicate, and generate enemies from saved configurations.</summary>
    public sealed class IrisTraceEnemyLibraryWindow : EditorWindow
    {
        private const string EnemyConfigRoot = "Assets/IrisTraceFPS_Generated/Configurations/Enemies";
        private const string EnemyPrefabRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Enemies";

        private TextField searchField;
        private VisualElement listContainer;
        private VisualElement detailsContainer;
        private EnemyDefinition selected;
        private readonly List<EnemyDefinition> allEnemies = new List<EnemyDefinition>();

        private TextField nameField;
        private EnumField behaviorField;
        private FloatField healthField;
        private FloatField moveSpeedField;
        private FloatField detectionRangeField;
        private FloatField attackRangeField;
        private FloatField attackDamageField;
        private FloatField attackCooldownField;

        [MenuItem("Window/IrisTrace FPS/Enemy Library")]
        public static void Open()
        {
            IrisTraceEnemyLibraryWindow window = GetWindow<IrisTraceEnemyLibraryWindow>("Enemy Library");
            window.minSize = new Vector2(430f, 560f);
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = IrisTraceTheme.Background;

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.backgroundColor = IrisTraceTheme.Background;
            scroll.style.paddingLeft = 16f;
            scroll.style.paddingRight = 16f;
            scroll.style.paddingTop = 14f;
            scroll.style.paddingBottom = 16f;
            rootVisualElement.Add(scroll);

            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 12f;

            VisualElement titleStack = new VisualElement();
            titleStack.style.flexGrow = 1f;
            Label title = new Label("Enemy Library");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 18f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label subtitle = new Label("BROWSE AND REUSE ENEMY CONFIGURATIONS");
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 9f;
            subtitle.style.letterSpacing = 0.7f;
            titleStack.Add(title);
            titleStack.Add(subtitle);
            header.Add(titleStack);

            Button createButton = new Button(IrisTraceEnemyWizardWindow.Open) { text = "+ New Enemy" };
            StyleButton(createButton, true);
            header.Add(createButton);
            scroll.Add(header);

            searchField = new TextField { value = string.Empty };
            searchField.label = "Search by name or ID";
            StyleField(searchField);
            searchField.RegisterValueChangedCallback(_ => RefreshList());
            scroll.Add(searchField);

            Button refreshButton = new Button(RefreshList) { text = "Refresh Library" };
            StyleButton(refreshButton, false);
            refreshButton.style.marginBottom = 12f;
            scroll.Add(refreshButton);

            listContainer = new VisualElement();
            listContainer.style.marginBottom = 14f;
            scroll.Add(listContainer);

            detailsContainer = new VisualElement();
            scroll.Add(detailsContainer);

            RefreshList();
        }

        private void RefreshList()
        {
            if (listContainer == null || detailsContainer == null)
                return;

            allEnemies.Clear();
            if (AssetDatabase.IsValidFolder(EnemyConfigRoot))
            {
                string[] guids = AssetDatabase.FindAssets("t:EnemyDefinition", new[] { EnemyConfigRoot });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    EnemyDefinition definition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(path);
                    if (definition != null)
                        allEnemies.Add(definition);
                }
            }

            allEnemies.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            listContainer.Clear();
            detailsContainer.Clear();

            string query = searchField != null ? searchField.value.Trim() : string.Empty;
            int visibleCount = 0;
            foreach (EnemyDefinition definition in allEnemies)
            {
                string displayName = definition.DisplayName ?? string.Empty;
                string id = definition.Id ?? string.Empty;
                if (!string.IsNullOrEmpty(query) &&
                    displayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    id.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                EnemyDefinition captured = definition;
                Button row = new Button(() => SelectEnemy(captured));
                row.text = $"{displayName}\n{ShortId(id)}  •  {definition.Behavior}  /  {definition.MaxHealth:0} HP";
                row.style.height = 48f;
                row.style.marginBottom = 5f;
                row.style.unityTextAlign = TextAnchor.MiddleLeft;
                row.style.whiteSpace = WhiteSpace.Normal;
                row.style.backgroundColor = selected == definition ? IrisTraceTheme.AccentHover : IrisTraceTheme.Surface;
                row.style.color = selected == definition ? IrisTraceTheme.AccentText : IrisTraceTheme.PrimaryText;
                SetRadius(row, 8f);
                listContainer.Add(row);
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                Label empty = new Label(allEnemies.Count == 0
                    ? "No enemy configurations yet. Create an enemy to add the first configuration."
                    : "No enemies match this search.");
                empty.style.color = IrisTraceTheme.SecondaryText;
                empty.style.whiteSpace = WhiteSpace.Normal;
                empty.style.marginTop = 8f;
                empty.style.marginBottom = 12f;
                listContainer.Add(empty);
            }

            if (selected != null && !allEnemies.Contains(selected))
                selected = null;
            DrawDetails();
        }

        private void SelectEnemy(EnemyDefinition definition)
        {
            selected = definition;
            RefreshList();
        }

        private void DrawDetails()
        {
            detailsContainer.Clear();
            if (selected == null)
                return;

            VisualElement panel = new VisualElement();
            panel.style.backgroundColor = IrisTraceTheme.Surface;
            panel.style.paddingLeft = 13f;
            panel.style.paddingRight = 13f;
            panel.style.paddingTop = 12f;
            panel.style.paddingBottom = 12f;
            panel.style.marginTop = 3f;
            SetRadius(panel, 10f);
            SetBorder(panel, IrisTraceTheme.Border);

            Label heading = new Label("Enemy Settings");
            heading.style.color = IrisTraceTheme.PrimaryText;
            heading.style.fontSize = 15f;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 5f;
            panel.Add(heading);

            Label idLabel = new Label("ID: " + (string.IsNullOrEmpty(selected.Id) ? "Missing ID" : selected.Id));
            idLabel.style.color = IrisTraceTheme.SecondaryText;
            idLabel.style.fontSize = 10f;
            idLabel.style.whiteSpace = WhiteSpace.Normal;
            idLabel.style.marginBottom = 8f;
            panel.Add(idLabel);

            nameField = new TextField("Display Name") { value = selected.DisplayName };
            behaviorField = new EnumField("Behavior", selected.Behavior);
            healthField = new FloatField("Max Health") { value = selected.MaxHealth };
            moveSpeedField = new FloatField("Move Speed") { value = selected.MoveSpeed };
            detectionRangeField = new FloatField("Detection Range") { value = selected.DetectionRange };
            attackRangeField = new FloatField("Attack Range") { value = selected.AttackRange };
            attackDamageField = new FloatField("Attack Damage") { value = selected.AttackDamage };
            attackCooldownField = new FloatField("Attack Cooldown (s)") { value = selected.AttackCooldown };

            AddField(panel, nameField);
            AddField(panel, behaviorField);
            AddField(panel, healthField);
            AddField(panel, moveSpeedField);
            AddField(panel, detectionRangeField);
            AddField(panel, attackRangeField);
            AddField(panel, attackDamageField);
            AddField(panel, attackCooldownField);

            Label hint = new Label("AI values are used when Behavior is set to BasicAI.");
            hint.style.color = IrisTraceTheme.SecondaryText;
            hint.style.fontSize = 10f;
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.marginTop = 2f;
            panel.Add(hint);

            VisualElement actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexWrap = Wrap.Wrap;
            actions.style.marginTop = 10f;

            Button save = new Button(SaveChanges) { text = "Save Changes" };
            StyleButton(save, true);
            save.style.marginRight = 6f;
            actions.Add(save);

            Button generate = new Button(GenerateFromSelected) { text = "Generate Prefab" };
            StyleButton(generate, false);
            generate.style.marginRight = 6f;
            actions.Add(generate);

            Button duplicate = new Button(DuplicateSelected) { text = "Duplicate" };
            StyleButton(duplicate, false);
            duplicate.style.marginRight = 6f;
            actions.Add(duplicate);

            Button copyId = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = selected != null ? selected.Id ?? string.Empty : string.Empty;
                ShowNotification(new GUIContent("ID copied"));
            }) { text = "Copy ID" };
            StyleButton(copyId, false);
            copyId.style.marginRight = 6f;
            actions.Add(copyId);

            Button locate = new Button(LocatePrefab) { text = "Find Prefab" };
            StyleButton(locate, false);
            actions.Add(locate);

            panel.Add(actions);
            detailsContainer.Add(panel);
        }

        private void SaveChanges()
        {
            if (selected == null || nameField == null)
                return;

            if (string.IsNullOrWhiteSpace(nameField.value))
            {
                EditorUtility.DisplayDialog("Missing Name", "Enter a display name for the enemy.", "OK");
                return;
            }

            Undo.RecordObject(selected, "Edit Enemy Definition");
            SerializedObject serialized = new SerializedObject(selected);
            SetString(serialized, "displayName", nameField.value.Trim());
            SetStringIfEmpty(serialized, "id", Guid.NewGuid().ToString("N"));
            SetEnum(serialized, "behavior", (EnemyBehaviorMode)behaviorField.value);
            SetFloat(serialized, "maxHealth", Mathf.Max(1f, healthField.value));
            SetFloat(serialized, "moveSpeed", Mathf.Max(0.1f, moveSpeedField.value));
            SetFloat(serialized, "detectionRange", Mathf.Max(0.1f, detectionRangeField.value));
            SetFloat(serialized, "attackRange", Mathf.Max(0.5f, attackRangeField.value));
            SetFloat(serialized, "attackDamage", Mathf.Max(0f, attackDamageField.value));
            SetFloat(serialized, "attackCooldown", Mathf.Max(0.1f, attackCooldownField.value));
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(selected);

            string assetPath = AssetDatabase.GetAssetPath(selected);
            string targetName = SanitizeFileName(nameField.value.Trim());
            if (!string.IsNullOrEmpty(assetPath) && Path.GetFileNameWithoutExtension(assetPath) != targetName)
            {
                string error = AssetDatabase.RenameAsset(assetPath, targetName);
                if (!string.IsNullOrEmpty(error))
                    Debug.LogWarning("IrisTrace FPS could not rename the enemy asset: " + error);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshList();
            ShowNotification(new GUIContent("Enemy saved"));
        }

        private void DuplicateSelected()
        {
            if (selected == null)
                return;

            EnsureAssetFolder(EnemyConfigRoot);
            string newName = selected.DisplayName + " Copy";
            EnemyDefinition duplicate = CreateInstance<EnemyDefinition>();
            duplicate.Configure(
                Guid.NewGuid().ToString("N"),
                newName,
                selected.Behavior,
                selected.MaxHealth,
                selected.MoveSpeed,
                selected.DetectionRange,
                selected.AttackRange,
                selected.AttackDamage,
                selected.AttackCooldown);
            duplicate.name = SanitizeFileName(newName);

            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{EnemyConfigRoot}/{SanitizeFileName(newName)}.asset");
            AssetDatabase.CreateAsset(duplicate, newPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            selected = duplicate;
            RefreshList();
            Selection.activeObject = duplicate;
            EditorGUIUtility.PingObject(duplicate);
        }

        private void GenerateFromSelected()
        {
            if (selected == null)
                return;

            try
            {
                EnemyPrefabGenerator.Generate(selected, SanitizeFileName(selected.DisplayName));
                ShowNotification(new GUIContent("Enemy prefab generated"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Generation Failed", "The enemy prefab could not be generated. Check the Console for details.", "OK");
            }
        }

        private void LocatePrefab()
        {
            if (selected == null || !AssetDatabase.IsValidFolder(EnemyPrefabRoot))
            {
                EditorUtility.DisplayDialog("Prefab Not Found", "No generated enemy prefabs were found yet.", "OK");
                return;
            }

            string wantedName = SanitizeFileName(selected.DisplayName);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyPrefabRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(assetName, wantedName, StringComparison.OrdinalIgnoreCase) ||
                    assetName.StartsWith(wantedName + " ", StringComparison.OrdinalIgnoreCase) ||
                    assetName.StartsWith(wantedName + "_", StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(path);
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                    return;
                }
            }

            EditorUtility.DisplayDialog("Prefab Not Found", "No prefab with a matching name was found. Use Generate Prefab to create one from this configuration.", "OK");
        }

        private static void AddField(VisualElement parent, VisualElement field)
        {
            field.style.color = IrisTraceTheme.PrimaryText;
            field.style.marginBottom = 8f;
            parent.Add(field);
        }

        private static void StyleButton(Button button, bool primary)
        {
            button.style.height = 28f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.marginBottom = 4f;
            button.style.backgroundColor = primary ? IrisTraceTheme.Accent : IrisTraceTheme.SurfaceStrong;
            button.style.color = primary ? IrisTraceTheme.AccentText : IrisTraceTheme.PrimaryText;
            button.style.unityFontStyleAndWeight = FontStyle.Normal;
            SetRadius(button, 7f);
            SetBorder(button, primary ? Color.clear : IrisTraceTheme.Border);
        }

        private static void StyleField(VisualElement field)
        {
            field.style.color = IrisTraceTheme.PrimaryText;
            field.style.marginBottom = 8f;
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
            float width = color == Color.clear ? 0f : 1f;
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static void SetStringIfEmpty(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && string.IsNullOrEmpty(property.stringValue))
                property.stringValue = value;
        }

        private static void SetEnum(SerializedObject serialized, string propertyName, EnemyBehaviorMode value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.enumValueIndex = (int)value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "No ID";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        private static string SanitizeFileName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Enemy" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return result.Replace('/', '_').Replace('\\', '_');
        }

        private static void EnsureAssetFolder(string assetFolder)
        {
            string[] parts = assetFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
