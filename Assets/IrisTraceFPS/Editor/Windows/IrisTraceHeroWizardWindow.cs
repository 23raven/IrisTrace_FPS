using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using IrisTrace.Editor.Generators;
using IrisTrace.Editor.UI;
using IrisTrace.Runtime.Characters;
using IrisTrace.Runtime.Weapons;

namespace IrisTrace.Editor.Windows
{
    public sealed class IrisTraceHeroWizardWindow : EditorWindow
    {
        private TextField displayNameField;
        private FloatField maxHealthField;
        private FloatField walkSpeedField;
        private FloatField sprintSpeedField;
        private FloatField jumpHeightField;
        private FloatField gravityMultiplierField;
        private FloatField fallGravityMultiplierField;
        private FloatField maximumFallSpeedField;
        private FloatField groundAccelerationField;
        private FloatField groundDecelerationField;
        private FloatField airControlField;
        private Toggle crouchEnabledField;
        private FloatField crouchSpeedField;
        private FloatField crouchedHeightField;
        private FloatField crouchTransitionDurationField;
        private FloatField sensitivityField;
        private FloatField fieldOfViewField;
        private FloatField weaponDamageField;
        private FloatField shotsPerSecondField;
        private IntegerField magazineSizeField;
        private IntegerField reserveAmmoField;
        private FloatField reloadDurationField;
        private FloatField weaponRangeField;
        private Toggle automaticWeaponField;
        private ObjectField walkFootstepSoundField;
        private ObjectField sprintFootstepSoundField;
        private ObjectField crouchFootstepSoundField;
        private ObjectField jumpSoundField;
        private ObjectField landingSoundField;
        private ObjectField crouchStartSoundField;
        private ObjectField crouchEndSoundField;
        private ObjectField weaponFireSoundField;
        private ObjectField weaponReloadSoundField;

        private EnumField ability1TypeField;
        private FloatField ability1CooldownField;
        private FloatField ability1ValueField;
        private EnumField ability2TypeField;
        private FloatField ability2CooldownField;
        private FloatField ability2ValueField;

        private KeyBindingField forwardKeyField;
        private KeyBindingField backwardKeyField;
        private KeyBindingField leftKeyField;
        private KeyBindingField rightKeyField;
        private KeyBindingField jumpKeyField;
        private KeyBindingField sprintKeyField;
        private KeyBindingField crouchKeyField;
        private KeyBindingField fireKeyField;
        private KeyBindingField reloadKeyField;
        private KeyBindingField ability1KeyField;
        private KeyBindingField ability2KeyField;
        private KeyBindingField activeBindingField;

        private InputAction captureAction;
        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;

        [MenuItem("Window/IrisTrace FPS/Create FPS Hero")]
        public static void Open()
        {
            IrisTraceHeroWizardWindow window = GetWindow<IrisTraceHeroWizardWindow>("Create FPS Hero");
            window.minSize = new Vector2(430f, 650f);
            window.Show();
        }

        private void OnDisable()
        {
            CancelBindingCapture(false);
        }

        public void CreateGUI()
        {
            CancelBindingCapture(true);
            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = IrisTraceTheme.Background;

            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.backgroundColor = IrisTraceTheme.Background;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            rootVisualElement.Add(scroll);

            VisualElement content = new VisualElement();
            content.style.paddingLeft = 20f;
            content.style.paddingRight = 20f;
            content.style.paddingTop = 18f;
            content.style.paddingBottom = 20f;
            scroll.Add(content);

            content.Add(BuildHeader());
            content.Add(BuildSection("BASICS", "Name your character and set core stats."));
            content.Add(BuildBasicsPanel());
            content.Add(BuildSection("MOVEMENT & CAMERA", "Configure movement speed, jump and optional crouching."));
            content.Add(BuildMovementPanel());
            content.Add(BuildSection("DEFAULT WEAPON", "Configure the generated hitscan weapon."));
            content.Add(BuildWeaponPanel());
            content.Add(BuildSection("AUDIO (OPTIONAL)", "Assign AudioClip assets. Empty fields are safely ignored."));
            content.Add(BuildAudioPanel());
            content.Add(BuildSection("ABILITIES", "Choose up to two built-in skills. Custom scripts can implement the same ability contract."));
            content.Add(BuildAbilitiesPanel());
            content.Add(BuildSection("DEFAULT CONTROLS", "Click a binding, then press a key or mouse button. Esc cancels."));
            content.Add(BuildBindingsPanel());
            content.Add(BuildFooterActions());
        }

        private VisualElement BuildHeader()
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 20f;

            VisualElement mark = new VisualElement();
            mark.style.width = 38f;
            mark.style.height = 38f;
            mark.style.marginRight = 11f;
            mark.style.backgroundColor = IrisTraceTheme.AccentHover;
            mark.style.alignItems = Align.Center;
            mark.style.justifyContent = Justify.Center;
            Radius(mark, 12f);

            Label markLabel = new Label("I");
            markLabel.style.color = IrisTraceTheme.AccentText;
            markLabel.style.fontSize = 18f;
            markLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            mark.Add(markLabel);

            VisualElement titleStack = new VisualElement();
            titleStack.style.flexGrow = 1f;
            Label title = new Label("Create FPS Hero");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 17f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label subtitle = new Label("IRISTRACE FPS / CHARACTER CONFIGURATION");
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 9f;
            subtitle.style.letterSpacing = 1f;
            titleStack.Add(title);
            titleStack.Add(subtitle);

            header.Add(mark);
            header.Add(titleStack);
            return header;
        }

        private VisualElement BuildSection(string titleText, string descriptionText)
        {
            VisualElement section = new VisualElement();
            section.style.marginTop = 5f;
            section.style.marginBottom = 10f;

            Label title = new Label(titleText);
            title.style.color = IrisTraceTheme.Accent;
            title.style.fontSize = 10f;
            title.style.letterSpacing = 1.1f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 4f;

            Label description = new Label(descriptionText);
            description.style.color = IrisTraceTheme.SecondaryText;
            description.style.fontSize = 11f;
            description.style.whiteSpace = WhiteSpace.Normal;
            section.Add(title);
            section.Add(description);
            return section;
        }

        private VisualElement BuildBasicsPanel()
        {
            VisualElement panel = BuildPanel();
            displayNameField = new TextField("Display Name") { value = "New FPS Hero" };
            maxHealthField = new FloatField("Max Health") { value = 100f };
            StyleField(displayNameField);
            StyleField(maxHealthField);
            panel.Add(displayNameField);
            panel.Add(maxHealthField);
            return panel;
        }

        private VisualElement BuildMovementPanel()
        {
            VisualElement panel = BuildPanel();
            walkSpeedField = new FloatField("Walk Speed") { value = 5f };
            sprintSpeedField = new FloatField("Sprint Speed") { value = 8f };
            jumpHeightField = new FloatField("Jump Height") { value = 1.5f };
            gravityMultiplierField = new FloatField("Gravity Multiplier (1 = project gravity)") { value = 1.8f };
            fallGravityMultiplierField = new FloatField("Fall Gravity Multiplier") { value = 2.2f };
            maximumFallSpeedField = new FloatField("Maximum Fall Speed") { value = 35f };
            groundAccelerationField = new FloatField("Ground Acceleration") { value = 18f };
            groundDecelerationField = new FloatField("Ground Deceleration") { value = 22f };
            airControlField = new FloatField("Air Control (0–1)") { value = 0.35f };
            crouchEnabledField = new Toggle("Enable Crouch") { value = true };
            crouchSpeedField = new FloatField("Crouch Speed") { value = 2.5f };
            crouchedHeightField = new FloatField("Crouched Height") { value = 1.1f };
            crouchTransitionDurationField = new FloatField("Crouch Transition (s)") { value = 0.18f };
            sensitivityField = new FloatField("Mouse Sensitivity") { value = 1f };
            fieldOfViewField = new FloatField("Camera Field of View") { value = 90f };

            StyleField(walkSpeedField);
            StyleField(sprintSpeedField);
            StyleField(jumpHeightField);
            StyleField(gravityMultiplierField);
            StyleField(fallGravityMultiplierField);
            StyleField(maximumFallSpeedField);
            StyleField(groundAccelerationField);
            StyleField(groundDecelerationField);
            StyleField(airControlField);
            StyleField(crouchEnabledField);
            StyleField(crouchSpeedField);
            StyleField(crouchedHeightField);
            StyleField(crouchTransitionDurationField);
            StyleField(sensitivityField);
            StyleField(fieldOfViewField);
            panel.Add(walkSpeedField);
            panel.Add(sprintSpeedField);
            panel.Add(jumpHeightField);
            panel.Add(gravityMultiplierField);
            panel.Add(fallGravityMultiplierField);
            panel.Add(maximumFallSpeedField);
            panel.Add(groundAccelerationField);
            panel.Add(groundDecelerationField);
            panel.Add(airControlField);
            panel.Add(crouchEnabledField);
            panel.Add(crouchSpeedField);
            panel.Add(crouchedHeightField);
            panel.Add(crouchTransitionDurationField);
            panel.Add(sensitivityField);
            panel.Add(fieldOfViewField);
            return panel;
        }

        private VisualElement BuildWeaponPanel()
        {
            VisualElement panel = BuildPanel();
            weaponDamageField = new FloatField("Damage Per Shot") { value = 25f };
            shotsPerSecondField = new FloatField("Shots Per Second") { value = 6f };
            magazineSizeField = new IntegerField("Magazine Size") { value = 12 };
            reserveAmmoField = new IntegerField("Reserve Ammo") { value = 48 };
            reloadDurationField = new FloatField("Reload Duration (s)") { value = 1.4f };
            weaponRangeField = new FloatField("Range (m)") { value = 100f };
            automaticWeaponField = new Toggle("Automatic Fire") { value = true };

            StyleField(weaponDamageField);
            StyleField(shotsPerSecondField);
            StyleField(magazineSizeField);
            StyleField(reserveAmmoField);
            StyleField(reloadDurationField);
            StyleField(weaponRangeField);
            StyleField(automaticWeaponField);
            panel.Add(weaponDamageField);
            panel.Add(shotsPerSecondField);
            panel.Add(magazineSizeField);
            panel.Add(reserveAmmoField);
            panel.Add(reloadDurationField);
            panel.Add(weaponRangeField);
            panel.Add(automaticWeaponField);
            return panel;
        }

        private VisualElement BuildAudioPanel()
        {
            VisualElement panel = BuildPanel();
            Label characterSounds = new Label("CHARACTER SOUNDS");
            characterSounds.style.color = IrisTraceTheme.Accent;
            characterSounds.style.fontSize = 10f;
            characterSounds.style.unityFontStyleAndWeight = FontStyle.Bold;
            characterSounds.style.marginBottom = 7f;
            panel.Add(characterSounds);

            walkFootstepSoundField = AudioField("Walk Footstep");
            sprintFootstepSoundField = AudioField("Sprint Footstep");
            crouchFootstepSoundField = AudioField("Crouch Footstep");
            jumpSoundField = AudioField("Jump");
            landingSoundField = AudioField("Landing");
            crouchStartSoundField = AudioField("Crouch Start");
            crouchEndSoundField = AudioField("Crouch End");
            panel.Add(walkFootstepSoundField);
            panel.Add(sprintFootstepSoundField);
            panel.Add(crouchFootstepSoundField);
            panel.Add(jumpSoundField);
            panel.Add(landingSoundField);
            panel.Add(crouchStartSoundField);
            panel.Add(crouchEndSoundField);

            Label weaponSounds = new Label("WEAPON SOUNDS");
            weaponSounds.style.color = IrisTraceTheme.Accent;
            weaponSounds.style.fontSize = 10f;
            weaponSounds.style.unityFontStyleAndWeight = FontStyle.Bold;
            weaponSounds.style.marginTop = 8f;
            weaponSounds.style.marginBottom = 7f;
            panel.Add(weaponSounds);
            weaponFireSoundField = AudioField("Fire");
            weaponReloadSoundField = AudioField("Reload");
            panel.Add(weaponFireSoundField);
            panel.Add(weaponReloadSoundField);
            return panel;
        }

        private static ObjectField AudioField(string label)
        {
            ObjectField field = new ObjectField(label)
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            StyleField(field);
            return field;
        }

        private VisualElement BuildAbilitiesPanel()
        {
            VisualElement panel = BuildPanel();

            ability1TypeField = new EnumField("Ability Slot 1", AbilityKind.Dash);
            ability1KeyField = KeyField("Ability 1 Binding", "<Keyboard>/q");
            ability1CooldownField = new FloatField("Ability 1 Cooldown (s)") { value = 5f };
            ability1ValueField = new FloatField("Ability 1 Effect (dash metres / heal HP)") { value = 6f };

            ability2TypeField = new EnumField("Ability Slot 2", AbilityKind.Heal);
            ability2KeyField = KeyField("Ability 2 Binding", "<Keyboard>/e");
            ability2CooldownField = new FloatField("Ability 2 Cooldown (s)") { value = 8f };
            ability2ValueField = new FloatField("Ability 2 Effect (dash metres / heal HP)") { value = 25f };

            StyleField(ability1TypeField);
            StyleField(ability1CooldownField);
            StyleField(ability1ValueField);
            StyleField(ability2TypeField);
            StyleField(ability2CooldownField);
            StyleField(ability2ValueField);

            panel.Add(ability1TypeField);
            panel.Add(ability1KeyField);
            panel.Add(ability1CooldownField);
            panel.Add(ability1ValueField);
            panel.Add(ability2TypeField);
            panel.Add(ability2KeyField);
            panel.Add(ability2CooldownField);
            panel.Add(ability2ValueField);
            return panel;
        }

        private VisualElement BuildBindingsPanel()
        {
            VisualElement panel = BuildPanel();
            forwardKeyField = KeyField("Move Forward", "<Keyboard>/w");
            backwardKeyField = KeyField("Move Backward", "<Keyboard>/s");
            leftKeyField = KeyField("Move Left", "<Keyboard>/a");
            rightKeyField = KeyField("Move Right", "<Keyboard>/d");
            jumpKeyField = KeyField("Jump", "<Keyboard>/space");
            sprintKeyField = KeyField("Sprint", "<Keyboard>/leftShift");
            crouchKeyField = KeyField("Crouch (Hold)", "<Keyboard>/leftCtrl");
            fireKeyField = KeyField("Fire", "<Mouse>/leftButton");
            reloadKeyField = KeyField("Reload", "<Keyboard>/r");

            panel.Add(forwardKeyField);
            panel.Add(backwardKeyField);
            panel.Add(leftKeyField);
            panel.Add(rightKeyField);
            panel.Add(jumpKeyField);
            panel.Add(sprintKeyField);
            panel.Add(crouchKeyField);
            panel.Add(fireKeyField);
            panel.Add(reloadKeyField);
            return panel;
        }

        private VisualElement BuildFooterActions()
        {
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.marginTop = 16f;
            footer.style.justifyContent = Justify.FlexEnd;

            Button cancelButton = new Button(Close) { text = "Cancel" };
            StyleSecondaryButton(cancelButton, 90f);
            cancelButton.style.marginRight = 8f;

            Button generateButton = new Button(SaveConfiguration) { text = "Generate Hero" };
            generateButton.style.height = 34f;
            generateButton.style.minWidth = 150f;
            generateButton.style.backgroundColor = IrisTraceTheme.Accent;
            generateButton.style.color = IrisTraceTheme.AccentText;
            generateButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            Radius(generateButton, 9f);
            Border(generateButton, 0f, Color.clear);
            generateButton.RegisterCallback<MouseEnterEvent>(_ => generateButton.style.backgroundColor = IrisTraceTheme.AccentHover);
            generateButton.RegisterCallback<MouseLeaveEvent>(_ => generateButton.style.backgroundColor = IrisTraceTheme.Accent);

            footer.Add(cancelButton);
            footer.Add(generateButton);
            return footer;
        }

        private static VisualElement BuildPanel()
        {
            VisualElement panel = new VisualElement();
            panel.style.backgroundColor = IrisTraceTheme.Surface;
            panel.style.paddingLeft = 13f;
            panel.style.paddingRight = 13f;
            panel.style.paddingTop = 12f;
            panel.style.paddingBottom = 5f;
            panel.style.marginBottom = 18f;
            Radius(panel, 12f);
            Border(panel, 1f, IrisTraceTheme.Border);
            return panel;
        }

        private KeyBindingField KeyField(string label, string defaultPath) => new KeyBindingField(label, defaultPath, StartBinding);

        private void StartBinding(KeyBindingField field)
        {
            CancelBindingCapture(true);
            activeBindingField = field;
            field.SetListening(true);
            // Start after the UI click has finished, otherwise that click itself could be captured as a binding.
            EditorApplication.delayCall += BeginBindingCapture;
        }

        private void BeginBindingCapture()
        {
            if (this == null || activeBindingField == null)
                return;

            captureAction = new InputAction("CaptureBinding", InputActionType.Button);
            captureAction.AddBinding("<Keyboard>/space");
            rebindingOperation = captureAction.PerformInteractiveRebinding(0)
                .WithCancelingThrough("<Keyboard>/escape")
                .WithControlsExcluding("<Mouse>/position")
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .OnComplete(_ =>
                {
                    string path = captureAction != null && captureAction.bindings.Count > 0
                        ? captureAction.bindings[0].effectivePath
                        : null;
                    FinishBindingCapture(path);
                })
                .OnCancel(_ => FinishBindingCapture(null));
            rebindingOperation.Start();
        }

        private void FinishBindingCapture(string path)
        {
            InputActionRebindingExtensions.RebindingOperation operation = rebindingOperation;
            rebindingOperation = null;
            operation?.Dispose();

            if (captureAction != null)
            {
                captureAction.Disable();
                captureAction.Dispose();
                captureAction = null;
            }

            if (activeBindingField != null)
            {
                if (!string.IsNullOrWhiteSpace(path))
                    activeBindingField.SetBinding(path);
                else
                    activeBindingField.SetListening(false);
            }

            activeBindingField = null;
        }

        private void CancelBindingCapture(bool restoreVisual)
        {
            EditorApplication.delayCall -= BeginBindingCapture;
            if (rebindingOperation != null)
            {
                InputActionRebindingExtensions.RebindingOperation operation = rebindingOperation;
                rebindingOperation = null;
                operation.Cancel();
                operation.Dispose();
            }

            if (captureAction != null)
            {
                captureAction.Disable();
                captureAction.Dispose();
                captureAction = null;
            }

            if (restoreVisual && activeBindingField != null)
                activeBindingField.SetListening(false);
            activeBindingField = null;
        }

        private static void StyleField(VisualElement field)
        {
            field.style.marginBottom = 9f;
            field.style.color = IrisTraceTheme.PrimaryText;
            Label label = field.Q<Label>(className: "unity-base-field__label");
            if (label != null)
                label.style.color = IrisTraceTheme.SecondaryText;

            VisualElement input = field.Q<VisualElement>(className: "unity-base-text-field__input")
                                  ?? field.Q<VisualElement>(className: "unity-enum-field__input");
            if (input != null)
            {
                input.style.backgroundColor = IrisTraceTheme.SurfaceStrong;
                input.style.color = IrisTraceTheme.PrimaryText;
                Radius(input, 6f);
            }
        }

        private static void StyleSecondaryButton(Button button, float width)
        {
            button.style.height = 30f;
            button.style.minWidth = width;
            button.style.backgroundColor = IrisTraceTheme.SurfaceStrong;
            button.style.color = IrisTraceTheme.PrimaryText;
            button.style.fontSize = 10f;
            Radius(button, 8f);
            Border(button, 1f, IrisTraceTheme.Border);
        }

        private static void Radius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }

        private static void Border(VisualElement element, float width, Color color)
        {
            element.style.borderTopWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderTopColor = color;
            element.style.borderRightColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
        }

        private void SaveConfiguration()
        {
            if (!ValidateInput())
                return;

            string safeName = SanitizeFileName(displayNameField.value.Trim());
            string characterFolder = "Assets/IrisTraceFPS_Generated/Configurations/Characters";
            EnsureAssetFolder(characterFolder);

            CharacterDefinition definition = CreateInstance<CharacterDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            SetString(serialized, "id", Guid.NewGuid().ToString("N"));
            SetString(serialized, "displayName", displayNameField.value.Trim());
            SetFloat(serialized, "maxHealth", maxHealthField.value);
            SetFloat(serialized, "walkSpeed", walkSpeedField.value);
            SetFloat(serialized, "sprintSpeed", sprintSpeedField.value);
            SetFloat(serialized, "jumpHeight", jumpHeightField.value);
            SetFloat(serialized, "gravityMultiplier", gravityMultiplierField.value);
            SetFloat(serialized, "fallGravityMultiplier", fallGravityMultiplierField.value);
            SetFloat(serialized, "maximumFallSpeed", maximumFallSpeedField.value);
            SetFloat(serialized, "groundAcceleration", groundAccelerationField.value);
            SetFloat(serialized, "groundDeceleration", groundDecelerationField.value);
            SetFloat(serialized, "airControl", airControlField.value);
            SetBool(serialized, "crouchEnabled", crouchEnabledField.value);
            SetFloat(serialized, "crouchSpeed", crouchSpeedField.value);
            SetFloat(serialized, "crouchedHeight", crouchedHeightField.value);
            SetFloat(serialized, "crouchTransitionDuration", crouchTransitionDurationField.value);
            SetFloat(serialized, "mouseSensitivity", sensitivityField.value);
            SetFloat(serialized, "cameraFieldOfView", fieldOfViewField.value);
            SetObjectReference(serialized, "walkFootstepClip", walkFootstepSoundField.value);
            SetObjectReference(serialized, "sprintFootstepClip", sprintFootstepSoundField.value);
            SetObjectReference(serialized, "crouchFootstepClip", crouchFootstepSoundField.value);
            SetObjectReference(serialized, "jumpClip", jumpSoundField.value);
            SetObjectReference(serialized, "landingClip", landingSoundField.value);
            SetObjectReference(serialized, "crouchStartClip", crouchStartSoundField.value);
            SetObjectReference(serialized, "crouchEndClip", crouchEndSoundField.value);
            SetString(serialized, "moveForwardBinding", forwardKeyField.Value);
            SetString(serialized, "moveBackwardBinding", backwardKeyField.Value);
            SetString(serialized, "moveLeftBinding", leftKeyField.Value);
            SetString(serialized, "moveRightBinding", rightKeyField.Value);
            SetString(serialized, "jumpBinding", jumpKeyField.Value);
            SetString(serialized, "sprintBinding", sprintKeyField.Value);
            SetString(serialized, "crouchBinding", crouchKeyField.Value);
            SetString(serialized, "fireBinding", fireKeyField.Value);
            SetString(serialized, "reloadBinding", reloadKeyField.Value);
            SetInt(serialized, "ability1Type", (int)GetAbility1Type());
            SetString(serialized, "ability1Binding", ability1KeyField.Value);
            SetFloat(serialized, "ability1Cooldown", ability1CooldownField.value);
            SetFloat(serialized, "ability1Value", ability1ValueField.value);
            SetInt(serialized, "ability2Type", (int)GetAbility2Type());
            SetString(serialized, "ability2Binding", ability2KeyField.Value);
            SetFloat(serialized, "ability2Cooldown", ability2CooldownField.value);
            SetFloat(serialized, "ability2Value", ability2ValueField.value);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string characterPath = AssetDatabase.GenerateUniqueAssetPath($"{characterFolder}/{safeName}.asset");
            AssetDatabase.CreateAsset(definition, characterPath);

            string weaponFolder = "Assets/IrisTraceFPS_Generated/Configurations/Weapons";
            EnsureAssetFolder(weaponFolder);
            WeaponDefinition weaponDefinition = CreateInstance<WeaponDefinition>();
            SerializedObject weaponSerialized = new SerializedObject(weaponDefinition);
            SetString(weaponSerialized, "id", Guid.NewGuid().ToString("N"));
            SetString(weaponSerialized, "displayName", safeName + " Primary Weapon");
            SetFloat(weaponSerialized, "damage", weaponDamageField.value);
            SetFloat(weaponSerialized, "shotsPerSecond", shotsPerSecondField.value);
            SetInt(weaponSerialized, "magazineSize", magazineSizeField.value);
            SetInt(weaponSerialized, "reserveAmmo", reserveAmmoField.value);
            SetFloat(weaponSerialized, "reloadDuration", reloadDurationField.value);
            SetFloat(weaponSerialized, "range", weaponRangeField.value);
            SetBool(weaponSerialized, "automatic", automaticWeaponField.value);
            SetObjectReference(weaponSerialized, "fireClip", weaponFireSoundField.value);
            SetObjectReference(weaponSerialized, "reloadClip", weaponReloadSoundField.value);
            weaponSerialized.ApplyModifiedPropertiesWithoutUndo();

            string weaponPath = AssetDatabase.GenerateUniqueAssetPath($"{weaponFolder}/{safeName}_PrimaryWeapon.asset");
            AssetDatabase.CreateAsset(weaponDefinition, weaponPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            try
            {
                FPSHeroPrefabGenerator.Generate(definition, weaponDefinition, safeName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Hero Generation Failed",
                    "The configuration was saved, but the prefab could not be generated. See the Console for details.", "OK");
                return;
            }

            EditorUtility.DisplayDialog("Hero Generated",
                $"'{definition.DisplayName}' was saved as a prefab and added to the active scene at (0, 0, 0).\n\nMovement, camera, health, weapon, reload, and selected abilities are ready.", "OK");
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(displayNameField.value))
            {
                EditorUtility.DisplayDialog("Missing Name", "Enter a display name for the character.", "OK");
                return false;
            }

            if (maxHealthField.value <= 0f || walkSpeedField.value <= 0f || sprintSpeedField.value <= 0f ||
                jumpHeightField.value <= 0f || groundAccelerationField.value <= 0f ||
                groundDecelerationField.value <= 0f || crouchTransitionDurationField.value < 0.05f ||
                sensitivityField.value <= 0f)
            {
                EditorUtility.DisplayDialog("Invalid Values", "Health, movement speed, jump height, acceleration, deceleration, crouch transition and sensitivity must be greater than zero.", "OK");
                return false;
            }

            if (gravityMultiplierField.value < 0.1f || gravityMultiplierField.value > 5f ||
                fallGravityMultiplierField.value < 1f || fallGravityMultiplierField.value > 4f ||
                maximumFallSpeedField.value < 5f || maximumFallSpeedField.value > 100f)
            {
                EditorUtility.DisplayDialog("Invalid Gravity Settings",
                    "Gravity Multiplier must be 0.1–5, Fall Gravity Multiplier 1–4, and Maximum Fall Speed 5–100.", "OK");
                return false;
            }

            if (airControlField.value < 0f || airControlField.value > 1f)
            {
                EditorUtility.DisplayDialog("Invalid Air Control", "Air Control must be between 0 and 1.", "OK");
                return false;
            }

            if (sprintSpeedField.value < walkSpeedField.value)
            {
                EditorUtility.DisplayDialog("Invalid Movement", "Sprint Speed must be greater than or equal to Walk Speed.", "OK");
                return false;
            }

            if (crouchEnabledField.value && (crouchSpeedField.value <= 0f ||
                crouchedHeightField.value <= 0.65f || crouchedHeightField.value >= 1.75f))
            {
                EditorUtility.DisplayDialog("Invalid Crouch Settings",
                    "Crouch Speed must be positive. Crouched Height must be greater than 0.65 and less than the 1.8 m standing height.", "OK");
                return false;
            }

            if (fieldOfViewField.value < 60f || fieldOfViewField.value > 120f)
            {
                EditorUtility.DisplayDialog("Invalid Camera FOV", "Camera Field of View must be between 60 and 120 degrees.", "OK");
                return false;
            }

            if (weaponDamageField.value <= 0f || shotsPerSecondField.value <= 0f || magazineSizeField.value < 1 ||
                reserveAmmoField.value < 0 || reloadDurationField.value <= 0f || weaponRangeField.value <= 0f)
            {
                EditorUtility.DisplayDialog("Invalid Weapon Settings", "Damage, fire rate, magazine size, reload duration, and range must be positive. Reserve ammo cannot be negative.", "OK");
                return false;
            }

            bool ability1Enabled = GetAbility1Type() != AbilityKind.None;
            bool ability2Enabled = GetAbility2Type() != AbilityKind.None;
            if ((ability1Enabled && (ability1CooldownField.value <= 0f || ability1ValueField.value <= 0f)) ||
                (ability2Enabled && (ability2CooldownField.value <= 0f || ability2ValueField.value <= 0f)))
            {
                EditorUtility.DisplayDialog("Invalid Ability Settings", "Enabled abilities need a positive effect value and cooldown.", "OK");
                return false;
            }

            List<KeyBindingField> fields = new List<KeyBindingField>
            {
                forwardKeyField, backwardKeyField, leftKeyField, rightKeyField,
                jumpKeyField, sprintKeyField, fireKeyField, reloadKeyField
            };
            List<string> labels = new List<string>
            {
                "Move Forward", "Move Backward", "Move Left", "Move Right",
                "Jump", "Sprint", "Fire", "Reload"
            };
            if (crouchEnabledField.value) { fields.Add(crouchKeyField); labels.Add("Crouch"); }
            if (ability1Enabled) { fields.Add(ability1KeyField); labels.Add("Ability 1"); }
            if (ability2Enabled) { fields.Add(ability2KeyField); labels.Add("Ability 2"); }

            Dictionary<string, string> assignedPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < fields.Count; i++)
            {
                string path = fields[i].Value;
                if (string.IsNullOrWhiteSpace(path))
                {
                    EditorUtility.DisplayDialog("Invalid Binding", $"Assign an input to '{labels[i]}'.", "OK");
                    return false;
                }

                if (assignedPaths.TryGetValue(path, out string previousAction))
                {
                    EditorUtility.DisplayDialog("Binding Conflict",
                        $"'{FormatBinding(path)}' is assigned to both '{previousAction}' and '{labels[i]}'. Choose a different input.", "OK");
                    return false;
                }

                assignedPaths.Add(path, labels[i]);
            }

            return true;
        }

        private AbilityKind GetAbility1Type()
        {
            return ability1TypeField != null && ability1TypeField.value is AbilityKind value ? value : AbilityKind.None;
        }

        private AbilityKind GetAbility2Type()
        {
            return ability2TypeField != null && ability2TypeField.value is AbilityKind value ? value : AbilityKind.None;
        }

        private sealed class KeyBindingField : VisualElement
        {
            private readonly Button bindingButton;
            private readonly Action<KeyBindingField> onStartBinding;
            public string Value { get; private set; }

            public KeyBindingField(string labelText, string initialPath, Action<KeyBindingField> startBinding)
            {
                Value = initialPath;
                onStartBinding = startBinding;
                style.flexDirection = FlexDirection.Row;
                style.alignItems = Align.Center;
                style.minHeight = 32f;
                style.marginBottom = 8f;

                Label label = new Label(labelText);
                label.style.flexGrow = 1f;
                label.style.color = IrisTraceTheme.SecondaryText;
                label.style.fontSize = 11f;

                bindingButton = new Button(() => onStartBinding(this));
                bindingButton.style.height = 29f;
                bindingButton.style.minWidth = 132f;
                bindingButton.style.backgroundColor = IrisTraceTheme.SurfaceStrong;
                bindingButton.style.color = IrisTraceTheme.PrimaryText;
                bindingButton.style.fontSize = 10f;
                bindingButton.style.unityFontStyleAndWeight = FontStyle.Bold;
                Radius(bindingButton, 8f);
                Border(bindingButton, 1f, IrisTraceTheme.Border);

                Add(label);
                Add(bindingButton);
                SetListening(false);
            }

            public void SetListening(bool listening)
            {
                bindingButton.text = listening ? "PRESS INPUT… ESC CANCELS" : FormatBinding(Value);
                bindingButton.style.backgroundColor = listening ? IrisTraceTheme.Accent : IrisTraceTheme.SurfaceStrong;
                bindingButton.style.color = listening ? IrisTraceTheme.AccentText : IrisTraceTheme.PrimaryText;
                Border(bindingButton, 1f, listening ? IrisTraceTheme.Accent : IrisTraceTheme.Border);
            }

            public void SetBinding(string path)
            {
                Value = path;
                SetListening(false);
            }
        }

        private static string FormatBinding(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "UNASSIGNED";

            string control = path.Substring(path.LastIndexOf('/') + 1);
            switch (control.ToLowerInvariant())
            {
                case "leftbutton": return "LMB";
                case "rightbutton": return "RMB";
                case "middlebutton": return "MMB";
                case "space": return "SPACE";
                case "leftshift": return "LEFT SHIFT";
                case "rightshift": return "RIGHT SHIFT";
                case "leftctrl": return "LEFT CTRL";
                case "rightctrl": return "RIGHT CTRL";
                case "leftalt": return "LEFT ALT";
                case "rightalt": return "RIGHT ALT";
                case "enter": return "ENTER";
                case "numpadenter": return "NUM ENTER";
                case "escape": return "ESC";
                case "digit0": return "0";
                case "digit1": return "1";
                case "digit2": return "2";
                case "digit3": return "3";
                case "digit4": return "4";
                case "digit5": return "5";
                case "digit6": return "6";
                case "digit7": return "7";
                case "digit8": return "8";
                case "digit9": return "9";
                default: return control.Replace("_", " ").ToUpperInvariant();
            }
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static void SetFloat(SerializedObject serialized, string propertyName, float value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.floatValue = value;
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetObjectReference(SerializedObject serialized, string propertyName, UnityEngine.Object value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.objectReferenceValue = value;
        }

        private static void SetBool(SerializedObject serialized, string propertyName, bool value)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);
            if (property != null)
                property.boolValue = value;
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

        private static string SanitizeFileName(string value)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            List<char> invalid = new List<char>(invalidChars) { '/', '\\', ':', '*', '?', '"', '<', '>', '|' };
            char[] chars = value.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (invalid.Contains(chars[i]))
                    chars[i] = '_';
            }

            string result = new string(chars).Trim().TrimEnd('.');
            return string.IsNullOrEmpty(result) ? "New FPS Hero" : result;
        }
    }
}
