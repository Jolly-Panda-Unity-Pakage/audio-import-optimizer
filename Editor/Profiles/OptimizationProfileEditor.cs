// Copyright (c) Usef Farahmand. All rights reserved.
// https://useffarahmand.com

using System.Collections.Generic;
using JP.AudioImportOptimizer.Core.Models;
using JP.AudioImportOptimizer.Core.Profiles;
using UnityEditor;
using UnityEngine;

namespace JP.AudioImportOptimizer.Editor.Profiles
{
    /// <summary>Inspector for <see cref="OptimizationProfile"/>: shows live validation and explains what the profile does and does not do.</summary>
    [CustomEditor(typeof(OptimizationProfile))]
    public sealed class OptimizationProfileEditor : UnityEditor.Editor
    {
        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "A profile only describes the desired import settings. Nothing is changed until you review a dry run in " +
                "Tools > JP > Audio Import Optimizer and press Apply. Settings whose 'Apply' toggle is off are never touched.",
                MessageType.Info);

            DrawDefaultInspector();

            var profile = (OptimizationProfile)target;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
            List<ValidationIssue> issues = ProfileValidator.Validate(profile.Rules);
            if (issues.Count == 0) EditorGUILayout.HelpBox("No problems found.", MessageType.None);
            foreach (var issue in issues)
            {
                string prefix = string.IsNullOrEmpty(issue.RuleName) ? string.Empty : "[" + issue.RuleName + "] ";
                MessageType type = issue.Severity == Severity.Error ? MessageType.Error : issue.Severity == Severity.Warning ? MessageType.Warning : MessageType.Info;
                EditorGUILayout.HelpBox(prefix + issue.Message, type);
            }

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(new GUIContent("Append Starter Rules", "Adds five example rules (long-form, one-shot, voice over, UI, music) after your existing rules. Existing rules are not modified.")))
                {
                    if (EditorUtility.DisplayDialog("Append starter rules?",
                        "Five example rules will be appended to this profile. Existing rules stay unchanged. You can undo this with Ctrl/Cmd+Z.", "Append", "Cancel"))
                    {
                        Undo.RecordObject(profile, "Append starter audio rules");
                        profile.AppendRules(DefaultRules.Create());
                        EditorUtility.SetDirty(profile);
                    }
                }
            }
        }
    }
}
