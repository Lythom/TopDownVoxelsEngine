// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/08/26

using System.IO;
using DG.DemiEditor;
using DG.DOTweenEditor.UI;
using UnityEditor;
using UnityEngine;

namespace DG.Tweening.TimelineEditor.ExtraEditors
{
    class CustomPluginsEditor : EditorWindow
    {
        // [MenuItem("Tools/Demigiant/" + _Title)] // Disabled while working on it
        public static void ShowWindow() { GetWindow(typeof(CustomPluginsEditor), true, _Title); }
		
        const string _Title = "DOTween Timeline : Custom Plugins Editor";

        DOTimelineEditorSettings.CustomPluginsEditorData _data { get { return _src.customPluginsData; } }
        ParsedPluginsFile _parsed { get { return _data.parsedPluginsFile; } set { _data.parsedPluginsFile = value; } }

        DOTimelineEditorSettings _src;
        DeScrollView _editFileScrollView;
        readonly GUIContent _gcDrag = new GUIContent("≡");
        readonly GUIContent _gcDelete = new GUIContent("×");
        readonly GUIContent _gcIntro = new GUIContent("Here you can create custom plugins in order to add your own " +
                                             "custom global/instance actions/tweens to DOTweenTimeline");
        readonly GUIContent _gcBtSelectExistingFile = new GUIContent("<b>Select Existing</b> Custom Plugins File...");
        readonly GUIContent _gcBtSelectExistingFile_short = new GUIContent("Select Other...");
        readonly GUIContent _gcBtCreateNewFile = new GUIContent("<b>Create New</b> Custom Plugins File...");
        readonly GUIContent _gcBtCreateNewFile_short = new GUIContent("Create New...");
        readonly GUIContent _gcFilePrefix = new GUIContent("Custom Plugins File");
        readonly GUIContent _gcBtForget = new GUIContent("Forget");
        readonly GUIContent _gcBtEdit = new GUIContent("Edit");
        readonly GUIContent _gcBtReload = new GUIContent("Reload", "Reload from file");
        readonly GUIContent _gcUsing = new GUIContent("using");

        readonly string[] _fileFilters = new[] { "CSharp", "cs" };
        bool _hasCustomPluginsFile;
        bool _isEditFileMode;

        #region Unity and GUI Methods

        void OnEnable()
        {
            ConnectToSettings();
            Refresh();
            Undo.undoRedoPerformed += Repaint;
        }

        void OnDisable()
        { Undo.undoRedoPerformed -= Repaint; }

        void OnGUI()
        {
            ConnectToSettings();
            Undo.RecordObject(_src, _Title);
            DOEGUI.BeginGUI();

            bool customPluginsFileExists = File.Exists(_data.pluginsFilePath);
            if (customPluginsFileExists) Draw_FileExists();
            else Draw_FileMissing();

            if (GUI.changed) MarkDirty();
        }

        void Draw_FileMissing()
        {
            EditorGUILayout.HelpBox(_gcIntro.text, MessageType.Info);
            GUILayout.Space(6);
            using (new GUILayout.HorizontalScope()) {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button(_gcBtSelectExistingFile, DOEGUI.Styles.pluginsEditor.btMain)) {
                    SelectPluginsFile();
                }
                if (GUILayout.Button(_gcBtCreateNewFile, DOEGUI.Styles.pluginsEditor.btMain)) {

                }
                GUILayout.FlexibleSpace();
            }
        }

        void Draw_FileExists()
        {
            Rect area = position.ResetXY();
            Rect line = area.SetHeight(EditorGUIUtility.singleLineHeight).ShiftY(2);

            // File
            Rect prefixR = line.SetWidth(GUI.skin.label.CalcSize(_gcFilePrefix).x);
            Rect fileR = line.ShiftXAndResize(prefixR.width + 2);
            GUI.Label(prefixR, _gcFilePrefix);
            GUI.Label(fileR, _data.pluginsFileLabel, DOEGUI.Styles.pluginsEditor.fileLabel);
            // File operations
            NextLine(ref line);
            GUIContent gcEditOrReload = _isEditFileMode ? _gcBtReload : _gcBtEdit;
            Rect btEditR = line.SetX(fileR.x).SetWidth(DOEGUI.Styles.pluginsEditor.bt.CalcSize(gcEditOrReload).x);
            Rect btCreateNewFileR = line.HangToLeftOf(line.xMax, 0, DOEGUI.Styles.pluginsEditor.bt.CalcSize(_gcBtCreateNewFile_short).x);
            Rect btSelectOtherFileR = line.HangToLeftOf(btCreateNewFileR.x, 2, DOEGUI.Styles.pluginsEditor.bt.CalcSize(_gcBtSelectExistingFile_short).x);
            Rect btForgetR = line.HangToLeftOf(btSelectOtherFileR.x, 2, DOEGUI.Styles.pluginsEditor.bt.CalcSize(_gcBtForget).x);
            if (GUI.Button(btEditR, gcEditOrReload, DOEGUI.Styles.pluginsEditor.bt)) {
                _parsed = new ParsedPluginsFile(_data);
                if (_parsed != null) {
                    _isEditFileMode = true;
                    MarkDirty();
                    Repaint();
                }
            }
            if (GUI.Button(btForgetR, _gcBtForget, DOEGUI.Styles.pluginsEditor.bt)) {
                _data.pluginsFilePath = "";
                _isEditFileMode = false;
                Repaint();
                GUI.changed = true;
            }
            if (GUI.Button(btSelectOtherFileR, _gcBtSelectExistingFile_short, DOEGUI.Styles.pluginsEditor.bt)) {
                SelectPluginsFile();
            }
            if (GUI.Button(btCreateNewFileR, _gcBtCreateNewFile_short, DOEGUI.Styles.pluginsEditor.bt)) {
            }
            // Edit mode
            if (_isEditFileMode) Draw_EditFileMode(area.ShiftYAndResize(line.yMax + 4));
        }

        void Draw_EditFileMode(Rect area)
        {
            ParsedPluginsFile f = _data.parsedPluginsFile;
            bool isOverflowing = _editFileScrollView.fullContentArea.height > area.height;
            _editFileScrollView = DeGUI.BeginScrollView(area, _editFileScrollView);
            area = area.ResetXY().Shift(4, 0, -8, 0);
            if (isOverflowing) area = area.Shift(0, 0, -14, 0);
            Rect line = area.SetHeight(EditorGUIUtility.singleLineHeight);

            // Usings
            Rect toolbaR = line;
            _editFileScrollView.IncreaseContentHeightBy(toolbaR.height);
            DeGUI.Box(toolbaR, Color.white, DOEGUI.Styles.toolbar.def);
            f.parts.foldoutUsings = DeGUI.ToolbarFoldoutButton(line, f.parts.foldoutUsings, "USINGS", false, true);
            if (f.parts.foldoutUsings) {
                float h = f.parts.usingsIds.Count * (EditorGUIUtility.singleLineHeight + 2);
                DeGUI.Box(line.ShiftY(line.height + 2).SetHeight(h), Color.white, DOEGUI.Styles.box.sticky);
                float usingW = GUI.skin.label.CalcSize(_gcUsing).x;
                for (int i = 0; i < f.parts.usingsIds.Count; ++i) {
                    NextLineInScrollView(ref line, ref _editFileScrollView, 2);
                    Rect innerLine = line.Shift(2, 0, -4, 0);
                    Rect btDragR = innerLine.SetWidth(20);
                    Rect labelR = innerLine.HangToRightAndResize(btDragR.xMax, 2).SetWidth(usingW);
                    Rect tfR = innerLine.HangToRightAndResize(labelR.xMax, 2, -20);
                    Rect btDeleteR = innerLine.HangToRightAndResize(tfR.xMax, 2).SetWidth(18);
                    if (DeGUI.PressButton(btDragR, _gcDrag, DOEGUI.Styles.button.tool)) {
                        DeGUIDrag.StartDrag(this, f.parts.usingsIds, i);
                    }
                    GUI.Label(labelR, _gcUsing);
                    f.parts.usingsIds[i] = EditorGUI.DelayedTextField(tfR, f.parts.usingsIds[i]);
                    if (GUI.Button(btDeleteR, _gcDelete, DOEGUI.Styles.button.tool)) {
                        DeGUI.Deselect();
                        f.parts.usingsIds.RemoveAt(i);
                        --i;
                        GUI.changed = true;
                    }
                    if (DeGUIDrag.Drag(f.parts.usingsIds, i, line).outcome == DeDragResultType.Accepted) GUI.changed = true;
                }
            }

            DeGUI.EndScrollView();
        }

        void NextLine(ref Rect line, int margin = 2)
        {
            line = line.SetY(line.yMax + margin).SetHeight(EditorGUIUtility.singleLineHeight);
        }

        void NextLineInScrollView(ref Rect line, ref DeScrollView scrollView, int margin = 2)
        {
            line = line.SetY(line.yMax + margin).SetHeight(EditorGUIUtility.singleLineHeight);
            scrollView.IncreaseContentHeightBy(line.height + margin);
        }

        #endregion

        #region Methods

        void ConnectToSettings()
        {
            if (_src == null) _src = DOTimelineEditorSettings.Load();
        }

        void Refresh()
        {
            _data.Refresh();
            EditorUtility.SetDirty(_src);
        }

        void MarkDirty()
        {
            if (_src != null) EditorUtility.SetDirty(_src);
        }

        void SelectPluginsFile()
        {
            string file = EditorUtility.OpenFilePanelWithFilters("DOTween Timeline Custom Plugins File", DeEditorFileUtils.assetsPath, _fileFilters);
            if (file == "") return;
            file = file.Replace(DeEditorFileUtils.PathSlashToReplace, DeEditorFileUtils.PathSlash);
            if (!file.StartsWith(DeEditorFileUtils.assetsPath)) {
                EditorUtility.DisplayDialog("DOTween Timeline Custom Plugins Editor", "You can't open a file outside of your Unity project", "Ok");
                return;
            }
            _data.pluginsFilePath = file;
            _isEditFileMode = false;
            MarkDirty();
            Repaint();
        }

        #endregion
    }
}