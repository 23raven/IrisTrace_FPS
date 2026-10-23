using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace IrisTrace.Editor.Windows
{
    /// <summary>
    /// A lightweight, standalone checklist for reviewing an IrisTrace FPS project before sharing it.
    /// This window reports basic file/package checks; it does not replace Play Mode testing.
    /// </summary>
    public sealed class IrisTraceReleaseAuditWindow : EditorWindow
    {
        private const string ProjectRoot = "Assets/IrisTraceFPS";
        private const string GeneratedRoot = "Assets/IrisTraceFPS_Generated";
        private const string ManifestPath = "Packages/manifest.json";

        private readonly List<AuditResult> results = new List<AuditResult>();
        private Vector2 scroll;
        private bool hasRun;

        private enum ResultKind
        {
            Pass,
            Warning,
            Fail
        }

        private sealed class AuditResult
        {
            public ResultKind Kind;
            public string Title;
            public string Details;

            public AuditResult(ResultKind kind, string title, string details)
            {
                Kind = kind;
                Title = title;
                Details = details;
            }
        }

        [MenuItem("Window/IrisTrace FPS/Release Audit")]
        public static void Open()
        {
            IrisTraceReleaseAuditWindow window = GetWindow<IrisTraceReleaseAuditWindow>();
            window.titleContent = new GUIContent("IrisTrace Audit");
            window.minSize = new Vector2(440f, 440f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("IrisTrace FPS", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("MVP Release Audit", EditorStyles.largeLabel);
            EditorGUILayout.HelpBox(
                "This audit checks basic package structure and setup. Passing it does not guarantee gameplay correctness; test the checklist in Play Mode before release.",
                MessageType.Info);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Run Audit", GUILayout.Height(30f)))
            {
                RunAudit();
            }

            if (GUILayout.Button("Refresh Assets", GUILayout.Height(30f)))
            {
                AssetDatabase.Refresh();
                RunAudit();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8f);
            if (!hasRun)
            {
                EditorGUILayout.HelpBox("Run the audit to check the current project.", MessageType.None);
            }
            else
            {
                DrawSummary();
                scroll = EditorGUILayout.BeginScrollView(scroll);
                for (int i = 0; i < results.Count; i++)
                {
                    AuditResult result = results[i];
                    MessageType messageType = result.Kind == ResultKind.Fail
                        ? MessageType.Error
                        : result.Kind == ResultKind.Warning ? MessageType.Warning : MessageType.Info;

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    EditorGUILayout.LabelField(GetPrefix(result.Kind) + result.Title, EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(result.Details, EditorStyles.wordWrappedLabel);
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Space(3f);
                }
                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Manual checks are listed in:", EditorStyles.miniBoldLabel);
            EditorGUILayout.SelectableLabel(
                "Assets/IrisTraceFPS/Documentation/MVP_RELEASE_CHECKLIST.txt",
                EditorStyles.textField,
                GUILayout.Height(EditorGUIUtility.singleLineHeight));

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Generator"))
                TryOpenMenu("Window/IrisTrace FPS/Open Generator");
            if (GUILayout.Button("Project Validation"))
                TryOpenMenu("Window/IrisTrace FPS/Project Validation");
            EditorGUILayout.EndHorizontal();
        }

        private void RunAudit()
        {
            results.Clear();
            hasRun = true;

            CheckDirectory(ProjectRoot, "IrisTrace source folder");
            CheckDirectory(ProjectRoot + "/Runtime", "Runtime modules");
            CheckDirectory(ProjectRoot + "/Editor", "Editor tools");
            CheckDirectory(ProjectRoot + "/Documentation", "Documentation folder");

            CheckFile(ProjectRoot + "/Editor/IrisTrace.Editor.asmdef", "Editor assembly definition");
            CheckFile(ProjectRoot + "/Runtime/Core/IrisTrace.Runtime.Core.asmdef", "Core assembly definition");
            CheckFile(ProjectRoot + "/Runtime/Input/IrisTrace.Runtime.Input.asmdef", "Input assembly definition");
            CheckFile(ProjectRoot + "/Runtime/Characters/IrisTrace.Runtime.Characters.asmdef", "Character assembly definition");
            CheckFile(ProjectRoot + "/Runtime/Weapons/IrisTrace.Runtime.Weapons.asmdef", "Weapon assembly definition");
            CheckFile(ProjectRoot + "/Runtime/Abilities/IrisTrace.Runtime.Abilities.asmdef", "Abilities assembly definition");
            CheckFile(ProjectRoot + "/Runtime/Enemies/IrisTrace.Runtime.Enemies.asmdef", "Enemy assembly definition");

            CheckFile(ProjectRoot + "/Runtime/Characters/FPSHeroController.cs", "FPS hero controller source");
            CheckFile(ProjectRoot + "/Runtime/Weapons/HitscanWeapon.cs", "Hitscan weapon source");
            CheckFile(ProjectRoot + "/Runtime/Input/IrisTraceInputReader.cs", "Input adapter source");
            CheckFile(ProjectRoot + "/Runtime/Abilities/AbilityInputHandler.cs", "Ability input handler source");
            CheckFile(ProjectRoot + "/Runtime/Enemies/BasicEnemyAI.cs", "Basic enemy AI source");
            CheckFile(ProjectRoot + "/Editor/Generators/FPSHeroPrefabGenerator.cs", "Hero prefab generator source");

            CheckInputSystemPackage();
            CheckDirectory(GeneratedRoot, "Generated output folder", true);
            CheckDirectory(GeneratedRoot + "/Prefabs/Characters", "Generated character prefab folder", true);
            CheckDirectory(GeneratedRoot + "/Prefabs/Enemies", "Generated enemy prefab folder", true);
            CheckDirectory(GeneratedRoot + "/Configurations/Characters", "Character configurations folder", true);
            CheckDirectory(GeneratedRoot + "/Configurations/Weapons", "Weapon configurations folder", true);
            CheckDirectory(GeneratedRoot + "/Configurations/Enemies", "Enemy configurations folder", true);
            CheckDirectory(GeneratedRoot + "/Scenes", "Generated scenes folder", true);

            CheckFile(ProjectRoot + "/Documentation/MVP_RELEASE_CHECKLIST.txt", "Manual MVP release checklist");
            CheckFile(ProjectRoot + "/Documentation/START_HERE.txt", "Quick-start guide", true);

            Repaint();
        }

        private void CheckDirectory(string path, string title, bool warningIfMissing = false)
        {
            bool exists = AssetDatabase.IsValidFolder(path);
            AddResult(exists ? ResultKind.Pass : warningIfMissing ? ResultKind.Warning : ResultKind.Fail,
                title,
                exists ? path : "Not found: " + path);
        }

        private void CheckFile(string path, string title, bool warningIfMissing = false)
        {
            bool exists = File.Exists(path);
            AddResult(exists ? ResultKind.Pass : warningIfMissing ? ResultKind.Warning : ResultKind.Fail,
                title,
                exists ? path : "Not found: " + path);
        }

        private void CheckInputSystemPackage()
        {
            bool manifestExists = File.Exists(ManifestPath);
            if (!manifestExists)
            {
                AddResult(ResultKind.Fail, "Package manifest", "Packages/manifest.json was not found.");
                return;
            }

            string manifest = File.ReadAllText(ManifestPath);
            bool declared = manifest.IndexOf("com.unity.inputsystem", StringComparison.OrdinalIgnoreCase) >= 0;
            AddResult(declared ? ResultKind.Pass : ResultKind.Fail,
                "New Input System package declaration",
                declared ? "com.unity.inputsystem is declared in Packages/manifest.json."
                         : "Add com.unity.inputsystem in Package Manager, then enable Input System Package (New) in Player Settings.");
        }

        private void DrawSummary()
        {
            int pass = 0;
            int warning = 0;
            int fail = 0;
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i].Kind == ResultKind.Pass) pass++;
                else if (results[i].Kind == ResultKind.Warning) warning++;
                else fail++;
            }

            EditorGUILayout.LabelField("Audit Summary", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("PASS: " + pass + "    WARNING: " + warning + "    FAIL: " + fail);
            if (fail > 0)
                EditorGUILayout.HelpBox("Resolve FAIL items before sharing the project.", MessageType.Error);
            else if (warning > 0)
                EditorGUILayout.HelpBox("No required structure checks failed. Review warnings and complete the manual checklist.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("Basic structure checks passed. Complete manual gameplay tests before release.", MessageType.Info);
        }

        private void AddResult(ResultKind kind, string title, string details)
        {
            results.Add(new AuditResult(kind, title, details));
        }

        private static string GetPrefix(ResultKind kind)
        {
            switch (kind)
            {
                case ResultKind.Pass: return "PASS  ";
                case ResultKind.Warning: return "WARN  ";
                default: return "FAIL  ";
            }
        }

        private static void TryOpenMenu(string menuPath)
        {
            if (!EditorApplication.ExecuteMenuItem(menuPath))
                Debug.LogWarning("IrisTrace FPS: menu item was not found: " + menuPath);
        }
    }
}
