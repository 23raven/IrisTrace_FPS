using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using IrisTrace.Editor.Generators;
using IrisTrace.Editor.UI;

namespace IrisTrace.Editor.Windows
{
    /// <summary>Single dockable workspace for all IrisTrace FPS editor tools.</summary>
    public sealed class IrisTraceWindow : EditorWindow
    {
        private const string Overview = "Overview";
        private VisualElement contentHost;
        private VisualElement sidebar;
        private readonly Dictionary<string, VisualElement> pageViews = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, EditorWindow> embeddedTools = new Dictionary<string, EditorWindow>();
        private readonly Dictionary<string, Button> navigationButtons = new Dictionary<string, Button>();
        private string currentPage = Overview;

        [MenuItem("Window/IrisTrace FPS/Open Workspace")]
        public static void OpenWindow()
        {
            OpenPage(Overview);
        }

        // Backwards-compatible menu entry; it now opens the unified workspace.
        [MenuItem("Window/IrisTrace FPS/Open Generator")]
        public static void OpenGeneratorAlias()
        {
            OpenPage(Overview);
        }

        public static void OpenPage(string pageId)
        {
            IrisTraceWindow window = GetWindow<IrisTraceWindow>();
            window.titleContent = new GUIContent("IrisTrace FPS");
            window.minSize = new Vector2(780f, 500f);
            window.currentPage = string.IsNullOrEmpty(pageId) ? Overview : pageId;
            window.Show();
            if (window.contentHost != null)
                window.NavigateTo(window.currentPage);
        }

        public void CreateGUI()
        {
            DisposeEmbeddedTools();
            pageViews.Clear();
            navigationButtons.Clear();

            rootVisualElement.Clear();
            rootVisualElement.style.flexGrow = 1f;
            rootVisualElement.style.backgroundColor = IrisTraceTheme.Background;
            rootVisualElement.style.flexDirection = FlexDirection.Row;

            BuildSidebar();
            contentHost = new VisualElement();
            contentHost.style.flexGrow = 1f;
            contentHost.style.minWidth = 0f;
            contentHost.style.backgroundColor = IrisTraceTheme.Background;
            rootVisualElement.Add(contentHost);

            NavigateTo(currentPage);
        }

        private void BuildSidebar()
        {
            sidebar = new VisualElement();
            sidebar.style.width = 190f;
            sidebar.style.minWidth = 170f;
            sidebar.style.flexShrink = 0f;
            sidebar.style.backgroundColor = IrisTraceTheme.Surface;
            sidebar.style.paddingLeft = 10f;
            sidebar.style.paddingRight = 10f;
            sidebar.style.paddingTop = 14f;
            sidebar.style.paddingBottom = 10f;
            sidebar.style.borderRightWidth = 1f;
            sidebar.style.borderRightColor = IrisTraceTheme.Border;
            rootVisualElement.Add(sidebar);

            VisualElement brand = new VisualElement();
            brand.style.flexDirection = FlexDirection.Row;
            brand.style.alignItems = Align.Center;
            brand.style.marginBottom = 18f;
            brand.style.paddingLeft = 4f;

            VisualElement mark = new VisualElement();
            mark.style.width = 30f;
            mark.style.height = 30f;
            mark.style.flexShrink = 0f;
            mark.style.marginRight = 8f;
            mark.style.alignItems = Align.Center;
            mark.style.justifyContent = Justify.Center;
            mark.style.backgroundColor = IrisTraceTheme.Accent;
            mark.style.borderTopLeftRadius = 8f;
            mark.style.borderTopRightRadius = 8f;
            mark.style.borderBottomLeftRadius = 8f;
            mark.style.borderBottomRightRadius = 8f;
            Label markLabel = new Label("I");
            markLabel.style.color = IrisTraceTheme.AccentText;
            markLabel.style.fontSize = 17f;
            markLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            mark.Add(markLabel);

            VisualElement brandText = new VisualElement();
            brandText.style.flexGrow = 1f;
            Label brandName = new Label("IrisTrace");
            brandName.style.color = IrisTraceTheme.PrimaryText;
            brandName.style.fontSize = 14f;
            brandName.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label brandSub = new Label("FPS FRAMEWORK");
            brandSub.style.color = IrisTraceTheme.SecondaryText;
            brandSub.style.fontSize = 8f;
            brandSub.style.letterSpacing = 0.6f;
            brandText.Add(brandName);
            brandText.Add(brandSub);
            brand.Add(mark);
            brand.Add(brandText);
            sidebar.Add(brand);

            ScrollView navScroll = new ScrollView(ScrollViewMode.Vertical);
            navScroll.style.flexGrow = 1f;
            navScroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            sidebar.Add(navScroll);

            AddNavGroup(navScroll, "WORKSPACE", new[] { "Overview", "Create FPS Hero", "Create Enemy" });
            AddNavGroup(navScroll, "LIBRARIES", new[] { "Characters", "Weapons", "Enemies" });
            AddNavGroup(navScroll, "TESTING", new[] { "Test Arena", "Project Validation", "Release Audit" });

            Label footer = new Label("v0.1  •  MVP PREVIEW");
            footer.style.color = IrisTraceTheme.SecondaryText;
            footer.style.fontSize = 9f;
            footer.style.marginTop = 10f;
            footer.style.paddingLeft = 4f;
            sidebar.Add(footer);
        }

        private void AddNavGroup(VisualElement parent, string heading, string[] pages)
        {
            Label group = new Label(heading);
            group.style.color = IrisTraceTheme.SecondaryText;
            group.style.fontSize = 9f;
            group.style.letterSpacing = 1f;
            group.style.marginTop = 8f;
            group.style.marginBottom = 5f;
            group.style.paddingLeft = 6f;
            parent.Add(group);

            foreach (string page in pages)
            {
                string pageId = page;
                Button button = new Button(() => NavigateTo(pageId)) { text = pageId };
                button.style.height = 32f;
                button.style.marginBottom = 3f;
                button.style.paddingLeft = 9f;
                button.style.paddingRight = 7f;
                button.style.unityTextAlign = TextAnchor.MiddleLeft;
                button.style.fontSize = 11f;
                button.style.borderTopWidth = 0f;
                button.style.borderBottomWidth = 0f;
                button.style.borderLeftWidth = 0f;
                button.style.borderRightWidth = 0f;
                button.style.borderTopLeftRadius = 7f;
                button.style.borderTopRightRadius = 7f;
                button.style.borderBottomLeftRadius = 7f;
                button.style.borderBottomRightRadius = 7f;
                parent.Add(button);
                navigationButtons[pageId] = button;
            }
        }

        private void NavigateTo(string pageId)
        {
            if (contentHost == null)
            {
                currentPage = pageId;
                return;
            }

            currentPage = pageId;
            contentHost.Clear();
            VisualElement view;
            if (!pageViews.TryGetValue(pageId, out view) || view == null)
            {
                view = BuildPage(pageId);
                pageViews[pageId] = view;
            }
            if (view == null)
                view = BuildMessagePage("Page unavailable", "This tool could not be initialized.");

            view.style.flexGrow = 1f;
            view.style.minWidth = 0f;
            contentHost.Add(view);
            UpdateNavigationStyles();
        }

        private VisualElement BuildPage(string pageId)
        {
            switch (pageId)
            {
                case "Overview": return BuildOverview();
                case "Create FPS Hero": return CreateToolView<IrisTraceHeroWizardWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Create Enemy": return CreateToolView<IrisTraceEnemyWizardWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Characters": return CreateToolView<IrisTraceCharacterLibraryWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Weapons": return CreateToolView<IrisTraceWeaponLibraryWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Enemies": return CreateToolView<IrisTraceEnemyLibraryWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Project Validation": return CreateToolView<IrisTraceProjectValidationWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Release Audit": return CreateToolView<IrisTraceReleaseAuditWindow>(pageId, tool => tool.CreateEmbeddedView());
                case "Test Arena": return BuildTestArenaPage();
                default: return BuildMessagePage("Unknown section", "Choose a section from the navigation panel.");
            }
        }

        private VisualElement CreateToolView<T>(string pageId, Func<T, VisualElement> builder) where T : EditorWindow
        {
            T tool = CreateInstance<T>();
            embeddedTools[pageId] = tool;
            try
            {
                VisualElement view = builder(tool);
                if (view == null)
                    return BuildMessagePage("Unable to load " + pageId, "The tool returned an empty view.");
                return view;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                DestroyImmediate(tool);
                embeddedTools.Remove(pageId);
                return BuildMessagePage("Unable to load " + pageId, "See the Unity Console for the exception details.");
            }
        }

        private VisualElement BuildOverview()
        {
            ScrollView scroll = MakePageScroll();
            VisualElement content = new VisualElement();
            content.style.paddingLeft = 22f;
            content.style.paddingRight = 22f;
            content.style.paddingTop = 22f;
            content.style.paddingBottom = 20f;
            content.style.maxWidth = 760f;
            scroll.Add(content);

            Label eyebrow = new Label("CHARACTER WORKSPACE");
            eyebrow.style.color = IrisTraceTheme.Accent;
            eyebrow.style.fontSize = 10f;
            eyebrow.style.letterSpacing = 1.1f;
            eyebrow.style.marginBottom = 8f;
            Label title = new Label("Build your next FPS character.");
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 24f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.whiteSpace = WhiteSpace.Normal;
            title.style.marginBottom = 8f;
            Label description = new Label("Create configurable gameplay assets in one workspace — without manually wiring components.");
            description.style.color = IrisTraceTheme.SecondaryText;
            description.style.fontSize = 12f;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 22f;
            content.Add(eyebrow);
            content.Add(title);
            content.Add(description);

            AddOverviewCard(content, "01", "FPS Hero", "Configure movement, camera, controls, weapon and abilities.", "Create FPS Hero", () => NavigateTo("Create FPS Hero"), true);
            AddOverviewCard(content, "02", "Enemy", "Create a target or a simple AI bot.", "Create Enemy", () => NavigateTo("Create Enemy"), false);
            AddOverviewCard(content, "03", "Libraries", "Manage characters, weapons and enemy definitions from the left navigation.", "Open Characters", () => NavigateTo("Characters"), false);
            AddOverviewCard(content, "04", "Test Arena", "Generate a test scene for the complete combat loop.", "Create Test Arena", () => NavigateTo("Test Arena"), true);
            AddOverviewCard(content, "05", "Validation", "Check project structure and configuration consistency.", "Open Validation", () => NavigateTo("Project Validation"), false);
            Label status = new Label("MVP PREVIEW  /  VERIFY IN UNITY BEFORE RELEASE");
            status.style.color = IrisTraceTheme.SecondaryText;
            status.style.fontSize = 9f;
            status.style.letterSpacing = 0.5f;
            status.style.marginTop = 8f;
            status.style.whiteSpace = WhiteSpace.Normal;
            content.Add(status);
            return scroll;
        }

        private void AddOverviewCard(VisualElement parent, string number, string titleText, string descriptionText, string buttonText, Action action, bool primary)
        {
            VisualElement card = new VisualElement();
            card.style.backgroundColor = IrisTraceTheme.Surface;
            card.style.paddingLeft = 15f;
            card.style.paddingRight = 15f;
            card.style.paddingTop = 13f;
            card.style.paddingBottom = 13f;
            card.style.marginBottom = 10f;
            card.style.borderTopWidth = 1f;
            card.style.borderBottomWidth = 1f;
            card.style.borderLeftWidth = 1f;
            card.style.borderRightWidth = 1f;
            card.style.borderTopColor = IrisTraceTheme.Border;
            card.style.borderBottomColor = IrisTraceTheme.Border;
            card.style.borderLeftColor = IrisTraceTheme.Border;
            card.style.borderRightColor = IrisTraceTheme.Border;
            card.style.borderTopLeftRadius = 10f;
            card.style.borderTopRightRadius = 10f;
            card.style.borderBottomLeftRadius = 10f;
            card.style.borderBottomRightRadius = 10f;

            VisualElement titleRow = new VisualElement();
            titleRow.style.flexDirection = FlexDirection.Row;
            titleRow.style.alignItems = Align.Center;
            titleRow.style.marginBottom = 5f;
            Label num = new Label(number);
            num.style.color = IrisTraceTheme.Accent;
            num.style.fontSize = 10f;
            num.style.marginRight = 9f;
            num.style.unityFontStyleAndWeight = FontStyle.Bold;
            Label title = new Label(titleText);
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 14f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleRow.Add(num);
            titleRow.Add(title);
            Label description = new Label(descriptionText);
            description.style.color = IrisTraceTheme.SecondaryText;
            description.style.fontSize = 11f;
            description.style.whiteSpace = WhiteSpace.Normal;
            description.style.marginBottom = 10f;
            Button button = new Button(action) { text = buttonText + "   →" };
            button.style.height = 31f;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderTopLeftRadius = 7f;
            button.style.borderTopRightRadius = 7f;
            button.style.borderBottomLeftRadius = 7f;
            button.style.borderBottomRightRadius = 7f;
            button.style.backgroundColor = primary ? IrisTraceTheme.Accent : IrisTraceTheme.SurfaceStrong;
            button.style.color = primary ? IrisTraceTheme.AccentText : IrisTraceTheme.PrimaryText;
            card.Add(titleRow);
            card.Add(description);
            card.Add(button);
            parent.Add(card);
        }

        private VisualElement BuildTestArenaPage()
        {
            ScrollView scroll = MakePageScroll();
            VisualElement content = new VisualElement();
            content.style.paddingLeft = 22f;
            content.style.paddingRight = 22f;
            content.style.paddingTop = 22f;
            content.style.maxWidth = 700f;
            scroll.Add(content);
            AddPageTitle(content, "Test Arena", "Generate a ready-to-play scene with a hero, target and basic enemy.");
            Button create = new Button(IrisTraceTestArenaBuilder.CreateTestArena) { text = "Create Test Arena" };
            StylePrimaryButton(create);
            content.Add(create);
            Label note = new Label("The scene is saved under Assets/IrisTraceFPS_Generated/Scenes. Review the Console if required project dependencies are missing.");
            note.style.color = IrisTraceTheme.SecondaryText;
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.marginTop = 12f;
            content.Add(note);
            return scroll;
        }

        private VisualElement BuildMessagePage(string title, string message)
        {
            VisualElement root = new VisualElement();
            root.style.flexGrow = 1f;
            root.style.backgroundColor = IrisTraceTheme.Background;
            root.style.paddingLeft = 22f;
            root.style.paddingRight = 22f;
            root.style.paddingTop = 22f;
            AddPageTitle(root, title, message);
            return root;
        }

        private static ScrollView MakePageScroll()
        {
            ScrollView scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.style.flexGrow = 1f;
            scroll.style.backgroundColor = IrisTraceTheme.Background;
            scroll.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            return scroll;
        }

        private static void AddPageTitle(VisualElement parent, string titleText, string subtitleText)
        {
            Label title = new Label(titleText);
            title.style.color = IrisTraceTheme.PrimaryText;
            title.style.fontSize = 20f;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginBottom = 5f;
            Label subtitle = new Label(subtitleText);
            subtitle.style.color = IrisTraceTheme.SecondaryText;
            subtitle.style.fontSize = 11f;
            subtitle.style.whiteSpace = WhiteSpace.Normal;
            subtitle.style.marginBottom = 18f;
            parent.Add(title);
            parent.Add(subtitle);
        }

        private static void StylePrimaryButton(Button button)
        {
            button.style.height = 36f;
            button.style.backgroundColor = IrisTraceTheme.Accent;
            button.style.color = IrisTraceTheme.AccentText;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopWidth = 0f;
            button.style.borderBottomWidth = 0f;
            button.style.borderLeftWidth = 0f;
            button.style.borderRightWidth = 0f;
            button.style.borderTopLeftRadius = 8f;
            button.style.borderTopRightRadius = 8f;
            button.style.borderBottomLeftRadius = 8f;
            button.style.borderBottomRightRadius = 8f;
        }

        private void UpdateNavigationStyles()
        {
            foreach (KeyValuePair<string, Button> entry in navigationButtons)
            {
                bool active = string.Equals(entry.Key, currentPage, StringComparison.Ordinal);
                Button button = entry.Value;
                button.style.backgroundColor = active ? IrisTraceTheme.AccentHover : Color.clear;
                button.style.color = active ? IrisTraceTheme.PrimaryText : IrisTraceTheme.SecondaryText;
                button.style.unityFontStyleAndWeight = active ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private void DisposeEmbeddedTools()
        {
            foreach (KeyValuePair<string, EditorWindow> entry in embeddedTools)
            {
                if (entry.Value != null)
                    DestroyImmediate(entry.Value);
            }
            embeddedTools.Clear();
        }

        private void OnDisable()
        {
            DisposeEmbeddedTools();
            pageViews.Clear();
        }
    }
}
