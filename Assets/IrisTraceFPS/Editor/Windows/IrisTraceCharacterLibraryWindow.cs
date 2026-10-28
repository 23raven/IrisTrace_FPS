using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using IrisTrace.Editor.Generators;
using IrisTrace.Editor.UI;
using IrisTrace.Runtime.Characters;
using IrisTrace.Runtime.Weapons;

namespace IrisTrace.Editor.Windows
{
    /// <summary>Browse, duplicate, and generate characters from saved IrisTrace configurations.</summary>
    public sealed class IrisTraceCharacterLibraryWindow : EditorWindow
    {
        private const string CharacterConfigRoot = "Assets/IrisTraceFPS_Generated/Configurations/Characters";
        private const string WeaponConfigRoot = "Assets/IrisTraceFPS_Generated/Configurations/Weapons";
        private const string PrefabRoot = "Assets/IrisTraceFPS_Generated/Prefabs/Characters";

        private TextField searchField;
        private VisualElement listContainer;
        private CharacterDefinition selected;
        private readonly List<CharacterDefinition> allCharacters = new List<CharacterDefinition>();

        [MenuItem("Window/IrisTrace FPS/Character Library")]
        public static void Open()
        {
            IrisTraceWindow.OpenPage("Characters");
        }

        public void CreateGUI()
        {
            BuildEmbeddedContent(rootVisualElement);
        }

        public VisualElement CreateEmbeddedView()
        {
            VisualElement embeddedRoot = new VisualElement();
            BuildEmbeddedContent(embeddedRoot);
            return embeddedRoot;
        }

        private void BuildEmbeddedContent(VisualElement root)
        {
            root.Clear();
            root.style.flexGrow = 1f;
            root.style.backgroundColor = IrisTraceTheme.Background;

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.paddingLeft = 16f;
            scroll.style.paddingRight = 16f;
            scroll.style.paddingTop = 14f;
            scroll.style.paddingBottom = 16f;
            root.Add(scroll);

            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 12f;

            VisualElement titleStack = new VisualElement();
            titleStack.style.flexGrow = 1f;
            Label title = new Label("Character Library");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 18f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label subtitle = new Label("BROWSE AND REUSE SAVED CONFIGURATIONS");
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 9f;
            subtitle.style.letterSpacing = 0.7f;
            titleStack.Add(title);
            titleStack.Add(subtitle);
            header.Add(titleStack);

            Button createButton = new Button(IrisTraceHeroWizardWindow.Open) { text = "Create Hero" };
            StyleButton(createButton, true);
            header.Add(createButton);
            scroll.Add(header);

            searchField = new TextField { value = string.Empty };
            searchField.label = "Search";
            searchField.style.marginBottom = 10f;
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

            RefreshList();
        
        }

        private void RefreshList()
        {
            if (listContainer == null)
                return;

            allCharacters.Clear();
            if (AssetDatabase.IsValidFolder(CharacterConfigRoot))
            {
                string[] guids = AssetDatabase.FindAssets("t:CharacterDefinition", new[] { CharacterConfigRoot });
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    CharacterDefinition definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(path);
                    if (definition != null)
                        allCharacters.Add(definition);
                }
            }

            allCharacters.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            listContainer.Clear();

            string query = searchField != null ? searchField.value.Trim() : string.Empty;
            if (selected != null && !string.IsNullOrEmpty(query) &&
                selected.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                (selected.Id == null || selected.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0))
                selected = null;

            int visibleCount = 0;
            foreach (CharacterDefinition definition in allCharacters)
            {
                if (!string.IsNullOrEmpty(query) &&
                    definition.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0 &&
                    (definition.Id == null || definition.Id.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0))
                    continue;

                CharacterDefinition captured = definition;
                bool isExpanded = selected == definition;

                VisualElement card = new VisualElement();
                card.style.backgroundColor = isExpanded ? IrisTraceTheme.AccentHover : IrisTraceTheme.Surface;
                card.style.paddingLeft = 8f;
                card.style.paddingRight = 8f;
                card.style.paddingTop = 7f;
                card.style.paddingBottom = 7f;
                card.style.marginBottom = 7f;
                SetRadius(card, 9f);
                SetBorder(card, isExpanded ? IrisTraceTheme.Accent : IrisTraceTheme.Border);

                VisualElement row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.minHeight = 48f;
                row.Add(CreateCharacterIcon(definition, 44f));

                VisualElement identity = new VisualElement();
                identity.style.flexGrow = 1f;
                identity.style.marginLeft = 9f;
                identity.style.marginRight = 8f;
                identity.style.justifyContent = Justify.Center;

                Label nameLabel = new Label(definition.DisplayName);
                nameLabel.style.color = IrisTraceTheme.PrimaryText;
                nameLabel.style.fontSize = 12f;
                nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

                Label metaLabel = new Label($"{ShortId(definition.Id)}  •  {definition.MaxHealth:0} HP");
                metaLabel.style.color = IrisTraceTheme.SecondaryText;
                metaLabel.style.fontSize = 9f;
                identity.Add(nameLabel);
                identity.Add(metaLabel);
                row.Add(identity);

                Button detailsButton = new Button(() => ToggleDetails(captured))
                {
                    text = isExpanded ? "Hide Details ▲" : "Details ▼"
                };
                StyleButton(detailsButton, isExpanded);
                detailsButton.style.minWidth = 88f;
                detailsButton.style.marginBottom = 0f;
                row.Add(detailsButton);
                card.Add(row);

                if (isExpanded)
                    card.Add(DrawDetails());

                listContainer.Add(card);
                visibleCount++;
            }

            if (visibleCount == 0)
            {
                Label empty = new Label(allCharacters.Count == 0
                    ? "No saved characters yet. Create a hero to add the first configuration."
                    : "No characters match this search.");
                empty.style.color = IrisTraceTheme.SecondaryText;
                empty.style.whiteSpace = WhiteSpace.Normal;
                empty.style.marginTop = 8f;
                empty.style.marginBottom = 12f;
                listContainer.Add(empty);
            }

            if (selected != null && !allCharacters.Contains(selected))
                selected = null;
        }

        private void ToggleDetails(CharacterDefinition definition)
        {
            selected = selected == definition ? null : definition;
            RefreshList();
        }

        private VisualElement DrawDetails()
        {
            if (selected == null)
                return new VisualElement();

            VisualElement panel = new VisualElement();
            panel.style.backgroundColor = IrisTraceTheme.Surface;
            panel.style.paddingLeft = 13f;
            panel.style.paddingRight = 13f;
            panel.style.paddingTop = 10f;
            panel.style.paddingBottom = 12f;
            panel.style.marginTop = 8f;
            SetRadius(panel, 8f);
            SetBorder(panel, IrisTraceTheme.Border);

            Label heading = new Label(selected.DisplayName);
            heading.style.color = IrisTraceTheme.PrimaryText;
            heading.style.fontSize = 15f;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            panel.Add(heading);

            VisualElement portraitRow = new VisualElement();
            portraitRow.style.flexDirection = FlexDirection.Row;
            portraitRow.style.alignItems = Align.Center;
            portraitRow.style.marginTop = 8f;
            portraitRow.style.marginBottom = 8f;
            portraitRow.Add(CreateCharacterIcon(selected, 84f));

            ObjectField portraitField = new ObjectField("Portrait Sprite")
            {
                objectType = typeof(Sprite),
                allowSceneObjects = false,
                value = selected.PortraitIcon
            };
            portraitField.style.flexGrow = 1f;
            portraitField.style.marginLeft = 12f;
            portraitField.tooltip = "Optional icon shown in the Character Library. Select a Sprite asset from your project.";
            portraitField.RegisterValueChangedCallback(evt => SavePortraitIcon(evt.newValue as Sprite));
            portraitRow.Add(portraitField);
            panel.Add(portraitRow);

            AddDetail(panel, "ID", string.IsNullOrEmpty(selected.Id) ? "Missing ID" : selected.Id);
            AddDetail(panel, "Health", $"{selected.MaxHealth:0} HP");
            AddDetail(panel, "Movement", $"Walk {selected.WalkSpeed:0.##}  /  Sprint {selected.SprintSpeed:0.##}");
            AddDetail(panel, "Config", AssetDatabase.GetAssetPath(selected));

            VisualElement actions = new VisualElement();
            actions.style.flexDirection = FlexDirection.Row;
            actions.style.flexWrap = Wrap.Wrap;
            actions.style.marginTop = 10f;

            Button generate = new Button(GenerateFromSelected) { text = "Generate Prefab" };
            StyleButton(generate, true);
            generate.style.marginRight = 6f;
            actions.Add(generate);

            Button duplicate = new Button(DuplicateSelected) { text = "Duplicate" };
            StyleButton(duplicate, false);
            duplicate.style.marginRight = 6f;
            actions.Add(duplicate);

            Button copyId = new Button(() =>
            {
                EditorGUIUtility.systemCopyBuffer = selected.Id ?? string.Empty;
                ShowNotification(new GUIContent("ID copied"));
            }) { text = "Copy ID" };
            StyleButton(copyId, false);
            copyId.style.marginRight = 6f;
            actions.Add(copyId);

            Button locate = new Button(LocatePrefab) { text = "Find Prefab" };
            StyleButton(locate, false);
            locate.style.marginRight = 6f;
            actions.Add(locate);

            Button delete = new Button(DeleteSelected) { text = "Delete" };
            StyleDangerButton(delete);
            actions.Add(delete);

            panel.Add(actions);
            return panel;
        }

        private void SavePortraitIcon(Sprite sprite)
        {
            if (selected == null)
                return;

            SerializedObject serialized = new SerializedObject(selected);
            SerializedProperty portraitProperty = serialized.FindProperty("portraitIcon");
            if (portraitProperty == null)
            {
                EditorUtility.DisplayDialog("Portrait Icon Unavailable",
                    "The character configuration does not contain the portrait field. Reimport the latest IrisTrace FPS files.", "OK");
                return;
            }

            Undo.RecordObject(selected, "Change Character Portrait");
            portraitProperty.objectReferenceValue = sprite;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            RefreshList();
            ShowNotification(new GUIContent(sprite == null ? "Portrait cleared" : "Portrait updated"));
        }

        private void DeleteSelected()
        {
            if (selected == null)
                return;

            string assetPath = AssetDatabase.GetAssetPath(selected);
            if (string.IsNullOrEmpty(assetPath) ||
                !assetPath.StartsWith(CharacterConfigRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("Delete Failed", "The selected asset is outside the IrisTrace character configuration folder.", "OK");
                return;
            }

            string displayName = selected.DisplayName;
            bool confirmed = EditorUtility.DisplayDialog(
                "Delete Character Configuration?",
                $"Delete '{displayName}' from the Character Library?\n\nOnly the saved configuration asset will be removed. Generated prefabs and related weapon configurations will remain untouched.",
                "Delete Configuration",
                "Cancel");
            if (!confirmed)
                return;

            if (!AssetDatabase.DeleteAsset(assetPath))
            {
                EditorUtility.DisplayDialog("Delete Failed", "Unity could not delete this configuration asset.", "OK");
                return;
            }

            selected = null;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RefreshList();
            ShowNotification(new GUIContent("Character configuration deleted"));
        }

        private static VisualElement CreateCharacterIcon(CharacterDefinition definition, float size)
        {
            VisualElement frame = new VisualElement();
            frame.style.width = size;
            frame.style.height = size;
            frame.style.minWidth = size;
            frame.style.minHeight = size;
            frame.style.alignItems = Align.Center;
            frame.style.justifyContent = Justify.Center;
            frame.style.backgroundColor = IrisTraceTheme.SurfaceStrong;
            frame.style.overflow = Overflow.Hidden;
            SetRadius(frame, 7f);
            SetBorder(frame, IrisTraceTheme.Border);

            if (definition != null && definition.PortraitIcon != null)
            {
                Image image = new Image { sprite = definition.PortraitIcon, scaleMode = ScaleMode.ScaleToFit };
                image.style.width = Length.Percent(100f);
                image.style.height = Length.Percent(100f);
                frame.Add(image);
            }
            else
            {
                string name = definition != null ? definition.DisplayName : string.Empty;
                string initials = string.IsNullOrWhiteSpace(name) ? "IT" : name.Trim().Substring(0, 1).ToUpperInvariant();
                Label placeholder = new Label(initials);
                placeholder.style.color = IrisTraceTheme.Accent;
                placeholder.style.fontSize = Mathf.Clamp(size * 0.42f, 14f, 30f);
                placeholder.style.unityFontStyleAndWeight = FontStyle.Bold;
                frame.Add(placeholder);
            }

            return frame;
        }

        private void GenerateFromSelected()
        {
            if (selected == null)
                return;

            WeaponDefinition weapon = FindMatchingWeapon(selected);
            if (weapon == null)
            {
                EditorUtility.DisplayDialog("Weapon Configuration Missing",
                    "No matching primary weapon configuration was found. Create a hero with the wizard first, or restore its weapon asset.", "OK");
                return;
            }

            try
            {
                FPSHeroPrefabGenerator.Generate(selected, weapon, SanitizeFileName(selected.DisplayName));
                ShowNotification(new GUIContent("Prefab generated"));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Generation Failed", "The prefab could not be generated. Check the Console for details.", "OK");
            }
        }

        private void DuplicateSelected()
        {
            if (selected == null)
                return;

            string sourcePath = AssetDatabase.GetAssetPath(selected);
            string sourceFolder = Path.GetDirectoryName(sourcePath)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(sourceFolder))
            {
                EditorUtility.DisplayDialog("Duplicate Failed", "Could not determine the configuration folder.", "OK");
                return;
            }

            string newName = selected.DisplayName + " Copy";
            WeaponDefinition sourceWeapon = FindMatchingWeapon(selected);
            string duplicatedWeaponId = null;

            CharacterDefinition duplicate = Instantiate(selected);
            duplicate.name = newName;
            SerializedObject characterSerialized = new SerializedObject(duplicate);
            SetString(characterSerialized, "id", Guid.NewGuid().ToString("N"));
            SetString(characterSerialized, "displayName", newName);
            characterSerialized.ApplyModifiedPropertiesWithoutUndo();
            string newPath = AssetDatabase.GenerateUniqueAssetPath($"{sourceFolder}/{SanitizeFileName(newName)}.asset");
            AssetDatabase.CreateAsset(duplicate, newPath);

            if (sourceWeapon != null)
            {
                string weaponPath = AssetDatabase.GetAssetPath(sourceWeapon);
                string weaponFolder = Path.GetDirectoryName(weaponPath)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(weaponFolder))
                {
                    WeaponDefinition weaponCopy = Instantiate(sourceWeapon);
                    weaponCopy.name = SanitizeFileName(newName) + " Primary Weapon";
                    duplicatedWeaponId = Guid.NewGuid().ToString("N");
                    SerializedObject weaponSerialized = new SerializedObject(weaponCopy);
                    SetString(weaponSerialized, "id", duplicatedWeaponId);
                    SetString(weaponSerialized, "displayName", SanitizeFileName(newName) + " Primary Weapon");
                    weaponSerialized.ApplyModifiedPropertiesWithoutUndo();
                    string weaponCopyPath = AssetDatabase.GenerateUniqueAssetPath($"{weaponFolder}/{SanitizeFileName(newName)}_PrimaryWeapon.asset");
                    AssetDatabase.CreateAsset(weaponCopy, weaponCopyPath);
                }
            }

            if (!string.IsNullOrEmpty(duplicatedWeaponId))
            {
                SerializedObject characterLink = new SerializedObject(duplicate);
                SetString(characterLink, "primaryWeaponId", duplicatedWeaponId);
                characterLink.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(duplicate);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            selected = duplicate;
            RefreshList();
            Selection.activeObject = duplicate;
            EditorGUIUtility.PingObject(duplicate);
        }

        private void LocatePrefab()
        {
            if (selected == null || !AssetDatabase.IsValidFolder(PrefabRoot))
            {
                EditorUtility.DisplayDialog("Prefab Not Found", "No generated character prefabs were found yet.", "OK");
                return;
            }

            string wantedName = SanitizeFileName(selected.DisplayName);
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(assetName, wantedName, StringComparison.OrdinalIgnoreCase) ||
                    assetName.StartsWith(wantedName + " ", StringComparison.OrdinalIgnoreCase))
                {
                    UnityEngine.Object prefab = AssetDatabase.LoadMainAssetAtPath(path);
                    Selection.activeObject = prefab;
                    EditorGUIUtility.PingObject(prefab);
                    return;
                }
            }

            EditorUtility.DisplayDialog("Prefab Not Found",
                "No prefab with a matching name was found. Use Generate Prefab to create one from this configuration.", "OK");
        }

        private static WeaponDefinition FindMatchingWeapon(CharacterDefinition character)
        {
            if (character == null || !AssetDatabase.IsValidFolder(WeaponConfigRoot))
                return null;

            string[] guids = AssetDatabase.FindAssets("t:WeaponDefinition", new[] { WeaponConfigRoot });
            if (!string.IsNullOrEmpty(character.PrimaryWeaponId))
            {
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                    if (weapon != null && string.Equals(weapon.Id, character.PrimaryWeaponId, StringComparison.Ordinal))
                        return weapon;
                }
            }

            // Backward compatibility for character configurations created before stable weapon links existed.
            string expectedName = SanitizeFileName(character.DisplayName) + " Primary Weapon";
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                WeaponDefinition weapon = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
                if (weapon != null && string.Equals(weapon.DisplayName, expectedName, StringComparison.OrdinalIgnoreCase))
                    return weapon;
            }
            return null;
        }

        private static void AddDetail(VisualElement parent, string label, string value)
        {
            Label detail = new Label($"{label}: {value}");
            detail.style.color = IrisTraceTheme.SecondaryText;
            detail.style.fontSize = 10f;
            detail.style.whiteSpace = WhiteSpace.Normal;
            detail.style.marginTop = 4f;
            parent.Add(detail);
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

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "No ID";
            return id.Length <= 8 ? id : id.Substring(0, 8);
        }

        private static string SanitizeFileName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Character" : value.Trim();
            foreach (char invalid in Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return result.Replace('/', '_').Replace('\\', '_');
        }
    }
}
