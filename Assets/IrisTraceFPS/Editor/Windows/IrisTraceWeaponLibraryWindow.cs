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
        private const string AimProfileConfigRoot = "Assets/IrisTraceFPS_Generated/Configurations/Weapons/AimProfiles";

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
        private ObjectField aimProfileField;
        private DropdownField aimModeField;
        private FloatField aimFieldOfViewField;
        private FloatField scopeFieldOfViewField;
        private FloatField aimTransitionSpeedField;
        private FloatField aimSensitivityMultiplierField;
        private Vector3Field aimPivotPositionField;
        private Vector3Field aimPivotEulerField;
        private Toggle hideViewmodelWhenScopedField;
        private ObjectField scopeOverlayTextureField;
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
        private Vector3Field recoilPositionField;
        private Vector3Field recoilEulerField;
        private FloatField recoilRecoverySpeedField;
        private Vector3Field reloadDipPositionField;
        private Vector3Field reloadDipEulerField;
        private FloatField reloadMotionSpeedField;
        private Toggle lookSwayEnabledField;
        private Vector3Field lookSwayPositionField;
        private Vector3Field lookSwayRotationField;
        private FloatField lookSwaySmoothField;
        private Toggle movementBobEnabledField;
        private Vector3Field movementBobPositionField;
        private Vector3Field movementBobRotationField;
        private FloatField movementBobFrequencyField;
        private FloatField movementBobSmoothField;

        [MenuItem("Window/IrisTrace FPS/Weapon Library")]
        public static void Open()
        {
            IrisTraceWindow.OpenPage("Weapons");
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

            WeaponAimProfile currentAimProfile = selected.AimProfile;
            aimProfileField = new ObjectField("Aim Profile (Optional)")
            {
                objectType = typeof(WeaponAimProfile),
                allowSceneObjects = false,
                value = currentAimProfile
            };
            aimModeField = new DropdownField("Aim Behavior")
            {
                choices = new List<string> { "Disabled", "Aim Down Sights", "Scope Zoom" },
                value = currentAimProfile != null ? AimModeToLabel(currentAimProfile.Mode) : "Aim Down Sights"
            };
            aimFieldOfViewField = new FloatField("ADS Field of View") { value = currentAimProfile != null ? currentAimProfile.AimFieldOfView : 55f };
            scopeFieldOfViewField = new FloatField("Scope Field of View") { value = currentAimProfile != null ? currentAimProfile.ScopeFieldOfView : 22f };
            aimTransitionSpeedField = new FloatField("Aim Transition Speed") { value = currentAimProfile != null ? currentAimProfile.TransitionSpeed : 12f };
            aimSensitivityMultiplierField = new FloatField("Aim Sensitivity Multiplier") { value = currentAimProfile != null ? currentAimProfile.AimSensitivityMultiplier : 0.65f };
            aimPivotPositionField = new Vector3Field("Aim Pose Position Offset") { value = currentAimProfile != null ? currentAimProfile.AimPivotPositionOffset : new Vector3(-0.16f, 0.14f, 0.02f) };
            aimPivotEulerField = new Vector3Field("Aim Pose Rotation Offset") { value = currentAimProfile != null ? currentAimProfile.AimPivotEulerOffset : Vector3.zero };
            hideViewmodelWhenScopedField = new Toggle("Hide Weapon Model While Scoped") { value = currentAimProfile == null || currentAimProfile.HideViewmodelWhileScoped };
            scopeOverlayTextureField = new ObjectField("Scope Overlay Texture (Optional)") { objectType = typeof(Texture2D), allowSceneObjects = false, value = currentAimProfile != null ? currentAimProfile.ScopeOverlayTexture : null };
            aimProfileField.RegisterValueChangedCallback(evt => LoadAimProfileFields(evt.newValue as WeaponAimProfile));
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
            recoilPositionField = new Vector3Field("Shot Recoil Position") { value = selected.RecoilLocalPosition };
            recoilEulerField = new Vector3Field("Shot Recoil Rotation") { value = selected.RecoilLocalEuler };
            recoilRecoverySpeedField = new FloatField("Recoil Recovery Speed") { value = selected.RecoilRecoverySpeed };
            reloadDipPositionField = new Vector3Field("Reload Dip Position") { value = selected.ReloadDipLocalPosition };
            reloadDipEulerField = new Vector3Field("Reload Dip Rotation") { value = selected.ReloadDipLocalEuler };
            reloadMotionSpeedField = new FloatField("Reload Motion Speed") { value = selected.ReloadMotionSpeed };
            lookSwayEnabledField = new Toggle("Enable Look Sway") { value = selected.EnableLookSway };
            lookSwayPositionField = new Vector3Field("Look Sway Position Amount") { value = selected.LookSwayPositionAmount };
            lookSwayRotationField = new Vector3Field("Look Sway Rotation Amount") { value = selected.LookSwayRotationAmount };
            lookSwaySmoothField = new FloatField("Look Sway Smoothness") { value = selected.LookSwaySmooth };
            movementBobEnabledField = new Toggle("Enable Movement Bob") { value = selected.EnableMovementBobbing };
            movementBobPositionField = new Vector3Field("Movement Bob Position Amount") { value = selected.MovementBobPositionAmount };
            movementBobRotationField = new Vector3Field("Movement Bob Rotation Amount") { value = selected.MovementBobRotationAmount };
            movementBobFrequencyField = new FloatField("Movement Bob Frequency") { value = selected.MovementBobFrequency };
            movementBobSmoothField = new FloatField("Movement Bob Smoothness") { value = selected.MovementBobSmooth };

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

            Foldout aimFoldout = new Foldout { text = "Optional Aim Behavior", value = currentAimProfile != null };
            aimFoldout.style.marginTop = 6f;
            AddField(aimFoldout, aimProfileField);
            Button createAimProfileButton = new Button(CreateAimProfileForSelected) { text = "Create Aim Profile" };
            StyleButton(createAimProfileButton, false);
            AddField(aimFoldout, createAimProfileButton);
            Label aimNote = new Label("No profile = no RMB aiming. Use separate profiles for hip-fire-only weapons, classic ADS, or scoped zoom.");
            aimNote.style.color = IrisTraceTheme.SecondaryText;
            aimNote.style.whiteSpace = WhiteSpace.Normal;
            aimNote.style.fontSize = 10f;
            AddField(aimFoldout, aimNote);
            AddField(aimFoldout, aimModeField);
            AddField(aimFoldout, aimFieldOfViewField);
            AddField(aimFoldout, scopeFieldOfViewField);
            AddField(aimFoldout, aimTransitionSpeedField);
            AddField(aimFoldout, aimSensitivityMultiplierField);
            AddField(aimFoldout, aimPivotPositionField);
            AddField(aimFoldout, aimPivotEulerField);
            AddField(aimFoldout, hideViewmodelWhenScopedField);
            AddField(aimFoldout, scopeOverlayTextureField);
            SetAimProfileControlsEnabled(currentAimProfile != null);
            panel.Add(aimFoldout);

            Foldout feedbackFoldout = new Foldout { text = "Recoil & Reload Feedback", value = false };
            feedbackFoldout.style.marginTop = 6f;
            AddField(feedbackFoldout, recoilPositionField);
            AddField(feedbackFoldout, recoilEulerField);
            AddField(feedbackFoldout, recoilRecoverySpeedField);
            AddField(feedbackFoldout, reloadDipPositionField);
            AddField(feedbackFoldout, reloadDipEulerField);
            AddField(feedbackFoldout, reloadMotionSpeedField);
            panel.Add(feedbackFoldout);

            Foldout motionFoldout = new Foldout { text = "Look Sway & Movement Bob", value = false };
            motionFoldout.style.marginTop = 6f;
            AddField(motionFoldout, lookSwayEnabledField);
            AddField(motionFoldout, lookSwayPositionField);
            AddField(motionFoldout, lookSwayRotationField);
            AddField(motionFoldout, lookSwaySmoothField);
            AddField(motionFoldout, movementBobEnabledField);
            AddField(motionFoldout, movementBobPositionField);
            AddField(motionFoldout, movementBobRotationField);
            AddField(motionFoldout, movementBobFrequencyField);
            AddField(motionFoldout, movementBobSmoothField);
            panel.Add(motionFoldout);

            Label viewmodelNote = new Label("Mount settings position and scale the whole weapon in camera space. Recoil, reload dip, look sway and movement bob animate the weapon only; they never change camera aim or hitscan direction.");
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
            WeaponAimProfile aimProfile = aimProfileField != null ? aimProfileField.value as WeaponAimProfile : null;
            SerializedProperty aimProfileProperty = serialized.FindProperty("aimProfile");
            if (aimProfileProperty != null) aimProfileProperty.objectReferenceValue = aimProfile;
            SetVector3(serialized, "viewModelMountLocalPosition", viewModelMountPositionField.value);
            SetVector3(serialized, "viewModelMountLocalEuler", viewModelMountRotationField.value);
            SetVector3(serialized, "viewModelMountLocalScale", viewModelMountScaleField.value);
            SetVector3(serialized, "viewModelLocalPosition", viewModelPositionField.value);
            SetVector3(serialized, "viewModelLocalEuler", viewModelRotationField.value);
            SetVector3(serialized, "viewModelLocalScale", viewModelScaleField.value);
            SetVector3(serialized, "recoilLocalPosition", recoilPositionField.value);
            SetVector3(serialized, "recoilLocalEuler", recoilEulerField.value);
            SetFloat(serialized, "recoilRecoverySpeed", Mathf.Max(0.1f, recoilRecoverySpeedField.value));
            SetVector3(serialized, "reloadDipLocalPosition", reloadDipPositionField.value);
            SetVector3(serialized, "reloadDipLocalEuler", reloadDipEulerField.value);
            SetFloat(serialized, "reloadMotionSpeed", Mathf.Max(0.1f, reloadMotionSpeedField.value));
            SerializedProperty lookSwayEnabled = serialized.FindProperty("enableLookSway");
            if (lookSwayEnabled != null) lookSwayEnabled.boolValue = lookSwayEnabledField.value;
            SetVector3(serialized, "lookSwayPositionAmount", lookSwayPositionField.value);
            SetVector3(serialized, "lookSwayRotationAmount", lookSwayRotationField.value);
            SetFloat(serialized, "lookSwaySmooth", Mathf.Max(0.1f, lookSwaySmoothField.value));
            SerializedProperty movementBobEnabled = serialized.FindProperty("enableMovementBobbing");
            if (movementBobEnabled != null) movementBobEnabled.boolValue = movementBobEnabledField.value;
            SetVector3(serialized, "movementBobPositionAmount", movementBobPositionField.value);
            SetVector3(serialized, "movementBobRotationAmount", movementBobRotationField.value);
            SetFloat(serialized, "movementBobFrequency", Mathf.Max(0.1f, movementBobFrequencyField.value));
            SetFloat(serialized, "movementBobSmooth", Mathf.Max(0.1f, movementBobSmoothField.value));
            serialized.ApplyModifiedProperties();

            SaveAimProfile(aimProfile);

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

        private void CreateAimProfileForSelected()
        {
            if (selected == null)
                return;

            EnsureFolderExists(AimProfileConfigRoot);
            WeaponAimProfile profile = CreateInstance<WeaponAimProfile>();
            string profileName = SanitizeFileName((selected.DisplayName ?? "Weapon") + " Aim Profile");
            profile.name = profileName;
            string path = AssetDatabase.GenerateUniqueAssetPath(AimProfileConfigRoot + "/" + profileName + ".asset");
            AssetDatabase.CreateAsset(profile, path);

            SerializedObject weaponSerialized = new SerializedObject(selected);
            SerializedProperty profileProperty = weaponSerialized.FindProperty("aimProfile");
            if (profileProperty != null)
                profileProperty.objectReferenceValue = profile;
            weaponSerialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (aimProfileField != null)
                aimProfileField.value = profile;
            LoadAimProfileFields(profile);
            ShowNotification(new GUIContent("Aim profile created"));
        }

        private void LoadAimProfileFields(WeaponAimProfile profile)
        {
            if (profile != null)
            {
                aimModeField.value = AimModeToLabel(profile.Mode);
                aimFieldOfViewField.value = profile.AimFieldOfView;
                scopeFieldOfViewField.value = profile.ScopeFieldOfView;
                aimTransitionSpeedField.value = profile.TransitionSpeed;
                aimSensitivityMultiplierField.value = profile.AimSensitivityMultiplier;
                aimPivotPositionField.value = profile.AimPivotPositionOffset;
                aimPivotEulerField.value = profile.AimPivotEulerOffset;
                hideViewmodelWhenScopedField.value = profile.HideViewmodelWhileScoped;
                scopeOverlayTextureField.value = profile.ScopeOverlayTexture;
            }
            else
            {
                aimModeField.value = "Aim Down Sights";
                aimFieldOfViewField.value = 55f;
                scopeFieldOfViewField.value = 22f;
                aimTransitionSpeedField.value = 12f;
                aimSensitivityMultiplierField.value = 0.65f;
                aimPivotPositionField.value = new Vector3(-0.16f, 0.14f, 0.02f);
                aimPivotEulerField.value = Vector3.zero;
                hideViewmodelWhenScopedField.value = true;
                scopeOverlayTextureField.value = null;
            }

            SetAimProfileControlsEnabled(profile != null);
        }

        private void SetAimProfileControlsEnabled(bool enabled)
        {
            if (aimModeField == null)
                return;
            aimModeField.SetEnabled(enabled);
            aimFieldOfViewField.SetEnabled(enabled);
            scopeFieldOfViewField.SetEnabled(enabled);
            aimTransitionSpeedField.SetEnabled(enabled);
            aimSensitivityMultiplierField.SetEnabled(enabled);
            aimPivotPositionField.SetEnabled(enabled);
            aimPivotEulerField.SetEnabled(enabled);
            hideViewmodelWhenScopedField.SetEnabled(enabled);
            scopeOverlayTextureField.SetEnabled(enabled);
        }

        private void SaveAimProfile(WeaponAimProfile profile)
        {
            if (profile == null)
                return;

            SerializedObject serialized = new SerializedObject(profile);
            SerializedProperty mode = serialized.FindProperty("mode");
            if (mode != null)
                mode.enumValueIndex = AimLabelToIndex(aimModeField.value);
            SetFloat(serialized, "aimFieldOfView", Mathf.Clamp(aimFieldOfViewField.value, 10f, 89f));
            SetFloat(serialized, "scopeFieldOfView", Mathf.Clamp(scopeFieldOfViewField.value, 5f, 60f));
            SetFloat(serialized, "transitionSpeed", Mathf.Max(0.1f, aimTransitionSpeedField.value));
            SetFloat(serialized, "aimSensitivityMultiplier", Mathf.Clamp(aimSensitivityMultiplierField.value, 0.05f, 2f));
            SetVector3(serialized, "aimPivotPositionOffset", aimPivotPositionField.value);
            SetVector3(serialized, "aimPivotEulerOffset", aimPivotEulerField.value);
            SerializedProperty hide = serialized.FindProperty("hideViewmodelWhileScoped");
            if (hide != null) hide.boolValue = hideViewmodelWhenScopedField.value;
            SerializedProperty overlay = serialized.FindProperty("scopeOverlayTexture");
            if (overlay != null) overlay.objectReferenceValue = scopeOverlayTextureField.value;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(profile);
        }

        private static string AimModeToLabel(WeaponAimMode mode)
        {
            switch (mode)
            {
                case WeaponAimMode.AimDownSights: return "Aim Down Sights";
                case WeaponAimMode.ScopeZoom: return "Scope Zoom";
                default: return "Disabled";
            }
        }

        private static int AimLabelToIndex(string label)
        {
            switch (label)
            {
                case "Aim Down Sights": return (int)WeaponAimMode.AimDownSights;
                case "Scope Zoom": return (int)WeaponAimMode.ScopeZoom;
                default: return (int)WeaponAimMode.Disabled;
            }
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
