// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/08/28

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#pragma warning disable 0649 // FIXME: temporary While working on it
#pragma warning disable 0168 // FIXME: temporary While working on it
namespace DG.Tweening.TimelineEditor.ExtraEditors
{
    // Serialized inside DOTimelineEditorSettings
    [Serializable]
    internal class ParsedPluginsFile
    {
        enum ParsingSection
        {
            Unset, Usings, Namespace, Plugins
        }
        enum ParsingSubsection
        {
            Unset, Tweens, GlobalTweens, Actions
        }

        public Parts parts = new Parts();

        [NonSerialized] DOTimelineEditorSettings.CustomPluginsEditorData _data;

        public ParsedPluginsFile(DOTimelineEditorSettings.CustomPluginsEditorData data)
        {
            _data = data;
            if (!File.Exists(data.pluginsFilePath)) {
                EditorUtility.DisplayDialog("Parsed Plugins Constructor", string.Format("File path doesn't exist (\"{0}\")", data.pluginsFilePath), "Ok");
                return;
            }

            parts.Reset();
            LoadFromFile();
        }

        #region Methods

        // Assumes the filePath exists
        void LoadFromFile()
        {
            using (StreamReader reader = new StreamReader(_data.pluginsFilePath)) {
                ParsingSection section = ParsingSection.Unset;
                ParsingSubsection subSection = ParsingSubsection.Unset;
                string line;
                while ((line = reader.ReadLine()) != null) {
                    line = line.Trim();
                    if (line.StartsWith("using")) section = ParsingSection.Usings;
                    else if (line.StartsWith("namespace")) section = ParsingSection.Namespace;
                    else if (line.StartsWith("static DOVisualTweenPlugin GetTweenPlugin")) {
                        section = ParsingSection.Plugins;
                        subSection = ParsingSubsection.Tweens;
                    } else if (line.StartsWith("static DOVisualTweenPlugin GetGlobalTweenPlugin")) {
                        section = ParsingSection.Plugins;
                        subSection = ParsingSubsection.GlobalTweens;
                    } else if (line.StartsWith("static DOVisualActionPlugin GetActionPlugin")) {
                        section = ParsingSection.Plugins;
                        subSection = ParsingSubsection.Actions;
                    } else {
                        switch (section) {
                        case ParsingSection.Usings:
                        case ParsingSection.Namespace:
                            section = ParsingSection.Unset;
                            break;
                        }
                    }
                    int startIndex, endIndex;
                    bool isWithinPluginSwitchCase;
                    switch (section) {
                    case ParsingSection.Usings:
                        startIndex = line.IndexOf(' ');
                        endIndex = line.IndexOf(';');
                        if (startIndex == -1 || endIndex == -1) break;
                        parts.usingsIds.Add(line.Substring(startIndex + 1, endIndex - startIndex - 1));
                        break;
                    case ParsingSection.Namespace:
                        startIndex = line.IndexOf(' ');
                        parts.namespaceId = line.Substring(startIndex);
                        break;
                    case ParsingSection.Plugins:
                        switch (subSection) {
                        case ParsingSubsection.Tweens:

                            break;
                        }
                        break;
                    }
                }
            }
        }

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        [Serializable]
        public class Parts
        {
            public bool foldoutUsings = true;
            public bool foldoutTweens = true;
            public bool foldoutGlobalTweens = true;
            public bool foldoutActions = true;
            public List<string> usingsIds = new List<string>();
            public string namespaceId; // Full namespace
            public string className;
            public TweenPlugin[] tweenPlugins;
            public GlobalTweenPlugin[] globalTweenPlugins;
            public ActionPlugin[] actionPlugins; // Global/local is defined per-plugData

            public void Reset()
            {
                foldoutUsings = foldoutTweens = foldoutGlobalTweens = foldoutActions = true;
            }
        }

        [Serializable]
        public class TweenPlugin
        {
            public string targetTypeQualifiedName;
            public string targetClassName;
            public List<TweenPlugData> plugDatas;

            [NonSerialized] public Type targetType;
        }

        [Serializable]
        public class GlobalTweenPlugin
        {
            public string id;
            public List<TweenPlugData> plugDatas;
        }

        [Serializable]
        public class ActionPlugin
        {
            public string id;
            public List<ActionPlugData> plugDatas;
        }

        [Serializable]
        public class TweenPlugData
        {
            public string label;
            public string mGet;
            public string mSet;

            [NonSerialized] public Type varType;
        }

        [Serializable]
        public class ActionPlugData
        {
            public string label;
            public string targetTypeQualifiedName; // NULL if global action (which is determined by ID instead of targetType)
            public string mAction;

            public bool isGlobal { get { return string.IsNullOrEmpty(targetTypeQualifiedName); } }
            [NonSerialized] public Type targetType; // NULL if global action

            void Refresh()
            {
                if (!isGlobal && !string.IsNullOrEmpty(targetTypeQualifiedName) && targetType == null) {
                    targetType = Type.GetType(targetTypeQualifiedName);
                }
            }
        }
    }
}