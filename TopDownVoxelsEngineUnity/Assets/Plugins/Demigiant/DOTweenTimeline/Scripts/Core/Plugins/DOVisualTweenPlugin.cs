// Author: Daniele Giardini - http://www.demigiant.com
// Created: 2020/01/21

using System;
using System.Collections.Generic;
using DG.Tweening.Core;
using DG.Tweening.Plugins;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace DG.Tweening.Timeline.Core.Plugins
{
    /// <summary>
    /// Contains/executes all possible tween types based on its componentType
    /// </summary>
    public class DOVisualTweenPlugin : IPlugin
    {
        public readonly Type targetType;
        public readonly ITweenPluginData[] pluginDatas; // Always sorted by label in editor windows
        public int totPluginDatas { get; }
        public IPluginData[] editor_iPluginDatas { get { return pluginDatas; } }
        public bool isSupportedViaSubtype { get; private set; } // Set to true when registering this plugin type via a subtype (like TMP_Text as Graphic)
        public string subtypeId { get; private set; } // Defined when editor_isSupportedAsSubtype is true (used only for menus and visual cues)

        readonly Dictionary<string, ITweenPluginData> _guidToPlugData = new Dictionary<string, ITweenPluginData>();

        public DOVisualTweenPlugin(Type targetType, params ITweenPluginData[] pluginDatas)
        {
            this.targetType = targetType;
            this.pluginDatas = pluginDatas;
            totPluginDatas = this.pluginDatas.Length;

            for (int i = 0; i < totPluginDatas; ++i) {
                string plugDataGuid = this.pluginDatas[i].guid ?? "INVALID:" + Guid.NewGuid(); // Missing GUID, assign a unique one but marked as invalid
                if (_guidToPlugData.ContainsKey(plugDataGuid)) {
                    DOLog.Error(string.Format("Another ITweenPluginData with the same guid for \"{0}\" already exists (guid: \"{1}\")", targetType, plugDataGuid));
                } else _guidToPlugData.Add(plugDataGuid, this.pluginDatas[i]);
            }
        }

        #region Public Methods

        /// <summary>
        /// Gets a <see cref="ITweenPluginData"/> by the given clipElement plugDataGuid, and if it doesn't find it falls back on the clipElement plugDataIndex.
        /// Returns NULL if both retrieval methods fail.
        /// </summary>
        public ITweenPluginData GetPlugData(DOTweenClipElement clipElement)
        {
            return GetPlugData(clipElement.plugDataGuid, clipElement.plugDataIndex);
        }
        /// <summary>
        /// Gets a <see cref="ITweenPluginData"/> by the given clipElement plugDataGuid, and if it doesn't find it falls back on the clipElement plugDataIndex.
        /// Returns NULL if both retrieval methods fail.
        /// </summary>
        public ITweenPluginData GetPlugData(string plugDataGuid, int plugDataIndex)
        {
            return plugDataGuid != null && _guidToPlugData.TryGetValue(plugDataGuid, out ITweenPluginData plugData)
                ? plugData
                : string.IsNullOrEmpty(plugDataGuid) && plugDataIndex < totPluginDatas ? pluginDatas[plugDataIndex] : null;
        }

        public bool HasPlugData(DOTweenClipElement clipElement)
        {
            if (!string.IsNullOrEmpty(clipElement.plugDataGuid)) return _guidToPlugData.ContainsKey(clipElement.plugDataGuid);
            return clipElement.plugDataIndex < totPluginDatas;
        }

        // Assumes target is not NULL. Returns NULL if plugData wasn't found.
        // NOTE: target is passed separately so it can be eventually replaced
        public Tweener CreateTween(DOTweenClipElement clipElement, object target, float timeMultiplier, ITweenPluginData plugData)
        {
            Tweener t = null;
            float duration = clipElement.duration * timeMultiplier;
            bool directTo = clipElement.toType == DOTweenClipElement.ToFromType.Direct;
            bool directFrom = clipElement.fromType == DOTweenClipElement.ToFromType.Direct;
            bool directFromWDynamicTo = directFrom && !directTo;
            bool directToAndFrom = directTo && directFrom;
            switch (plugData.propertyType) {
            case DOTweenClipElement.PropertyType.Float: //---------------------------------------------------------
                DOGetter<float> floatGetter = plugData.FloatGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<float, float, FloatOptions> floatT = DOTween.To(
                    floatGetter, plugData.FloatSetter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromFloatVal : clipElement.toFloatVal, duration
                );
                floatT.SetOptions(clipElement.boolOption0);
                if (directFrom) {
                    if (directToAndFrom) floatT.From(clipElement.fromFloatVal, true, clipElement.isRelative);
                    else floatT.From(true, clipElement.isRelative);
                }
                floatT.SetRelative(clipElement.isRelative);
                t = floatT;
                // TweenerCore<float, float, FloatOptions> floatT;
                // DOGetter<float> floatGetter = plugData.FloatGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     floatT = DOTween.To(
                //         floatGetter, plugData.FloatSetter(target, clipElement.stringOption0, clipElement.intOption1), floatGetter(), duration
                //     );
                //     break;
                // default:
                //     floatT = DOTween.To(
                //         floatGetter, plugData.FloatSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         clipElement.isRelative ? floatGetter() + clipElement.toFloatVal : clipElement.toFloatVal, duration
                //     );
                //     break;
                // }
                // floatT.SetOptions(clipElement.boolOption0);
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     floatT.From(clipElement.isRelative ? floatGetter() + clipElement.fromFloatVal : clipElement.fromFloatVal);
                // }
                // t = floatT;
                break;
            case DOTweenClipElement.PropertyType.Int: //---------------------------------------------------------
                DOGetter<int> intGetter = plugData.IntGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<int, int, NoOptions> intT = DOTween.To(
                    intGetter, plugData.IntSetter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromIntVal : clipElement.toIntVal, duration
                );
                if (directFrom) {
                    if (directToAndFrom) intT.From(clipElement.fromIntVal, true, clipElement.isRelative);
                    else intT.From(true, clipElement.isRelative);
                }
                intT.SetRelative(clipElement.isRelative);
                t = intT;
                // TweenerCore<int, int, NoOptions> intT;
                // DOGetter<int> intGetter = plugData.IntGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     intT = DOTween.To(
                //         intGetter, plugData.IntSetter(target, clipElement.stringOption0, clipElement.intOption1), intGetter(), duration
                //         );
                //     break;
                // default:
                //     intT = DOTween.To(
                //         intGetter, plugData.IntSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         clipElement.isRelative ? intGetter() + clipElement.toIntVal : clipElement.toIntVal, duration
                //     );
                //     break;
                // }
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     intT.From(clipElement.isRelative ? intGetter() + clipElement.fromIntVal : clipElement.fromIntVal);
                // }
                // t = intT;
                break;
            case DOTweenClipElement.PropertyType.Uint: //---------------------------------------------------------
                DOGetter<uint> uintGetter = plugData.UintGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<uint, uint, UintOptions> uintT = DOTween.To(
                    uintGetter, plugData.UintSetter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromUintVal : clipElement.toUintVal, duration
                );
                if (directFrom) {
                    if (directToAndFrom) uintT.From(clipElement.fromUintVal, true, clipElement.isRelative);
                    else uintT.From(true, clipElement.isRelative);
                }
                uintT.SetRelative(clipElement.isRelative);
                t = uintT;
                // TweenerCore<uint, uint, UintOptions> uintT;
                // DOGetter<uint> uintGetter = plugData.UintGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     uintT = DOTween.To(
                //         uintGetter, plugData.UintSetter(target, clipElement.stringOption0, clipElement.intOption1), uintGetter(), duration
                //         );
                //     break;
                // default:
                //     uintT = DOTween.To(
                //         uintGetter, plugData.UintSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         clipElement.isRelative ? uintGetter() + clipElement.toUintVal : clipElement.toUintVal, duration
                //     );
                //     break;
                // }
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     uintT.From(clipElement.isRelative ? uintGetter() + clipElement.fromUintVal : clipElement.fromUintVal);
                // }
                // t = uintT;
                break;
            case DOTweenClipElement.PropertyType.String: //---------------------------------------------------------
                DOGetter<string> stringGetter = plugData.StringGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<string, string, StringOptions> stringT = DOTween.To(
                    stringGetter, plugData.StringSetter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromStringVal : clipElement.toStringVal, duration
                );
                stringT.SetOptions(
                    clipElement.boolOption0, clipElement.intOption0 == 0 ? ScrambleMode.None : (ScrambleMode)clipElement.intOption1, clipElement.stringOption0
                );
                if (directFrom) {
                    if (directToAndFrom) stringT.From(clipElement.fromStringVal, true, clipElement.isRelative);
                    else stringT.From(true, clipElement.isRelative);
                }
                stringT.SetRelative(clipElement.isRelative);
                t = stringT;
                // TweenerCore<string, string, StringOptions> stringT;
                // DOGetter<string> stringGetter = plugData.StringGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     stringT = DOTween.To(
                //         stringGetter, plugData.StringSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         stringGetter(), duration
                //     );
                //     break;
                // default:
                //     stringT = DOTween.To(
                //         stringGetter, plugData.StringSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         clipElement.isRelative ? stringGetter() + clipElement.toStringVal : clipElement.toStringVal, duration
                //     );
                //     break;
                // }
                // stringT.SetOptions(
                //     clipElement.boolOption0, clipElement.intOption0 == 0 ? ScrambleMode.None : (ScrambleMode)clipElement.intOption1, clipElement.stringOption0
                // );
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     stringT.From(clipElement.isRelative ? stringGetter() + clipElement.fromStringVal : clipElement.fromStringVal);
                // }
                // t = stringT;
                break;
            case DOTweenClipElement.PropertyType.Vector2: //---------------------------------------------------------
                switch (plugData.tweenType) {
                case PluginTweenType.ShapeCircle: //---------
                    DOGetter<Vector2> vector2CircleGetter = plugData.Vector2Getter(target, clipElement.stringOption0, clipElement.intOption1);
                    Vector2 circleCenter = clipElement.toVector2Val;
                    float circleToDegrees = clipElement.toFloatVal;
                    float circleFromDegrees = clipElement.fromFloatVal;
                    bool circleHasRelativeCenter = clipElement.intOption1 == 1;
                    TweenerCore<Vector2, Vector2, CircleOptions> circleT = DOTween.To(CirclePlugin.Get(),
                        vector2CircleGetter, plugData.Vector2Setter(target, clipElement.stringOption0, clipElement.intOption1),
                        circleCenter, duration
                    );
                    circleT.SetOptions(circleToDegrees, circleHasRelativeCenter, clipElement.boolOption0);
                    if (directTo) {
                        if (directFrom) {
                            circleT.From(new Vector2(circleFromDegrees, 0), true, clipElement.isRelative);
                        }
                    } else {
                        if (directFrom) {
                            circleT.From(true, clipElement.isRelative);
                        }
                    }
                    circleT.SetRelative(clipElement.isRelative);
                    t = circleT;
                    break;
                default:
                    DOGetter<Vector2> vector2Getter = plugData.Vector2Getter(target, clipElement.stringOption0, clipElement.intOption1);
                    TweenerCore<Vector2, Vector2, VectorOptions> vector2T = DOTween.To(
                        vector2Getter, plugData.Vector2Setter(target, clipElement.stringOption0, clipElement.intOption1),
                        directFromWDynamicTo ? clipElement.fromVector2Val : clipElement.toVector2Val, duration
                    );
                    vector2T.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                    if (directFrom) {
                        if (directToAndFrom) vector2T.From(clipElement.fromVector2Val, true, clipElement.isRelative);
                        else vector2T.From(true, clipElement.isRelative);
                    }
                    vector2T.SetRelative(clipElement.isRelative);
                    t = vector2T;
                    // TweenerCore<Vector2, Vector2, VectorOptions> vector2T;
                    // DOGetter<Vector2> vector2Getter = plugData.Vector2Getter(target, clipElement.stringOption0, clipElement.intOption1);
                    // switch (clipElement.toType) {
                    // case DOTweenClipElement.ToFromType.Dynamic:
                    //     vector2T = DOTween.To(
                    //         vector2Getter, plugData.Vector2Setter(target, clipElement.stringOption0, clipElement.intOption1),
                    //         vector2Getter(), duration
                    //     );
                    //     break;
                    // default:
                    //     vector2T = DOTween.To(
                    //         vector2Getter, plugData.Vector2Setter(target, clipElement.stringOption0, clipElement.intOption1),
                    //         clipElement.isRelative ? vector2Getter() + clipElement.toVector2Val : clipElement.toVector2Val, duration
                    //     );
                    //     break;
                    // }
                    // vector2T.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                    // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                    //     bool noAxisConstraint = clipElement.axisConstraint == AxisConstraint.None;
                    //     Vector2 fromVector2ValFiltered = new Vector2(
                    //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.X ? clipElement.fromVector2Val.x : 0,
                    //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.Y ? clipElement.fromVector2Val.y : 0
                    //     );
                    //     vector2T.From(clipElement.isRelative ? vector2Getter() + fromVector2ValFiltered : fromVector2ValFiltered);
                    // }
                    // t = vector2T;
                    break;
                }
                break;
            case DOTweenClipElement.PropertyType.Vector3: //---------------------------------------------------------
                switch (plugData.tweenType) {
                case PluginTweenType.Punch: //---------------
                    TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> punchT;
                    punchT = DOTween.Punch(
                        plugData.Vector3Getter(target, clipElement.stringOption0, clipElement.intOption1),
                        plugData.Vector3Setter(target, clipElement.stringOption0, clipElement.intOption1),
                        clipElement.toVector3Val, duration, clipElement.intOption1, clipElement.floatOption0
                    );
                    punchT.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                    t = punchT;
                    break;
                case PluginTweenType.Shake: //---------------
                    TweenerCore<Vector3, Vector3[], Vector3ArrayOptions> shakeT;
                    shakeT = DOTween.Shake(
                        plugData.Vector3Getter(target, clipElement.stringOption0, clipElement.intOption1),
                        plugData.Vector3Setter(target, clipElement.stringOption0, clipElement.intOption1),
                        duration, clipElement.toVector3Val, clipElement.intOption1, clipElement.floatOption0, clipElement.intOption0 == 1
                    );
                    shakeT.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                    t = shakeT;
                    break;
                default: //---------------
                    DOGetter<Vector3> vector3Getter = plugData.Vector3Getter(target, clipElement.stringOption0, clipElement.intOption1);
                    TweenerCore<Vector3, Vector3, VectorOptions> vector3T = DOTween.To(
                        vector3Getter, plugData.Vector3Setter(target, clipElement.stringOption0, clipElement.intOption1),
                        directFromWDynamicTo ? clipElement.fromVector3Val : clipElement.toVector3Val, duration
                    );
                    vector3T.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                    if (directFrom) {
                        if (directToAndFrom) vector3T.From(clipElement.fromVector3Val, true, clipElement.isRelative);
                        else vector3T.From(true, clipElement.isRelative);
                    }
                    vector3T.SetRelative(clipElement.isRelative);
                    t = vector3T;
                    // TweenerCore<Vector3, Vector3, VectorOptions> vector3T;
                    // DOGetter<Vector3> vector3Getter = plugData.Vector3Getter(target, clipElement.stringOption0, clipElement.intOption1);
                    // switch (clipElement.toType) {
                    // case DOTweenClipElement.ToFromType.Dynamic:
                    //     vector3T = DOTween.To(
                    //         vector3Getter, plugData.Vector3Setter(target, clipElement.stringOption0, clipElement.intOption1),
                    //         vector3Getter(), duration
                    //     );
                    //     break;
                    // default:
                    //     vector3T = DOTween.To(
                    //         vector3Getter, plugData.Vector3Setter(target, clipElement.stringOption0, clipElement.intOption1),
                    //         clipElement.isRelative ? vector3Getter() + clipElement.toVector3Val : clipElement.toVector3Val, duration
                    //     );
                    //     break;
                    // }
                    // vector3T.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                    // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                    //     bool noAxisConstraint = clipElement.axisConstraint == AxisConstraint.None;
                    //     Vector3 fromVector3ValFiltered = new Vector3(
                    //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.X ? clipElement.fromVector3Val.x : 0,
                    //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.Y ? clipElement.fromVector3Val.y : 0,
                    //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.Z ? clipElement.fromVector3Val.z : 0
                    //     );
                    //     vector3T.From(clipElement.isRelative ? vector3Getter() + fromVector3ValFiltered : fromVector3ValFiltered);
                    // }
                    // t = vector3T;
                    break;
                }
                break;
            case DOTweenClipElement.PropertyType.Vector4: //---------------------------------------------------------
                DOGetter<Vector4> vector4Getter = plugData.Vector4Getter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<Vector4, Vector4, VectorOptions> vector4T = DOTween.To(
                    vector4Getter, plugData.Vector4Setter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromVector4Val : clipElement.toVector4Val, duration
                );
                vector4T.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                if (directFrom) {
                    if (directToAndFrom) vector4T.From(clipElement.fromVector4Val, true, clipElement.isRelative);
                    else vector4T.From(true, clipElement.isRelative);
                }
                vector4T.SetRelative(clipElement.isRelative);
                t = vector4T;
                // TweenerCore<Vector4, Vector4, VectorOptions> vector4T;
                // DOGetter<Vector4> vector4Getter = plugData.Vector4Getter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     vector4T = DOTween.To(
                //         vector4Getter, plugData.Vector4Setter(target, clipElement.stringOption0, clipElement.intOption1),
                //         vector4Getter(), duration
                //     );
                //     break;
                // default:
                //     vector4T = DOTween.To(
                //         vector4Getter, plugData.Vector4Setter(target, clipElement.stringOption0, clipElement.intOption1),
                //         clipElement.isRelative ? vector4Getter() + clipElement.toVector4Val : clipElement.toVector4Val, duration
                //     );
                //     break;
                // }
                // vector4T.SetOptions(clipElement.axisConstraint, clipElement.boolOption0);
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     bool noAxisConstraint = clipElement.axisConstraint == AxisConstraint.None;
                //     Vector4 fromVector4ValFiltered = new Vector4(
                //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.X ? clipElement.fromVector4Val.x : 0,
                //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.Y ? clipElement.fromVector4Val.y : 0,
                //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.Z ? clipElement.fromVector4Val.z : 0,
                //         noAxisConstraint || clipElement.axisConstraint == AxisConstraint.W ? clipElement.fromVector4Val.w : 0
                //     );
                //     vector4T.From(clipElement.isRelative ? vector4Getter() + fromVector4ValFiltered : fromVector4ValFiltered);
                // }
                // t = vector4T;
                break;
            case DOTweenClipElement.PropertyType.Quaternion: //---------------------------------------------------------
                DOGetter<Quaternion> quaternionGetter = plugData.QuaternionGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<Quaternion, Vector3, QuaternionOptions> quaternionT = DOTween.To(
                    quaternionGetter, plugData.QuaternionSetter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromVector3Val : clipElement.toVector3Val, duration
                );
                if (directFrom) {
                    if (directToAndFrom) quaternionT.From(clipElement.fromVector3Val, true, clipElement.isRelative);
                    else quaternionT.From(true, clipElement.isRelative);
                }
                quaternionT.plugOptions.rotateMode = (RotateMode)clipElement.intOption0;
                quaternionT.SetRelative(clipElement.isRelative);
                t = quaternionT;
                // TweenerCore<Quaternion, Vector3, QuaternionOptions> quaternionT;
                // DOGetter<Quaternion> quaternionGetter = plugData.QuaternionGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     quaternionT = DOTween.To(
                //         quaternionGetter, plugData.QuaternionSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         quaternionGetter().eulerAngles, duration
                //     );
                //     break;
                // default:
                //     quaternionT = DOTween.To(
                //         quaternionGetter, plugData.QuaternionSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //         clipElement.isRelative ? quaternionGetter().eulerAngles + clipElement.toVector3Val : clipElement.toVector3Val, duration
                //     );
                //     break;
                // }
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     quaternionT.From(clipElement.isRelative ? quaternionGetter().eulerAngles + clipElement.fromVector3Val : clipElement.fromVector3Val);
                // }
                // quaternionT.plugOptions.rotateMode = (RotateMode)clipElement.intOption0;
                // t = quaternionT;
                break;
            case DOTweenClipElement.PropertyType.Color: //---------------------------------------------------------
                DOGetter<Color> colorGetter = plugData.ColorGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<Color, Color, ColorOptions> colorT;
                bool isAlphaOnly = clipElement.boolOption0;
                if (isAlphaOnly) {
                    colorT = DOTween.ToAlpha(
                        colorGetter, plugData.ColorSetter(target, clipElement.stringOption0, clipElement.intOption1),
                        directFromWDynamicTo ? clipElement.fromColorVal.a : clipElement.toColorVal.a, duration
                    );
                } else {
                    colorT = DOTween.To(
                        colorGetter, plugData.ColorSetter(target, clipElement.stringOption0, clipElement.intOption1),
                        directFromWDynamicTo ? clipElement.fromColorVal : clipElement.toColorVal, duration
                    );
                }
                if (directFrom) {
                    if (directToAndFrom) {
                        if (isAlphaOnly) colorT.From(clipElement.fromColorVal.a, true, clipElement.isRelative);
                        else colorT.From(clipElement.fromColorVal, true, clipElement.isRelative);
                    }
                    else colorT.From(true, clipElement.isRelative);
                }
                colorT.SetRelative(clipElement.isRelative);
                t = colorT;
                // TweenerCore<Color, Color, ColorOptions> colorT;
                // DOGetter<Color> colorGetter = plugData.ColorGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     colorT = clipElement.boolOption0
                //         ? DOTween.ToAlpha(
                //             colorGetter, plugData.ColorSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //             colorGetter().a, duration
                //         )
                //         : DOTween.To(
                //             colorGetter, plugData.ColorSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //             colorGetter(), duration
                //         );
                //     break;
                // default:
                //     colorT = clipElement.boolOption0
                //         ? DOTween.ToAlpha(
                //             colorGetter, plugData.ColorSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //             clipElement.isRelative ? colorGetter().a + clipElement.toColorVal.a : clipElement.toColorVal.a, duration
                //         )
                //         : DOTween.To(
                //             colorGetter, plugData.ColorSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //             clipElement.isRelative ? colorGetter() + clipElement.toColorVal : clipElement.toColorVal, duration
                //         );
                //     break;
                // }
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     if (clipElement.boolOption0) colorT.From(clipElement.isRelative ? colorGetter().a + clipElement.fromColorVal.a : clipElement.fromColorVal.a);
                //     else colorT.From(clipElement.isRelative ? colorGetter() + clipElement.fromColorVal : clipElement.fromColorVal);
                // }
                // t = colorT;
                break;
            case DOTweenClipElement.PropertyType.Rect: //---------------------------------------------------------
                DOGetter<Rect> rectGetter = plugData.RectGetter(target, clipElement.stringOption0, clipElement.intOption1);
                TweenerCore<Rect, Rect, RectOptions> rectT = DOTween.To(
                    rectGetter, plugData.RectSetter(target, clipElement.stringOption0, clipElement.intOption1),
                    directFromWDynamicTo ? clipElement.fromRectVal : clipElement.toRectVal, duration
                );
                if (directFrom) {
                    if (directToAndFrom) rectT.From(clipElement.fromRectVal, true, clipElement.isRelative);
                    else rectT.From(true, clipElement.isRelative);
                }
                rectT.SetRelative(clipElement.isRelative);
                t = rectT;
                // TweenerCore<Rect, Rect, RectOptions> rectT;
                // DOGetter<Rect> rectGetter = plugData.RectGetter(target, clipElement.stringOption0, clipElement.intOption1);
                // switch (clipElement.toType) {
                // case DOTweenClipElement.ToFromType.Dynamic:
                //     rectT = DOTween.To(
                //         rectGetter, plugData.RectSetter(target, clipElement.stringOption0, clipElement.intOption1), rectGetter(), duration
                //     );
                //     break;
                // default:
                //     rectT = DOTween.To(
                //             rectGetter, plugData.RectSetter(target, clipElement.stringOption0, clipElement.intOption1),
                //             clipElement.isRelative ? Add(rectGetter(), clipElement.toRectVal) : clipElement.toRectVal, duration
                //         );
                //     break;
                // }
                // if (clipElement.fromType == DOTweenClipElement.ToFromType.Direct) {
                //     rectT.From(clipElement.isRelative ? Add(rectGetter(), clipElement.fromRectVal) : clipElement.fromRectVal);
                // }
                // t = rectT;
                break;
            }
            if (t == null) return null;
            if (clipElement.ease == Ease.INTERNAL_Custom) t.SetEase(clipElement.easeCurve);
            else t.SetEase(clipElement.ease, clipElement.overshootOrAmplitude, clipElement.period);
            t.SetLoops(clipElement.loops, clipElement.loopType);
            if (DOTweenTimelineSettings.isApplicationPlaying) {
                if (clipElement.onComplete.GetPersistentEventCount() > 0) t.OnComplete(clipElement.onComplete.Invoke);
                if (clipElement.onStepComplete.GetPersistentEventCount() > 0) t.OnStepComplete(clipElement.onStepComplete.Invoke);
                if (clipElement.onUpdate.GetPersistentEventCount() > 0) t.OnUpdate(clipElement.onUpdate.Invoke);
            }
            return t;
        }

        public DOVisualTweenPlugin IsSupportedViaSubtype(string subtypeId)
        {
            isSupportedViaSubtype = true;
            this.subtypeId = subtypeId;
            return this;
        }

        #region EDITOR-ONLY
#if UNITY_EDITOR

        Dictionary<string, GUIContent> _editor_guidToGc; // Used to cache GUIContent values
        Dictionary<string, GUIContent> _editor_guidToGc_timeline; // Used to cache GUIContent values
        GUIContent[] _editor_gcs; // Used to cache GUIContent values - fallback for legacy plugin index usage
        GUIContent[] _editor_gcs_timeline; // Used to cache GUIContent values - fallback for legacy plugin index usage
        readonly GUIContent _editor_missingPlugin = new GUIContent("<color=#ff0000>UNSUPPORTED</color>");

        public GUIContent Editor_GetShortTypeAndAnimationNameGUIContent(DOTweenClipElement clipElement)
        {
            if (_editor_guidToGc == null) {
                _editor_guidToGc = new Dictionary<string, GUIContent>();
                _editor_gcs = new GUIContent[totPluginDatas];
            }
            GUIContent gc = GetGuiContent(clipElement, _editor_guidToGc, _editor_gcs);
            if (gc == null) {
                ITweenPluginData plugData = GetPlugData(clipElement);
                if (plugData == null) return _editor_missingPlugin; // This can happen if clipElement was created with a now disabled module in legacy mode
                gc = new GUIContent(Editor_GetShortTypeAndAnimationName(plugData, false));
                if (!string.IsNullOrEmpty(plugData.guid)) _editor_guidToGc.Add(plugData.guid, gc);
                _editor_gcs[clipElement.plugDataIndex] = gc;
            }
            return gc;
        }

        public GUIContent Editor_GetAnimationNameGUIContent(DOTweenClipElement clipElement)
        {
            if (_editor_gcs_timeline == null) {
                _editor_guidToGc_timeline = new Dictionary<string, GUIContent>();
                _editor_gcs_timeline = new GUIContent[totPluginDatas];
            }
            GUIContent gc = GetGuiContent(clipElement, _editor_guidToGc_timeline, _editor_gcs_timeline);
            if (gc == null) {
                ITweenPluginData plugData = GetPlugData(clipElement);
                if (plugData == null) return _editor_missingPlugin; // This can happen if clipElement was created with a now disabled module in legacy mode
                gc = new GUIContent(Editor_GetAnimationName(plugData, true));
                if (!string.IsNullOrEmpty(plugData.guid)) _editor_guidToGc_timeline.Add(plugData.guid, gc);
                _editor_gcs_timeline[clipElement.plugDataIndex] = gc;
            }
            return gc;
        }

        public string Editor_GetShortTypeName(bool richText = false)
        {
            string s = targetType.FullName;
            int dotIndex = s.LastIndexOf('.');
            if (dotIndex != -1) s = s.Substring(dotIndex + 1);
            if (richText) s = string.Format("<color=#68b3e2>{0}</color>", s);
            return s;
        }

        GUIContent GetGuiContent(DOTweenClipElement clipElement, Dictionary<string, GUIContent> guidToGcDict, GUIContent[] gcList)
        {
            return clipElement.plugDataGuid != null && guidToGcDict.TryGetValue(clipElement.plugDataGuid, out GUIContent gc)
                ? gc
                : clipElement.plugDataIndex < totPluginDatas ? gcList[clipElement.plugDataIndex] : null;
        }

        string Editor_GetShortTypeAndAnimationName(ITweenPluginData plugData, bool forTimeline)
        {
            return targetType == null
                ? string.Format("<color=#68b3e2>Global</color>→<color=#ffa047>{0}</color>", Editor_GetAnimationName(plugData, forTimeline))
                : string.Format("<color=#68b3e2>{0}</color>→<color=#ffa047>{1}</color>", Editor_GetShortTypeName(), Editor_GetAnimationName(plugData, forTimeline));
        }

        string Editor_GetAnimationName(ITweenPluginData plugData, bool forTimeline)
        {
            string label = plugData.label;
            int lastSlashIndex = label.LastIndexOf('/');
            if (lastSlashIndex == -1)
                return string.Format("<color=#ffa047>{0}</color>", label);
            return forTimeline
                ? string.Format("<color=#2e0020>{0}</color>→<color=#ffa047>{1}</color>",
                    label.Substring(0, lastSlashIndex), label.Substring(lastSlashIndex + 1))
                : string.Format("<color=#a8a5ff>{0}</color>→<color=#ffa047>{1}</color>",
                    label.Substring(0, lastSlashIndex), label.Substring(lastSlashIndex + 1));
        }

#endif
        #endregion

        #endregion

        #region Methods

        /// <summary>Adds one rect into another, and returns the resulting a</summary>
        static Rect Add(Rect a, Rect b)
        {
            if (b.xMin < a.xMin) a.xMin = b.xMin;
            if (b.xMax > a.xMax) a.xMax = b.xMax;
            if (b.yMin < a.yMin) a.yMin = b.yMin;
            if (b.yMax > a.yMax) a.yMax = b.yMax;
            return a;
        }

        #endregion
    }
}