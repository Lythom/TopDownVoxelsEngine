// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/18

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using DG.DemiEditor;
using DG.Tweening.Timeline;
using DG.Tweening.Timeline.Core;
using DG.Tweening.Timeline.Core.Plugins;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace DG.Tweening.TimelineEditor
{
    [InitializeOnLoad]
    internal static class TimelineEditorUtils
    {
        public static TransformSnapshot transformSnapshot { get; private set; }

        static DOTimelineEditorSettings _settings { get { return DOTweenClipTimeline.settings; } }
        static TimelineLayout _layout { get { return DOTweenClipTimeline.Layout; } }
        static DOTweenClip _clip { get { return DOTweenClipTimeline.clip; } }
        static readonly StringBuilder _Strb = new StringBuilder();
        static readonly Color _EaseColor = new Color(1f, 0f, 0.74f);
        static readonly Color _EaseLimiterColor = new Color(0.39f, 0.12f, 0.51f);
        static Color32[] _easeTextureFloodFill;
        static Vector2Int _easeTextureLastSize;
        static PropertyInfo _piPrefabAutoSave;
        static readonly Type _TClip = typeof(DOTweenClip);
        static readonly Type _TClipArray = typeof(DOTweenClip[]);
        static readonly Type _TClipList = typeof(List<DOTweenClip>);
        static readonly Type _TSerializeFieldAttribute = typeof(SerializeField);
        static readonly Type _TNonSerializedAttribute = typeof(NonSerializedAttribute);
        static readonly Type _TUnityObject = typeof(UnityEngine.Object);
        static readonly List<FieldInfo> _TmpFInfos = new List<FieldInfo>();
        static readonly List<GameObject> _TmpGos = new List<GameObject>();
        static readonly List<DOTweenClip> _TmpClips = new List<DOTweenClip>();
        static readonly List<SelectionClip> _TmpSelectionClips = new List<SelectionClip>();

        static TimelineEditorUtils()
        {
            EditorConnector.Request.CloneUnityEvent += CloneUnityEvent;
        }

        #region Public Methods

        /// <summary>
        /// Uses undo system and displays dialog at the end
        /// </summary>
        public static void CleanupClip(DOTweenClip clip)
        {
            using (new DOScope.UndoableSerialization()) {
                // Check that all clipElements are in layers
                // (if not it's because of bug solved in v0.9.155, where deleting a layer didn't delete the clipElements)
                int totUnlayeredClipElements = 0;
                for (int i = clip.elements.Length - 1; i > -1; i--) {
                    string guid = clip.elements[i].guid;
                    bool found = false;
                    for (int j = 0; j < clip.layers.Length; ++j) {
                        if (Array.IndexOf(clip.layers[j].clipElementGuids, guid) == -1) continue;
                        found = true;
                        break;
                    }
                    if (!found) {
                        totUnlayeredClipElements++;
                        DeEditorUtils.Array.RemoveAtIndexAndContract(ref clip.elements, i);
                    }
                }
                EditorUtility.DisplayDialog("Cleanup",
                    string.Format("Elements removed because they had no layer: {0}", totUnlayeredClipElements),
                "Ok");
            }
        }

        public static UnityEvent CloneUnityEvent(UnityEvent unityEvent)
        {
            return unityEvent.Clone();
        }

        public static string ConvertSecondsToTimeString(float seconds, bool showMilliseconds = true, bool ignoreMinutesIfLessThanOne = false)
        {
            int mins = (int)(seconds / 60);
            float secs = seconds % 60;
            float millisecs = (secs - (int)secs) * 1000;
            return !ignoreMinutesIfLessThanOne || mins > 0
                ? showMilliseconds
                    ? string.Format("{0:00}:{1:00}:{2:0000}", mins, (int)secs, millisecs)
                    : string.Format("{0:00}:{1:00}", mins, (int)secs)
                : showMilliseconds
                    ? string.Format("{0:00}:{1:0000}", (int)secs, millisecs)
                    : string.Format("{0:00}", (int)secs);
        }

        /// <summary>
        /// Uses Reflection to determine if the given Component contains a clip.<para/>
        /// </summary>
        public static bool ComponentContainsSerializedClip(Component component)
        {
            _TmpClips.Clear();
            FindSerializedClips(component, _TmpClips, "ANY", true);
            bool result = _TmpClips.Count > 0;
            _TmpClips.Clear();
            return result;
        }

        /// <summary>
        /// Fills the given list with all the GameObjects in the scene,
        /// or the prefab root gameObject if we're in prefab editing mode
        /// </summary>
        public static void FindAllGameObjectsInScene(List<GameObject> fillList, bool rootGameObjectsOnly = false, bool ignoreHidden = true)
        {
            fillList.Clear();
            bool isPrefabStage = IsEditingPrefab();
            GameObject[] allGos = Resources.FindObjectsOfTypeAll<GameObject>();
            if (isPrefabStage) {
                GameObject prefabRootGo = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;
                fillList.Add(prefabRootGo);
            } else {
                foreach (GameObject go in allGos) {
                    Transform root = go.transform.root;
                    bool isSceneGo = !EditorUtility.IsPersistent(root.gameObject)
                                     && !((go.hideFlags & HideFlags.NotEditable) == HideFlags.NotEditable || (go.hideFlags & HideFlags.HideAndDontSave) == HideFlags.HideAndDontSave);
                    bool isValid = isSceneGo
                                   && (!rootGameObjectsOnly || root == go.transform)
                                   && (!ignoreHidden || (go.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy);
                    if (!isValid) continue;
                    fillList.Add(go);
                }
            }
        }

        /// <summary>
        /// Returns a temporary sorted list of <see cref="SelectionClip"/> elements which will be reused for future operations
        /// (and thus shouldn't be stored as-is, or even better should be cleared at the end of the operation)
        /// </summary>
        public static List<SelectionClip> FindAllClipsInSceneNonAlloc(bool sort = false)
        {
            _TmpSelectionClips.Clear();
            FindAllGameObjectsInScene(_TmpGos, true); // Only root gameObjects (will find components in children)
            if (_TmpGos.Count == 0) return _TmpSelectionClips;

            foreach (GameObject go in _TmpGos) {
                Component[] components = go.GetComponentsInChildren<Component>(true);
                foreach (Component c in components) {
                    _TmpClips.Clear();
                    FindSerializedClips(c, _TmpClips, null, true);
                    foreach (DOTweenClip s in _TmpClips) _TmpSelectionClips.Add(new SelectionClip(c, s));
                }
            }
            if (sort && _TmpSelectionClips.Count > 0) {
                _TmpSelectionClips.Sort((a, b) => string.Compare(a.label.text, b.label.text, StringComparison.OrdinalIgnoreCase));
            }
            _TmpClips.Clear();
            _TmpGos.Clear();
            return _TmpSelectionClips;
        }

        // public static SelectionClip FindSerializedClipInScene(string clipGuid)
        // {
        //     FindAllGameObjectsInScene(_TmpGos, true); // Only root gameObjects (will find components in children)
        //     if (_TmpGos.Count == 0) return null;
        //
        //     SelectionClip result = null;
        //     bool found = false;
        //     foreach (GameObject go in _TmpGos) {
        //         Component[] components = go.GetComponentsInChildren<Component>(true);
        //         foreach (Component c in components) {
        //             _TmpClips.Clear();
        //             FindSerializedClips(c, _TmpClips, clipGuid, true);
        //             if (_TmpClips.Count == 0) continue;
        //             found = true;
        //             result = new SelectionClip(c, _TmpClips[0]);
        //             break;
        //         }
        //         if (found) break;
        //     }
        //     _TmpClips.Clear();
        //     _TmpGos.Clear();
        //     return result;
        // }

        /// <summary>
        /// Uses Reflection to find the clip with the given guid in the given Component.<para/>
        /// </summary>
        public static SelectionClip FindSerializedClipInComponent(string clipGuid, Component component, ref bool wasInsideNestedObj)
        {
            _TmpClips.Clear();
            wasInsideNestedObj = FindSerializedClips(component, _TmpClips, clipGuid, true);
            SelectionClip result = _TmpClips.Count > 0 ? new SelectionClip(component, _TmpClips[0]) : null;
            _TmpClips.Clear();
            return result;
        }

        /// <summary>
        /// Uses Reflection to find the clip/s in the given object, eventually looking for the given GUID.<para/>
        /// Returns TRUE if we were looking for a single clip and it was found inside a nested object<para/>
        /// Looking inside all nested objects (even nested of nested) caused a crash before (specifically when entering a TextMeshPro object),
        /// but it's now solved by ignoring nested objects that derive from UnityEngine.Object.<para/>
        /// </summary>
        /// <param name="withinObj">The object inside which to look for clips</param>
        /// <param name="appendToList">List to which to append the results (can't be NULL)</param>
        /// <param name="clipGuid">If NULL is ignored, if not NULL only one clip (the one with this GUID) will be returned.
        /// SPECIAL NOTE: if "ANY" finds the first clip and returns it</param>
        /// <param name="lookInNestedObjs">If TRUE also search nested objects (referenced instances of classes)</param>
        public static bool FindSerializedClips(object withinObj, List<DOTweenClip> appendToList, string clipGuid, bool lookInNestedObjs = true)
        {
            if (withinObj == null) return false;
            bool findByGuid = clipGuid != null;
            bool findAnyFirstClip = findByGuid && clipGuid == "ANY";
            List<object> nestedObjs = null;
            // Find all fields including ones inside base classes
            _TmpFInfos.Clear();
            Type t = withinObj.GetType();
            FieldInfo[] fInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < fInfos.Length; ++i) _TmpFInfos.Add(fInfos[i]);
            t = t.BaseType;
            while (t != null) {
                fInfos = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int i = 0; i < fInfos.Length; ++i) {
                    bool add = true;
                    for (int j = 0; j < _TmpFInfos.Count; ++j) {
                        if (_TmpFInfos[j].Name != fInfos[i].Name) continue;
                        add = false;
                        break;
                    }
                    if (add) _TmpFInfos.Add(fInfos[i]);
                }
                t = t.BaseType;
            }
            // Parse fields
            for (int i = 0; i < _TmpFInfos.Count; ++i) {
                FieldInfo fInfo = _TmpFInfos[i];
                bool isSerialized = fInfo.IsPublic && !Attribute.IsDefined(fInfo, _TNonSerializedAttribute)
                                    || !fInfo.IsPublic && Attribute.IsDefined(fInfo, _TSerializeFieldAttribute);
                if (!isSerialized) continue;
                if (fInfo.FieldType == _TClip) {
                    DOTweenClip s = fInfo.GetValue(withinObj) as DOTweenClip;
                    if (s == null || !findAnyFirstClip && findByGuid && s.guid != clipGuid) continue;
                    appendToList.Add(s);
                    if (findByGuid) {
                        _TmpFInfos.Clear();
                        return false;
                    }
                } else if (fInfo.FieldType == _TClipArray || fInfo.FieldType == _TClipList) {
                    IList<DOTweenClip> listS = fInfo.GetValue(withinObj) as IList<DOTweenClip>;
                    if (listS != null) {
                        foreach (DOTweenClip s in listS) {
                            if (s == null || !findAnyFirstClip && findByGuid && s.guid != clipGuid) continue;
                            appendToList.Add(s);
                            if (findByGuid) {
                                _TmpFInfos.Clear();
                                return false;
                            }
                        }
                    }
                } else if (lookInNestedObjs) {
                    // Non-clip object: check if it's serialized in which case we'll look inside that too
                    object nestedObj = fInfo.GetValue(withinObj);
                    if (nestedObj != null && nestedObj != withinObj && !fInfo.FieldType.IsSubclassOf(_TUnityObject)) {
                        if (nestedObjs == null) nestedObjs = new List<object>();
                        nestedObjs.Add(nestedObj);
                    }
                }
            }
            // Look inside nested serialized classes
            _TmpFInfos.Clear();
            if (lookInNestedObjs && nestedObjs != null) {
                for (int i = 0; i < nestedObjs.Count; ++i) {
                    FindSerializedClips(nestedObjs[i], appendToList, clipGuid, true);
                    if (findByGuid && appendToList.Count > 0) {
                        // _TmpFInfos.Clear();
                        return true;
                    }
                }
            }
            // _TmpFInfos.Clear();
            return false;
        }

        /// <summary>
        /// Generates a flat black texture in case of Custom ease
        /// </summary>
        public static void GenerateEaseTextureIn(Texture2D texture, Ease ease, float overshootOrAmplitude = 1.70158f, float period = 0)
        {
            Vector2Int size = new Vector2Int(texture.width, texture.height);
            int totPixels = size.x * size.y;
            int easeH = (int)(size.y * 0.35f);
            int easeBaseY = (int)((size.y - easeH) * 0.5f);
            int easeTopY = easeBaseY + easeH;
            // Flood fill bg
            if (size != _easeTextureLastSize) {
                _easeTextureLastSize = size;
                _easeTextureFloodFill = new Color32[totPixels];
                for (int i = 0; i < totPixels; ++i) _easeTextureFloodFill[i] = Color.black;
            }
            texture.SetPixels32(_easeTextureFloodFill);
            //
            if (ease != Ease.INTERNAL_Custom) {
                for (int i = 0; i < size.x; ++i) {
                    int x = i;
                    int y = (int)DOVirtual.EasedValue(easeBaseY, easeTopY, (float)i / size.x, ease, overshootOrAmplitude, period);
                    texture.SetPixel(x, easeBaseY, _EaseLimiterColor);
                    texture.SetPixel(x, easeTopY, _EaseLimiterColor);
                    if (y >= size.y || y <= 0) continue;
                    texture.SetPixel(x, y, _EaseColor);
                }
            }
            texture.Apply();
        }

        public static string GetCleanType(Type type)
        {
            string s = type.ToString();
            int index = s.LastIndexOf('.');
            return index == -1 ? s : s.Substring(index + 1);
        }

        public static Rect GetLayerRect(int layerIndex, float timelineAreaWidth)
        {
            return new Rect(
                0, _layout.partialOffset.y + _settings.layerHeight * (layerIndex - _layout.firstVisibleLayerIndex),
                timelineAreaWidth, _settings.layerHeight
            );
        }

        // Returns -1 if the clipElement isn't linked to any layer (can happen when creating a new clip from an array in the Inspector)
        public static int GetClipElementLayerIndex(DOTweenClip clip, string clipElementGuid)
        {
            int lLen = clip.layers.Length;
            for (int i = 0; i < lLen; ++i) {
                DOTweenClip.ClipLayer layer = clip.layers[i];
                int gLen = layer.clipElementGuids.Length;
                for (int j = 0; j < gLen; ++j) {
                    if (layer.clipElementGuids[j] == clipElementGuid) return i;
                }
            }
            return -1;
        }

        public static Color GetClipElementBaseColor(DOTweenClipElement clipElement)
        {
            switch (clipElement.type) {
            case DOTweenClipElement.Type.Event:
                return DOEGUI.Colors.timeline.sEvent;
            case DOTweenClipElement.Type.Action:
                return DOEGUI.Colors.timeline.sAction;
            case DOTweenClipElement.Type.Interval:
                return DOEGUI.Colors.timeline.sInterval;
            case DOTweenClipElement.Type.GlobalTween:
                return DOEGUI.Colors.timeline.sGlobalTween;
            default:
                return DOEGUI.Colors.timeline.sTween;
            }
        }

        public static Rect GetClipElementRect(DOTweenClipElement clipElement, float timelineAreaWidth)
        {
            int layerIndex = _clip.FindClipElementLayerIndexByGuid(clipElement.guid);
            return GetClipElementRect(clipElement, GetLayerRect(layerIndex, timelineAreaWidth));
        }
        public static Rect GetClipElementRect(DOTweenClipElement clipElement, Rect layerRect)
        {
            return new Rect(
                (int)_layout.GetTimelineXAtTime(clipElement.startTime), layerRect.y + 1,
                (int)(clipElement.Editor_DrawDuration() * _settings.secondToPixels), layerRect.height - 2
            );
        }

        public static SerializedProperty GetSerializedClip(Component fromSrc, string clipGuid)
        {
            // Debug.Log("GetSerializedClip " + fromSrc.name);
            SerializedObject so = new SerializedObject(fromSrc);
            SerializedProperty iterator = fromSrc.GetType() == typeof(DOTweenClipCollection)
                ? so.FindProperty("clips")
                : so.GetIterator();
            while (iterator.Next(true)) {
                if (iterator.type != "DOTweenClip") continue;
                // Debug.Log("   found A DOTweenClip: " + iterator.type + ", " + iterator.propertyType);
                if (iterator.isArray) {
                    for (int i = 0; i < iterator.arraySize; ++i) {
                        SerializedProperty iteratorMember = iterator.GetArrayElementAtIndex(i);
                        if (iteratorMember.FindPropertyRelative("_guid").stringValue == clipGuid) return iteratorMember;
                    }
                } else {
                    // Debug.Log("   found CORRECT DOTweenClip: " + iterator.type + ", " + iterator.propertyType);
                    if (iterator.FindPropertyRelative("_guid").stringValue == clipGuid) return iterator;
                }
            }
            return null;
        }

        /// <summary>
        /// Adds to the given list all the <see cref="DOTweenClip"/> serializedProperties in the component
        /// </summary>
        public static void GetAllSerializedClipsInComponent(Component fromSrc, SerializedObject so, List<SerializedProperty> addToList)
        { GetAllSerializedClipsOrVariantsInComponent(false, fromSrc, so, addToList); }
        /// <summary>
        /// Adds to the given list all the <see cref="DOTweenClipVariant"/> serializedProperties in the component
        /// </summary>
        public static void GetAllSerializedClipVariantsInComponent(Component fromSrc, SerializedObject so, List<SerializedProperty> addToList)
        { GetAllSerializedClipsOrVariantsInComponent(true, fromSrc, so, addToList); }
        static void GetAllSerializedClipsOrVariantsInComponent(bool isClipVariants, Component fromSrc, SerializedObject so, List<SerializedProperty> addToList)
        {
            string type = isClipVariants ? "DOTweenClipVariant" : "DOTweenClip";
            SerializedProperty iterator = !isClipVariants && fromSrc.GetType() == typeof(DOTweenClipCollection)
                ? so.FindProperty("clips")
                : so.GetIterator();
            while (iterator.Next(true)) {
                if (iterator.type != type) continue;
                if (iterator.isArray) {
                    for (int i = 0; i < iterator.arraySize; ++i) {
                        SerializedProperty iteratorMember = iterator.GetArrayElementAtIndex(i);
                        addToList.Add(iteratorMember);
                    }
                } else addToList.Add(iterator);
            }
        }

        public static SerializedProperty GetSerializedClipElement(SerializedProperty spClip, string clipElementGuid)
        {
            SerializedProperty clipElements = spClip.FindPropertyRelative("elements");
//            Debug.Log("GetSerializedClipElement (tot elements: " + clipElements.arraySize + ")");
            for (int i = 0; i < clipElements.arraySize; ++i) {
                SerializedProperty spClipElement = clipElements.GetArrayElementAtIndex(i);
                if (spClipElement.FindPropertyRelative("_guid").stringValue != clipElementGuid) continue;
//                Debug.Log("   found CORRECT DOTweenClipElement: " + spClipElement.type + ", " + spClipElement.propertyType);
                return spClipElement;
            }
            return null;
        }

        public static bool IsEditingPrefab()
        {
            // HACK uses experimental PrefabSceneUtility
            // (should've been non-experimental in Unity 2019 but still is :|)
//            return EditorSceneManager.previewSceneCount > 1 && PrefabStageUtility.GetCurrentPrefabStage() != null;
            return PrefabStageUtility.GetCurrentPrefabStage() != null;
        }

        // Returns TRUE if we're in prefab editing mode and prefab editing mode has autoSave active
        public static PrefabEditSaveMode GetPrefabEditSaveMode()
        {
            if (!IsEditingPrefab()) return PrefabEditSaveMode.Undetermined;
            if (_piPrefabAutoSave == null) {
                _piPrefabAutoSave = typeof(PrefabStage).GetProperty("autoSave", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            }
            if (_piPrefabAutoSave == null) return PrefabEditSaveMode.Undetermined;
            bool autoSaveActive = (bool)_piPrefabAutoSave.GetValue(PrefabStageUtility.GetCurrentPrefabStage());
            return autoSaveActive ? PrefabEditSaveMode.AutoSave : PrefabEditSaveMode.ManualSave;
        }

        public static void RegisterCompleteSceneUndo(string undoLabel)
        {
            FindAllGameObjectsInScene(_TmpGos, true);
            foreach (GameObject go in _TmpGos) {
                Undo.RegisterFullObjectHierarchyUndo(go, undoLabel);
            }
            _TmpGos.Clear();
        }

        public static void RemoveLayer(DOTweenClip fromClip, int layerIndex)
        {
            DOTweenClip.ClipLayer layer = fromClip.layers[layerIndex];
            foreach (string guid in layer.clipElementGuids) {
                int clipElementIndex = fromClip.Editor_GetClipElementIndex(guid);
                if (clipElementIndex == -1) continue;
                DeEditorUtils.Array.RemoveAtIndexAndContract(ref fromClip.elements, clipElementIndex);
            }
            DeEditorUtils.Array.RemoveAtIndexAndContract(ref fromClip.layers, layerIndex);
        }

        public static void RemoveClipElement(DOTweenClip fromClip, string clipElementGuid)
        {
            for (int i = 0; i < fromClip.elements.Length; ++i) {
                if (fromClip.elements[i].guid != clipElementGuid) continue;
                DeEditorUtils.Array.RemoveAtIndexAndContract(ref fromClip.elements, i);
                break;
            }
            for (int i = 0; i < fromClip.layers.Length; ++i) {
                DOTweenClip.ClipLayer layer = fromClip.layers[i];
                for (int j = 0; j < layer.clipElementGuids.Length; ++j) {
                    if (layer.clipElementGuids[j] != clipElementGuid) continue;
                    DeEditorUtils.Array.RemoveAtIndexAndContract(ref layer.clipElementGuids, j);
                    break;
                }
            }
        }

        public static void ShiftClipElementToLayer(DOTweenClip clip, DOTweenClipElement clipElement, int fromLayerIndex, int toLayerIndex)
        {
            DOTweenClip.ClipLayer fromLayer = clip.layers[fromLayerIndex];
            DOTweenClip.ClipLayer toLayer = clip.layers[toLayerIndex];
            int fromIndex = Array.IndexOf(fromLayer.clipElementGuids, clipElement.guid);
            if (fromIndex == -1) return;
            DeEditorUtils.Array.RemoveAtIndexAndContract(ref fromLayer.clipElementGuids, fromIndex);
            DeEditorUtils.Array.ExpandAndAdd(ref toLayer.clipElementGuids, clipElement.guid);
        }

        public static int SortPlugDataLabels(string a, string b)
        {
            const char divider = '/';
            int aDividers = 0, bDividers = 0;
            foreach (char c in a) {
                if (c == divider) aDividers++;
            }
            foreach (char c in b) {
                if (c == divider) bDividers++;
            }
            // if (aDividers > bDividers) return -1;
            // if (aDividers < bDividers) return 1;
            if (aDividers == 0 && bDividers > 0) return 1;
            if (aDividers > 0 && bDividers == 0) return -1;
            return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
        }

        public static void StoreTransform(Transform t)
        {
            transformSnapshot = new TransformSnapshot(t);
        }

        public static void UpdateSecondToPixels(int newValue, float? mouseOffsetX = null)
        {
            float offsetX = mouseOffsetX == null ? 0 : (float)mouseOffsetX;
            float scaleFactor = (float)newValue / _settings.secondToPixels;
            float newShiftX = _clip.editor.areaShift.x * scaleFactor + (offsetX - offsetX * scaleFactor);
            _clip.editor.areaShift = new Vector2(Mathf.Min(0, newShiftX), _clip.editor.areaShift.y);
            _settings.secondToPixels = newValue;
        }

        public static void UpdateLayerHeight(int newValue, float? mouseOffsetY = null)
        {
            float offsetY = mouseOffsetY == null ? 0 : (float)mouseOffsetY;
            float scaleFactor = (float)newValue / _settings.layerHeight;
            float newShifty = _clip.editor.areaShift.y * scaleFactor + (offsetY - offsetY * scaleFactor);
            _clip.editor.areaShift = new Vector2(_clip.editor.areaShift.x, Mathf.Min(0, newShifty));
            _settings.layerHeight = newValue;
        }

        /// <summary>
        /// Validates the clip by:<para/>
        /// - looking for elements that have no GUID assigned
        /// - looking for layers that refer to non-existing elements GUIDs, in which case automatically removes them<para/>
        /// - looking for clipElement that aren't linked to any layer, in which case automatically adds a new layer and links it<para/>
        /// Returns TRUE if fixes were applied and the Component needs to be saved
        /// </summary>
        public static bool ValidateAndFixClip(DOTweenClip clip, SerializedProperty spClip)
        {
            _Strb.Length = 0;
            bool hasAppliedFixes = false;
            int totNullClipElementGuids = 0, totNullLayerClipElementGuids = 0, totUnusedGuids = 0, totUnlinkedClipElement = 0;
            // Look for clipElement with empty GUID and layers that store and empty clipElement GUID
            foreach (DOTweenClipElement clipElement in clip.elements) {
                if (!string.IsNullOrEmpty(clipElement.guid)) continue;
                // Assign guid to clipElement (will be assigned to new layer later below) and activate them
                hasAppliedFixes = true;
                totNullClipElementGuids++;
                clipElement.isActive = true;
                clipElement.Editor_RegenerateGuid();
            }
            // Look for layers that refer to non-existent or empty clipElement GUIDs
            foreach (DOTweenClip.ClipLayer layer in clip.layers) {
                for (int i = layer.clipElementGuids.Length - 1; i > -1; --i) {
                    string guid = layer.clipElementGuids[i];
                    if (string.IsNullOrEmpty(guid)) {
                        // Remove guid since it's empty
                        hasAppliedFixes = true;
                        totNullLayerClipElementGuids++;
                        DeEditorUtils.Array.RemoveAtIndexAndContract(ref layer.clipElementGuids, i);
                    } else {
                        bool guidIsValid = false;
                        foreach (DOTweenClipElement clipElement in clip.elements) {
                            if (clipElement.guid != guid) continue;
                            guidIsValid = true;
                            break;
                        }
                        if (guidIsValid) continue;
                        // Remove guid since there's no clipElement that uses it
                        hasAppliedFixes = true;
                        totUnusedGuids++;
                        DeEditorUtils.Array.RemoveAtIndexAndContract(ref layer.clipElementGuids, i);
                    }
                }
            }
            // Look for elements that are not referred by any layer
            int sLen = clip.elements.Length;
            for (int i = 0; i < sLen; ++i) {
                if (GetClipElementLayerIndex(clip, clip.elements[i].guid) != -1) continue;
                // Missing layer for clipElement. Add new layer and add clipElement to it
                hasAppliedFixes = true;
                totUnlinkedClipElement++;
                DOTweenClip.ClipLayer newLayer = new DOTweenClip.ClipLayer("Layer " + (clip.layers.Length + 1));
                DeEditorUtils.Array.ExpandAndAdd(ref newLayer.clipElementGuids, clip.elements[i].guid);
                DeEditorUtils.Array.ExpandAndAdd(ref clip.layers, newLayer);
            }
            if (hasAppliedFixes) {
                _Strb.Append("Some errors were found and fixed in this DOTweenClip:");
                if (totNullClipElementGuids > 0) {
                    _Strb.Append("\n- ").Append(totNullClipElementGuids).Append(" clipElement GUIDs were regenerated because they were unset");
                }
                if (totNullLayerClipElementGuids > 0) {
                    _Strb.Append("\n- ").Append(totNullLayerClipElementGuids).Append(" null/empty clipElement GUIDs removed from layers");
                }
                if (totUnusedGuids > 0) {
                    _Strb.Append("\n- ").Append(totUnusedGuids).Append(" unused clipElement GUIDs were removed from layers");
                }
                if (totUnlinkedClipElement > 0) {
                    _Strb.Append("\n- ").Append(totUnlinkedClipElement)
                        .Append(" clipElement not referred by any layer were re-added by adding new layers");
                }
                EditorUtility.DisplayDialog("DOTweenClip Problems", _Strb.ToString(), "Ok");
                _Strb.Length = 0;
            }
            return hasAppliedFixes;
        }

        /// <summary>
        /// Returns FALSE if the plugin used is invalid (happens if the plugins were changed and the clipElement refers to an older version) or NULL.
        /// </summary>
        public static bool ValidateClipElementPlugin(DOTweenClipElement clipElement, DOVisualTweenPlugin plugin, bool isGlobal)
        {
            if (plugin == null) return false;
            if (!plugin.HasPlugData(clipElement)) return false;
            if (!isGlobal && clipElement.target != null) {
                Type targetType = clipElement.target.GetType();
                if (targetType != plugin.targetType && !targetType.IsSubclassOf(plugin.targetType)) return false;
            }
            return true;
        }

        /// <summary>
        /// Returns FALSE if the plugin used is invalid (happens if the plugins were changed and the clipElement refers to an older version).
        /// </summary>
        public static bool ValidateClipElementPlugin(DOTweenClipElement clipElement, DOVisualActionPlugin plugin)
        {
            if (plugin == null) return false;
            PlugDataAction plugData = plugin.GetPlugData(clipElement);
            if (plugData == null) return false;
            if (plugData.wantsTarget && clipElement.target != null) {
                Type targetType = clipElement.target.GetType();
                if (targetType != plugData.targetType && !targetType.IsSubclassOf(plugData.targetType)) return false;
            }
            return true;
        }

        #region Context Menus

        public static void CM_SelectClipElementTargetFromGameObject(GameObject go, Action<Component> onSelect)
        {
            // HACK mysterious undoPerformed fix but only for prefabs
            // For some reason, after choosing the menu item in a prefab instance (and not in any other case)
            // an UNDO operation is fired, which clears the newly added clipElement.
            // Settings things dirty here prevents the UNDO operation to be fired at all.
            DOTweenClipTimeline.editor.MarkDirty();
            // hack end

            GenericMenu menu = new GenericMenu();
            Component[] components = go.GetComponents<Component>();
            Array.Sort(components, (a, b) => {
                bool aSupported = DOVisualPluginsManager.GetTweenPlugin(a) != null;
                bool bSupported = DOVisualPluginsManager.GetTweenPlugin(b) != null;
                if (aSupported && !bSupported) return -1;
                if (!aSupported && bSupported) return 1;
                return 0;
            });
            for (int i = 0; i < components.Length; ++i) {
                Component component = components[i];
                DOVisualTweenPlugin plugin = DOVisualPluginsManager.GetTweenPlugin(component);
                if (plugin != null) {
                    string itemName = component.GetType().Editor_GetShortName();
                    if (plugin.isSupportedViaSubtype) itemName += string.Format(" (as {0})", plugin.subtypeId);
                    menu.AddItem(new GUIContent(itemName), false, ()=> onSelect(component));
                } else {
                    menu.AddDisabledItem(new GUIContent("- (Not Supported) " + component.GetType().Editor_GetShortName()));
                }
            }
            menu.DropDown(new Rect(Event.current.mousePosition.x, Event.current.mousePosition.y, 0, 0));
            // menu.ShowAsContext();
        }

        /// <summary>
        /// If <see cref="onSelected"/> is NULL automatically opens the clip on selection, otherwise just fires the event
        /// </summary>
        public static void CM_SelectClipInScene(Rect buttonR, DOTweenClip currSelectedClip = null, bool addNoneItem = false, Action<DOTweenClip,Component> onSelected = null)
        {
            GenericMenu menu = new GenericMenu();
            if (IsEditingPrefab()) {
                menu.AddDisabledItem(new GUIContent("× Selection disabled in Prefab editing mode"));
            } else {
                List<SelectionClip> sceneClips = FindAllClipsInSceneNonAlloc(true);
                if (sceneClips.Count == 0) {
                    menu.AddDisabledItem(new GUIContent("No DOTweenClips in Scene"));
                } else {
                    if (addNoneItem) {
                        menu.AddItem(new GUIContent("None"), false, () => {
                            if (onSelected != null) onSelected(null, null);
                        });
                    }
                    foreach (SelectionClip sel in sceneClips) {
                        bool selected = currSelectedClip != null && currSelectedClip == sel.clip;
                        menu.AddItem(sel.label, selected, () => {
                            if (onSelected != null) onSelected(sel.clip, sel.component);
                            else DOTweenClipTimeline.ShowWindow(sel.component, sel.clip, null);
                        });
                    }
                }
                sceneClips.Clear();
            }
            menu.DropDown(buttonR.SetX(buttonR.xMax).SetY(buttonR.y - buttonR.height));
        }

        #endregion

        #endregion

        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████
        // ███ INTERNAL CLASSES ████████████████████████████████████████████████████████████████████████████████████████████████
        // █████████████████████████████████████████████████████████████████████████████████████████████████████████████████████

        public class SelectionClip
        {
            public Component component;
            public DOTweenClip clip;
            public GUIContent label;
            public SelectionClip(Component component, DOTweenClip clip)
            {
                this.component = component;
                this.clip = clip;
                label = new GUIContent(string.Format("{0}/{1}{2}",
                    component.name,
                    clip.isActive ? "" : "[× INACTIVE] ",
                    clip.name.IsNullOrEmpty() ? string.Format("[unnamed - {0}]", clip.guid) : clip.name
                ));
            }
        }

        public struct TransformSnapshot
        {
            public Vector3 position, localPosition, eulerAngles, localEulerAngles, localScale;
            public Quaternion rotation, localRotation;
            public TransformSnapshot(Transform t)
            {
                this.position = t.position;
                this.localPosition = t.localPosition;
                this.eulerAngles = t.eulerAngles;
                this.localEulerAngles = t.localEulerAngles;
                this.localScale = t.localScale;
                this.rotation = t.rotation;
                this.localRotation = t.localRotation;
            }
        }
    }
}