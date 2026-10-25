using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using IrisTrace.Editor.UI;
using IrisTrace.Runtime.Weapons;
using IrisTrace.Runtime.Characters;

namespace IrisTrace.Editor.Windows
{
    /// <summary>Browse, edit, and duplicate saved weapon definitions.</summary>
    public sealed class IrisTraceWeaponLibraryWindow : EditorWindow
    {
        private const string WeaponConfigRoot = "Assets/IrisTraceFPS_Generated/Configurations/Weapons";
        private const string CharacterConfigRoot = "Assets/IrisTraceFPS_Generated/Configurations/Characters";

        private readonly List<WeaponDefinition> allWeapons = new List<WeaponDefinition>();
        private TextField searchField;
        private VisualElement listContainer;
        private VisualElement detailsContainer;
        private WeaponDefinition selected;

        private TextField nameField;
        private FloatField damageField;
        private FloatField shotsPerSecondField;
        private IntegerField magazineSizeField;
        private IntegerField reserveAmmoField;
        private FloatField reloadDurationField;
        private FloatField rangeField;
        private DropdownField fireModeField;
        private ObjectField fireClipField;
        private ObjectField reloadClipField;
        private ObjectField dryFireClipField;
        private ObjectField viewModelPrefabField;
        private Vector3Field viewModelMountPositionField;
        private Vector3Field viewModelMountRotationField;
        private Vector3Field viewModelMountScaleField;
        private Vector3Field viewModelPositionField;
        private Vector3Field viewModelRotationField;
        private Vector3Field viewModelScaleField;

        [MenuItem("Window/IrisTrace FPS/Weapon Library")]
        public static void Open()
        {
            IrisTraceWeaponLibraryWindow window = GetWindow<IrisTraceWeaponLibraryWindow>("Weapon Library");
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
            Label title = new Label("Weapon Library");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 18f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label subtitle = new Label("MANAGE REUSABLE WEAPON CONFIGURATIONS");
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 9f;
            subtitle.style.letterSpacing = 0.7f;
            titleStack.Add(title);
            titleStack.Add(subtitle);
            header.Add(titleStack);

            Button createButton = new Button(CreateWeapon) { text = "+ New Weapon" };
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

            allWeapons.Clear();
            if (AssetDatabase.IsValidFolder(WeaponConfigRoot))
            {
                string[] guids = AssetDatabase.FindAssets("t:WeaponDefinition", new[] { WeaponConfigRoot });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    WeaponDefinition definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                    if (definition != null)
                        allWeapons.Add(definition);
                }
            }

            allWeapons.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            listContainer.Clear();
            detailsContainer.Clear();

            string query = searchField != null ? searchField.value.Trim() : string.Empty;
            int visibleCount = 0;
            foreach (WeaponDefinition definition in allWeapons)
            {
                string displayName = definition.DisplayName ?? string.Empty;
                string id = definition.Id ?? string.Empty;
                if (!string.IsNullOrEmpty(query) &&
                    displayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    id.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                WeaponDefinition captured = definition;
                Button row = new Button(() => SelectWeapon(captured));
                row.text = $"{displayName}\n{ShortId(id)}  •  {definition.Damage:0.#} dmg  /  {definition.MagazineSize} rounds";
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
                Label empty = new Label(allWeapons.Count == 0
                    ? "No weapon configurations yet. Create one to start your library."
                    : "No weapons match this search.");
                empty.style.color = IrisTraceTheme.SecondaryText;
                empty.style.whiteSpace = WhiteSpace.Normal;
                empty.style.marginTop = 8f;
                empty.style.marginBottom = 12f;
                listContainer.Add(empty);
            }

            if (selected != null && !allWeapons.Contains(selected))
                selected = null;
            DrawDetails();
        }

        private void SelectWeapon(WeaponDefinition definition)
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

            Label heading = new Label("Weapon Settings");
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
            damageField = new FloatField("Damage") { value = selected.Damage };
            shotsPerSecondField = new FloatField("Shots Per Second") { value = selected.ShotsPerSecond };
            magazineSizeField = new IntegerField("Magazine Size") { value = selected.MagazineSize };
            reserveAmmoField = new IntegerField("Starting Reserve Ammo") { value = selected.StartingReserveAmmo };
            reloadDurationField = new FloatField("Reload Duration (s)") { value = selected.ReloadDuration };
            rangeField = new FloatField("Range") { value = selected.Range };
            fireModeField = new DropdownField("Fire Mode");
            fireModeField.choices = new List<string> { "Semi-Auto", "Automatic" };
            fireModeField.value = selected.Automatic ? "Automatic" : "Semi-Auto";
            fireClipField = new ObjectField("Fire Sound") { objectType = typeof(AudioClip), allowSceneObjects = false, value = selected.FireClip };
            reloadClipField = new ObjectField("Reload Sound") { objectType = typeof(AudioClip), allowSceneObjects = false, value = selected.ReloadClip };
            dryFireClipField = new ObjectField("Dry-Fire Sound (Optional)") { objectType = typeof(AudioClip), allowSceneObjects = false, value = selected.DryFireClip };
            viewModelPrefabField = new ObjectField("First-Person Viewmodel Prefab") { objectType = typeof(GameObject), allowSceneObjects = false, value = selected.ViewModelPrefab };
            viewModelMountPositionField = new Vector3Field("Weapon Mount Position (Camera Local)") { value = selected.ViewModelMountLocalPosition };
            viewModelMountRotationField = new Vector3Field("Weapon Mount Rotation") { value = selected.ViewModelMountLocalEuler };
            viewModelMountScaleField = new Vector3Field("Weapon Mount Scale") { value = selected.ViewModelMountLocalScale };
            viewModelPositionField = new Vector3Field("Model Local Position (Inside Mount)") { value = selected.ViewModelLocalPosition };
            viewModelRotationField = new Vector3Field("Viewmodel Local Rotation") { value = selected.ViewModelLocalEuler };
            viewModelScaleField = new Vector3Field("Viewmodel Local Scale") { value = selected.ViewModelLocalScale };

            AddField(panel, nameField);
            AddField(panel, damageField);
            AddField(panel, shotsPerSecondField);
            AddField(panel, magazineSizeField);
            AddField(panel, reserveAmmoField);
            AddField(panel, reloadDurationField);
            AddField(panel, rangeField);
            AddField(panel, fireModeField);
            AddField(panel, fireClipField);
            AddField(panel, reloadClipField);
            AddField(panel, dryFireClipField);
            AddField(panel, viewModelPrefabField);
            AddField(panel, viewModelMountPositionField);
            AddField(panel, viewModelMountRotationField);
            AddField(panel, viewModelMountScaleField);
            AddField(panel, viewModelPositionField);
            AddField(panel, viewModelRotationField);
            AddField(panel, viewModelScaleField);

            Label viewmodelNote = new Label("Mount settings position and scale the whole weapon in camera space. Model Local settings offset the assigned prefab or the built-in placeholder inside the mount.");
            viewmodelNote.style.color = IrisTraceTheme.SecondaryText;
            viewmodelNote.style.fontSize = 10f;
            viewmodelNote.style.whiteSpace = WhiteSpace.Normal;
            viewmodelNote.style.marginBottom = 8f;
            panel.Add(viewmodelNote);

            VisualElement actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexWrap = Wrap.Wrap;
            actions.style.marginTop = 10f;

            Button save = new Button(SaveChanges) { text = "Save Changes" };
            StyleButton(save, true);
            save.style.marginRight = 6f;
            actions.Add(save);

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

            Button ping = new Button(PingSelected) { text = "Find Asset" };
            StyleButton(ping, false);
            ping.style.marginRight = 6f;
            actions.Add(ping);

            Button delete = new Button(DeleteSelected) { text = "Delete" };
            StyleDangerButton(delete);
            actions.Add(delete);

            panel.Add(actions);
            detailsContainer.Add(panel);
        }

        private void CreateWeapon()
        {
            EnsureFolderExists(WeaponConfigRoot);
            WeaponDefinition definition = CreateInstance<WeaponDefinition>();
            definition.name = "New Weapon";

            SerializedObject serialized = new SerializedObject(definition);
            SerializedProperty idProperty = serialized.FindProperty("id");
            SerializedProperty nameProperty = serialized.FindProperty("displayName");
            if (idProperty != null) idProperty.stringValue = Guid.NewGuid().ToString("N");
            if (nameProperty != null) nameProperty.stringValue = "New Weapon";
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string path = AssetDatabase.GenerateUniqueAssetPath(WeaponConfigRoot + "/New Weapon.asset");
            AssetDatabase.CreateAsset(definition, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            selected = definition;
            RefreshList();
            Selection.activeObject = definition;
            EditorGUIUtility.PingObject(definition);
        }

        private void SaveChanges()
        {
            if (selected == null || nameField == null)
                return;

            string displayName = string.IsNullOrWhiteSpace(nameField.value) ? "Weapon" : nameField.value.Trim();
            Undo.RecordObject(selected, "Edit Weapon Definition");
            SerializedObject serialized = new SerializedObject(selected);
            SetString(serialized, "displayName", displayName);
            SetStringIfEmpty(serialized, "id", Guid.NewGuid().ToString("N"));
            SetFloat(serialized, "damage", Mathf.Max(0.1f, damageField.value));
            SetFloat(serialized, "shotsPerSecond", Mathf.Max(0.1f, shotsPerSecondField.value));
            SetInt(serialized, "magazineSize", Mathf.Max(1, magazineSizeField.value));
            SetInt(serialized, "reserveAmmo", Mathf.Max(0, reserveAmmoField.value));
            SetFloat(serialized, "reloadDuration", Mathf.Max(0.1f, reloadDurationField.value));
            SetFloat(serialized, "range", Mathf.Max(1f, rangeField.value));
            SerializedProperty automatic = serialized.FindProperty("automatic");
            if (automatic != null) automatic.boolValue = fireModeField.value == "Automatic";
            SerializedProperty fireClip = serialized.FindProperty("fireClip");
            if (fireClip != null) fireClip.objectReferenceValue = fireClipField.value;
            SerializedProperty reloadClip = serialized.FindProperty("reloadClip");
            if (reloadClip != null) reloadClip.objectReferenceValue = reloadClipField.value;
            SerializedProperty dryFireClip = serialized.FindProperty("dryFireClip");
            if (dryFireClip != null) dryFireClip.objectReferenceValue = dryFireClipField.value;
            SerializedProperty viewModelPrefab = serialized.FindProperty("viewModelPrefab");
            if (viewModelPrefab != null) viewModelPrefab.objectReferenceValue = viewModelPrefabField.value;
            SetVector3(serialized, "viewModelMountLocalPosition", viewModelMountPositionField.value);
            SetVector3(serialized, "viewModelMountLocalEuler", viewModelMountRotationField.value);
            SetVector3(serialized, "viewModelMountLocalScale", viewModelMountScaleField.value);
            SetVector3(serialized, "viewModelLocalPosition", viewModelPositionField.value);
            SetVector3(serialized, "viewModelLocalEuler", viewModelRotationField.value);
            SetVector3(serialized, "viewModelLocalScale", viewModelScaleField.value);
            serialized.ApplyModifiedProperties();

            string assetPath = AssetDatabase.GetAssetPath(selected);
            string targetName = SanitizeFileName(displayName);
            if (!string.IsNullOrEmpty(assetPath) && Path.GetFileNameWithoutExtension(assetPath) != targetName)
                AssetDatabase.RenameAsset(assetPath, targetName);

            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ShowNotification(new GUIContent("Weapon saved"));
            RefreshList();
        }

        private void DuplicateSelected()
        {
            if (selected == null)
                return;

            EnsureFolderExists(WeaponConfigRoot);
            string newName = (selected.DisplayName ?? "Weapon") + " Copy";
            WeaponDefinition duplicate = Instantiate(selected);
            duplicate.name = SanitizeFileName(newName);
            SerializedObject serialized = new SerializedObject(duplicate);
            SetString(serialized, "id", Guid.NewGuid().ToString("N"));
            SetString(serialized, "displayName", newName);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string path = AssetDatabase.GenerateUniqueAssetPath(WeaponConfigRoot + "/" + SanitizeFileName(newName) + ".asset");
            AssetDatabase.CreateAsset(duplicate, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            selected = duplicate;
            RefreshList();
            Selection.activeObject = duplicate;
            EditorGUIUtility.PingObject(duplicate);
        }

        private void DeleteSelected()
        {
            if (selected == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith(WeaponConfigRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Delete Failed", "The selected asset is outside the IrisTrace weapon configuration folder.", "OK");
                return;
            }

            int dependentCharacters = CountCharactersUsingWeapon(selected.Id);
            string dependencyWarning = dependentCharacters > 0
                ? $"\n\nWarning: {dependentCharacters} character configuration(s) reference this weapon. They may need another weapon assigned before prefab generation."
                : string.Empty;
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Weapon Configuration?",
                $"Delete '{selected.DisplayName}' from the Weapon Library?{dependencyWarning}\n\nOnly the saved weapon configuration will be removed. Existing character configurations and generated prefabs will remain untouched.",
                "Delete Configuration",
                "Cancel");
            if (!confirmed)
                return;

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                EditorUtility.DisplayDialog("Delete Failed", "Unity could not delete this weapon configuration asset.", "OK");
                return;
            }

            selected = null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshList();
            ShowNotification(new GUIContent("Weapon configuration deleted"));
        }

        private static int CountCharactersUsingWeapon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId) || !AssetDatabase.IsValidFolder(CharacterConfigRoot))
                return 0;

            int count = 0;
            string[] guids = AssetDatabase.FindAssets("t:CharacterDefinition", new[] { CharacterConfigRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                CharacterDefinition character = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                if (character != null && string.Equals(character.PrimaryWeaponId, weaponId, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private void PingSelected()
        {
            if (selected == null)
                return;

            Selection.activeObject = selected;
            EditorGUIUtility.PingObject(selected);
        }

        private static void AddField(VisualElement parent, VisualElement field)
        {
            field.style.marginBottom = 6f;
            field.style.color = IrisTraceTheme.PrimaryText;
            parent.Add(field);
        }

        private static void StyleField(VisualElement field)
        {
            field.style.color = IrisTraceTheme.PrimaryText;
            field.style.marginBottom = 8f;
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

        private static void StyleDangerButton(Button button)
        {
            button.style.height = 28f;
            button.style.paddingLeft = 10f;
            button.style.paddingRight = 10f;
            button.style.marginBottom = 4f;
            button.style.backgroundColor = new Color(145f / 255f, 52f / 255f, 52f / 255f, 1f);
            button.style.color = Color.white;
            SetRadius(button, 7f);
            SetBorder(button, new Color32(176, 77, 77, 255));
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

        private static void SetVector3(SerializedObject serialized, string propertyName, Vector3 value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.vector3Value = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.stringValue = value;
        }

        private static void SetStringIfEmpty(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null && string.IsNullOrEmpty(property.stringValue)) property.stringValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.floatValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null) property.intValue = value;
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "No ID";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        private static string SanitizeFileName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Weapon" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars()) result = result.Replace(invalid, '_');
            return result.Replace('/', '_').Replace('\\', '_');
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string normalized = folderPath.Replace('\\', '/');
            string[] parts = normalized.Split('/');
            if (parts.Length == 0 || parts[0] != "Assets")
                throw new InvalidOperationException("Weapon configuration folder must be inside Assets.");

            string current = "Assets";
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
