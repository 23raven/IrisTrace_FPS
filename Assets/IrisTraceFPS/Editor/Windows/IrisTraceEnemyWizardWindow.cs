using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using IrisTrace.Editor.Generators;
using IrisTrace.Editor.UI;
using IrisTrace.Runtime.Enemies;

namespace IrisTrace.Editor.Windows
{
    public sealed class IrisTraceEnemyWizardWindow : EditorWindow
    {
        private TextField displayNameField;
        private EnumField behaviorField;
        private FloatField maxHealthField;
        private FloatField moveSpeedField;
        private FloatField detectionRangeField;
        private FloatField attackRangeField;
        private FloatField attackDamageField;
        private FloatField attackCooldownField;
        private VisualElement aiFields;

        [MenuItem("Window/IrisTrace FPS/Create Enemy")]
        public static void Open()
        {
            IrisTraceEnemyWizardWindow window = GetWindow<IrisTraceEnemyWizardWindow>("Create Enemy");
            window.minSize = new Vector2(420f, 540f);
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
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            rootVisualElement.Add(scroll);

            VisualElement content = new VisualElement();
            content.style.paddingLeft = 20f;
            content.style.paddingRight = 20f;
            content.style.paddingTop = 18f;
            content.style.paddingBottom = 20f;
            scroll.Add(content);

            content.Add(BuildHeader());
            content.Add(BuildSection("ENEMY SETUP", "Choose a stationary target or a simple enemy bot."));
            content.Add(BuildSettingsPanel());
            content.Add(BuildSection("BASIC AI", "The bot detects the generated hero, follows and attacks within range."));
            content.Add(BuildAISettingsPanel());
            content.Add(BuildFooterActions());

            UpdateAIFieldVisibility();
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
            Label title = new Label("Create Enemy");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 17f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label subtitle = new Label("IRISTRACE FPS / ENEMY CONFIGURATION");
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

        private VisualElement BuildSettingsPanel()
        {
            VisualElement panel = BuildPanel();
            displayNameField = new TextField("Display Name") { value = "Training Enemy" };
            behaviorField = new EnumField("Behavior", EnemyBehaviorMode.StationaryTarget);
            maxHealthField = new FloatField("Max Health") { value = 100f };

            StyleField(displayNameField);
            StyleField(behaviorField);
            StyleField(maxHealthField);

            behaviorField.RegisterValueChangedCallback(_ => UpdateAIFieldVisibility());
            panel.Add(displayNameField);
            panel.Add(behaviorField);
            panel.Add(maxHealthField);
            return panel;
        }

        private VisualElement BuildAISettingsPanel()
        {
            aiFields = BuildPanel();
            moveSpeedField = new FloatField("Move Speed") { value = 3.5f };
            detectionRangeField = new FloatField("Detection Range") { value = 14f };
            attackRangeField = new FloatField("Attack Range") { value = 2.2f };
            attackDamageField = new FloatField("Damage Per Attack") { value = 10f };
            attackCooldownField = new FloatField("Attack Cooldown (s)") { value = 1.25f };

            StyleField(moveSpeedField);
            StyleField(detectionRangeField);
            StyleField(attackRangeField);
            StyleField(attackDamageField);
            StyleField(attackCooldownField);

            aiFields.Add(moveSpeedField);
            aiFields.Add(detectionRangeField);
            aiFields.Add(attackRangeField);
            aiFields.Add(attackDamageField);
            aiFields.Add(attackCooldownField);
            return aiFields;
        }

        private VisualElement BuildFooterActions()
        {
            VisualElement footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.marginTop = 12f;
            footer.style.justifyContent = Justify.FlexEnd;

            Button cancelButton = new Button(Close) { text = "Cancel" };
            StyleSecondaryButton(cancelButton, 90f);
            cancelButton.style.marginRight = 8f;

            Button generateButton = new Button(GenerateEnemy) { text = "Generate Enemy" };
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

        private void UpdateAIFieldVisibility()
        {
            if (aiFields == null || behaviorField == null)
                return;

            bool showAI = (EnemyBehaviorMode)behaviorField.value == EnemyBehaviorMode.BasicAI;
            aiFields.style.display = showAI ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static VisualElement BuildPanel()
        {
            VisualElement panel = new VisualElement();
            panel.style.backgroundColor = IrisTraceTheme.Surface;
            panel.style.paddingLeft = 13f;
            panel.style.paddingRight = 13f;
            panel.style.paddingTop = 12f;
            panel.style.paddingBottom = 5f;
            panel.style.marginBottom = 16f;
            Radius(panel, 12f);
            Border(panel, 1f, IrisTraceTheme.Border);
            return panel;
        }

        private static void StyleField(VisualElement field)
        {
            field.style.marginBottom = 9f;
            field.style.color = IrisTraceTheme.PrimaryText;
            Label label = field.Q<Label>(className: "unity-base-field__label");
            if (label != null)
                label.style.color = IrisTraceTheme.SecondaryText;
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

        private void GenerateEnemy()
        {
            if (!ValidateInput())
                return;

            string safeName = SanitizeFileName(displayNameField.value.Trim());
            string configurationFolder = "Assets/IrisTraceFPS_Generated/Configurations/Enemies";
            EnsureAssetFolder(configurationFolder);

            EnemyDefinition definition = CreateInstance<EnemyDefinition>();
            definition.Configure(
                Guid.NewGuid().ToString("N"),
                displayNameField.value.Trim(),
                (EnemyBehaviorMode)behaviorField.value,
                maxHealthField.value,
                moveSpeedField.value,
                detectionRangeField.value,
                attackRangeField.value,
                attackDamageField.value,
                attackCooldownField.value);

            string configurationPath = AssetDatabase.GenerateUniqueAssetPath($"{configurationFolder}/{safeName}.asset");
            AssetDatabase.CreateAsset(definition, configurationPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            try
            {
                EnemyPrefabGenerator.Generate(definition, safeName);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("Enemy Generation Failed",
                    "The configuration was saved, but the enemy prefab could not be generated. Check the Console for details.", "OK");
                return;
            }

            string behaviorDescription = definition.Behavior == EnemyBehaviorMode.BasicAI
                ? "Basic AI is enabled. It uses NavMesh when available and falls back to direct pursuit in unbaked test scenes."
                : "Stationary target is ready for weapon testing.";
            EditorUtility.DisplayDialog("Enemy Generated",
                $"'{definition.DisplayName}' was saved as a prefab and placed in the active scene.\n\n{behaviorDescription}", "OK");
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(displayNameField.value))
            {
                EditorUtility.DisplayDialog("Missing Name", "Enter a display name for the enemy.", "OK");
                return false;
            }
            if (maxHealthField.value <= 0f)
            {
                EditorUtility.DisplayDialog("Invalid Health", "Max Health must be greater than zero.", "OK");
                return false;
            }

            if ((EnemyBehaviorMode)behaviorField.value == EnemyBehaviorMode.BasicAI)
            {
                if (moveSpeedField.value <= 0f || detectionRangeField.value <= 0f || attackRangeField.value <= 0f || attackCooldownField.value <= 0f || attackDamageField.value < 0f)
                {
                    EditorUtility.DisplayDialog("Invalid AI Settings", "Move speed, detection range, attack range and cooldown must be positive. Damage cannot be negative.", "OK");
                    return false;
                }
            }
            return true;
        }

        private static string SanitizeFileName(string value)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value.Trim();
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
