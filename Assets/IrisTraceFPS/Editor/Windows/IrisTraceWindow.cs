using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using IrisTrace.Editor.UI;
using IrisTrace.Editor.Generators;

namespace IrisTrace.Editor.Windows
{
    public sealed class IrisTraceWindow : EditorWindow
    {
        [MenuItem("Window/IrisTrace FPS/Open Generator")]
        public static void OpenWindow()
        {
            IrisTraceWindow window = GetWindow<IrisTraceWindow>();
            window.titleContent = new GUIContent("IrisTrace FPS");
            window.minSize = new Vector2(420f, 510f);
            window.Show();
        }

        private void OnEnable()
        {
            IrisTraceTheme.ThemeChanged += RebuildForTheme;
        }

        private void OnDisable()
        {
            IrisTraceTheme.ThemeChanged -= RebuildForTheme;
        }

        private void RebuildForTheme()
        {
            CreateGUI();
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
            content.style.paddingTop = 20f;
            content.style.paddingBottom = 16f;
            content.style.flexGrow = 1f;
            scroll.Add(content);

            content.Add(BuildHeader());
            content.Add(BuildIntro());
            content.Add(BuildDivider());
            content.Add(BuildSectionLabel("CREATE"));
            content.Add(BuildActionCard(
                "01",
                "FPS Hero",
                "Configure a player character, movement, camera and default controls.",
                "Create FPS Hero",
                IrisTraceHeroWizardWindow.Open,
                true));
            content.Add(BuildActionCard(
                "02",
                "Enemy",
                "Create a stationary target to test shots, damage and weapon range.",
                "Create Enemy",
                CreateTrainingTarget,
                false));
            content.Add(BuildStatus());
            content.Add(BuildFooter());
        }

        private VisualElement BuildHeader()
        {
            VisualElement header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 30f;

            VisualElement mark = new VisualElement();
            mark.style.width = 42f;
            mark.style.height = 42f;
            mark.style.marginRight = 12f;
            mark.style.backgroundColor = IrisTraceTheme.AccentHover;
            Radius(mark, 13f);
            mark.style.alignItems = Align.Center;
            mark.style.justifyContent = Justify.Center;

            Label markText = new Label("I");
            markText.style.color = IrisTraceTheme.AccentText;
            markText.style.fontSize = 19f;
            markText.style.unityFontStyleAndWeight = FontStyle.Bold;
            mark.Add(markText);

            VisualElement titles = new VisualElement();
            titles.style.flexGrow = 1f;

            Label title = new Label("IrisTrace FPS");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 19f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 2f;

            Label subtitle = new Label("FPS GENERATION FRAMEWORK");
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 9f;
            subtitle.style.letterSpacing = 1.2f;

            titles.Add(title);
            titles.Add(subtitle);

            Button themeButton = new Button(IrisTraceTheme.Toggle)
            {
                text = IrisTraceTheme.IsDark ? "Light Mode" : "Dark Mode"
            };
            themeButton.style.height = 30f;
            themeButton.style.minWidth = 83f;
            themeButton.style.backgroundColor = IrisTraceTheme.SurfaceStrong;
            themeButton.style.color = IrisTraceTheme.PrimaryText;
            themeButton.style.fontSize = 10f;
            Radius(themeButton, 8f);
            Border(themeButton, 1f, IrisTraceTheme.Border);

            header.Add(mark);
            header.Add(titles);
            header.Add(themeButton);
            return header;
        }

        private VisualElement BuildIntro()
        {
            VisualElement intro = new VisualElement();
            intro.style.marginBottom = 22f;

            Label eyebrow = new Label("CHARACTER WORKSPACE");
            eyebrow.style.color = IrisTraceTheme.Accent;
            eyebrow.style.fontSize = 10f;
            eyebrow.style.letterSpacing = 1.1f;
            eyebrow.style.marginBottom = 8f;

            Label title = new Label("Build your next FPS character.");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 23f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.marginBottom = 7f;

            Label description = new Label("Create configurable gameplay assets without manually wiring components.");
            description.style.color = IrisTraceTheme.SecondaryText;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.fontSize = 12f;

            intro.Add(eyebrow);
            intro.Add(title);
            intro.Add(description);
            return intro;
        }

        private VisualElement BuildDivider()
        {
            VisualElement divider = new VisualElement();
            divider.style.height = 1f;
            divider.style.backgroundColor = IrisTraceTheme.Border;
            divider.style.marginBottom = 20f;
            return divider;
        }

        private static Label BuildSectionLabel(string text)
        {
            Label label = new Label(text);
            label.style.color = IrisTraceTheme.SecondaryText;
            label.style.fontSize = 10f;
            label.style.letterSpacing = 1.2f;
            label.style.marginBottom = 10f;
            return label;
        }

        private VisualElement BuildActionCard(
            string number,
            string titleText,
            string descriptionText,
            string buttonText,
            Action action,
            bool primary)
        {
            VisualElement card = new VisualElement();
            card.style.backgroundColor = IrisTraceTheme.Surface;
            card.style.paddingLeft = 15f;
            card.style.paddingRight = 15f;
            card.style.paddingTop = 15f;
            card.style.paddingBottom = 15f;
            card.style.marginBottom = 12f;
            Radius(card, 13f);
            Border(card, 1f, IrisTraceTheme.Border);

            VisualElement heading = new VisualElement();
            heading.style.flexDirection = FlexDirection.Row;
            heading.style.alignItems = Align.Center;
            heading.style.marginBottom = 7f;

            VisualElement numberBadge = new VisualElement();
            numberBadge.style.width = 29f;
            numberBadge.style.height = 29f;
            numberBadge.style.marginRight = 10f;
            numberBadge.style.backgroundColor = new Color(
                IrisTraceTheme.AccentHover.r,
                IrisTraceTheme.AccentHover.g,
                IrisTraceTheme.AccentHover.b,
                0.24f);
            Radius(numberBadge, 9f);
            numberBadge.style.alignItems = Align.Center;
            numberBadge.style.justifyContent = Justify.Center;

            Label numberLabel = new Label(number);
            numberLabel.style.color = IrisTraceTheme.Accent;
            numberLabel.style.fontSize = 10f;
            numberLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            numberBadge.Add(numberLabel);

            Label title = new Label(titleText);
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 15f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.Add(numberBadge);
            heading.Add(title);

            Label description = new Label(descriptionText);
            description.style.color = IrisTraceTheme.SecondaryText;
            description.style.fontSize = 11f;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 15f;

            Button button = new Button(action) { text = buttonText + "   →" };
            button.style.height = 36f;
            button.style.fontSize = 12f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            Radius(button, 9f);
            Border(button, 0f, Color.clear);

            if (primary)
            {
                button.style.backgroundColor = IrisTraceTheme.Accent;
                button.style.color = IrisTraceTheme.AccentText;
                button.RegisterCallback<MouseEnterEvent>(_ =>
                {
                    button.style.backgroundColor = IrisTraceTheme.AccentHover;
                    button.style.color = IrisTraceTheme.AccentText;
                });
                button.RegisterCallback<MouseLeaveEvent>(_ =>
                {
                    button.style.backgroundColor = IrisTraceTheme.Accent;
                    button.style.color = IrisTraceTheme.AccentText;
                });
            }
            else
            {
                button.style.backgroundColor = IrisTraceTheme.SurfaceStrong;
                button.style.color = IrisTraceTheme.PrimaryText;
                button.RegisterCallback<MouseEnterEvent>(_ => button.style.backgroundColor = IrisTraceTheme.AccentHover);
                button.RegisterCallback<MouseLeaveEvent>(_ => button.style.backgroundColor = IrisTraceTheme.SurfaceStrong);
            }

            card.Add(heading);
            card.Add(description);
            card.Add(button);
            return card;
        }

        private VisualElement BuildStatus()
        {
            VisualElement status = new VisualElement();
            status.style.flexDirection = FlexDirection.Row;
            status.style.alignItems = Align.Center;
            status.style.marginTop = 9f;
            status.style.marginBottom = 22f;

            VisualElement dot = new VisualElement();
            dot.style.width = 7f;
            dot.style.height = 7f;
            dot.style.marginRight = 8f;
            dot.style.backgroundColor = IrisTraceTheme.Accent;
            Radius(dot, 4f);

            Label statusText = new Label("EDITOR READY  /  CONFIGURATION WORKFLOW IN PROGRESS");
            statusText.style.color = IrisTraceTheme.SecondaryText;
            statusText.style.fontSize = 9f;
            statusText.style.letterSpacing = 0.4f;
            statusText.style.whiteSpace = WhiteSpace.Normal;

            status.Add(dot);
            status.Add(statusText);
            return status;
        }

        private VisualElement BuildFooter()
        {
            VisualElement footer = new VisualElement();
            footer.style.paddingTop = 12f;
            footer.style.borderTopWidth = 1f;
            footer.style.borderTopColor = IrisTraceTheme.Border;

            Label text = new Label("IRISTRACE FPS    /    PROTOTYPE 0.4");
            text.style.color = IrisTraceTheme.SecondaryText;
            text.style.fontSize = 9f;
            text.style.letterSpacing = 0.5f;
            footer.Add(text);
            return footer;
        }

        private static void CreateTrainingTarget()
        {
            try
            {
                GameObject target = TrainingTargetPrefabGenerator.Generate();
                if (target != null)
                    Debug.Log("IrisTrace FPS: training target generated.", target);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Could Not Create Target",
                    "IrisTrace FPS could not create the training target. Check the Console for details.",
                    "OK");
            }
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
    }
}
