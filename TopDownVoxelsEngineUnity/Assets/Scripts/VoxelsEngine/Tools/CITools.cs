using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace LoneStoneStudio.Tools {
    /// <summary>
    ///     Helfull class containing some methods such as AddChild
    /// </summary>
    public static class CITools {
        public static bool SmartActive(this Component c, bool active) {
            SmartActive(c.gameObject, active);
            return active;
        }

        public static void SmartActive(this GameObject go, bool active) {
            if (go.activeSelf != active) go.SetActive(active);
        }

        public static void SmartColor(this Image i, Color c) {
            if (i.color != c) i.color = c;
        }

        public static void SmartAlpha(this CanvasGroup i, float a) {
            if (i.alpha != a) i.alpha = a;
        }

        public static void SetLayerRecursively(this GameObject obj, int layer) {
            obj.layer = layer;
            foreach (Transform child in obj.transform) {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        #region Transform extensions

        public static void DestroyChildren(this Transform transform) {
#if UNITY_EDITOR
            if (!Application.isPlaying) {
                transform.DestroyChildrenImmediate();
                return;
            }
#endif
            //Add children to list before destroying
            //otherwise GetChild(i) may bomb out
            var children = new List<Transform>();

            for (var i = 0; i < transform.childCount; i++) {
                var child = transform.GetChild(i);
                children.Add(child);
            }

            foreach (var child in children) {
                Object.Destroy(child.gameObject);
            }
        }

        public static void DestroyChildrenImmediate(this Transform transform) {
            //Add children to list before destroying
            //otherwise GetChild(i) may bomb out
            var children = new List<Transform>();

            for (var i = 0; i < transform.childCount; i++) {
                var child = transform.GetChild(i);
                children.Add(child);
            }

            foreach (var child in children) {
                Object.DestroyImmediate(child.gameObject);
            }
        }

        public static Vector3 WithY(this Vector3 v, float y) {
            v.y = y;
            return v;
        }

        #endregion


        #region Enum extensions

        /// <summary>
        ///     Return the list of enums
        /// </summary>
        /// <typeparam name="T">Enum type</typeparam>
        /// <returns></returns>
        public static List<T> GetListFromEnum<T>() {
            Array enums = Enum.GetValues(typeof(T));
            return enums.Cast<T>().ToList();
        }

        #endregion

        #region String extensions

        /// <summary>
        ///     From Sirenix Utilities
        ///     Returns true if this string is null, empty, or contains only whitespace.
        /// </summary>
        /// <param name="str">The string to check.</param>
        /// <returns><c>true</c> if this string is null, empty, or contains only whitespace; otherwise, <c>false</c>.</returns>
        public static bool IsNullOrWhitespace(this string str) {
            if (!string.IsNullOrEmpty(str)) {
                for (int index = 0; index < str.Length; ++index) {
                    if (!char.IsWhiteSpace(str[index]))
                        return false;
                }
            }

            return true;
        }

        #endregion

        #region UI Extensions

        /// <summary>
        ///     From Sirenix Utilities
        ///     Returns a Rect that has been expanded by the specified amount.
        /// </summary>
        /// <param name="rect">The original Rect.</param>
        /// <param name="expand">The desired expansion.</param>
        public static Rect Expand(this Rect rect, float expand) {
            rect.x -= expand;
            rect.y -= expand;
            rect.height += expand * 2f;
            rect.width += expand * 2f;
            return rect;
        }

        /// <summary>
        ///     From Sirenix Utilities
        ///     Returns a Rect that has been expanded by the specified amount.
        /// </summary>
        /// <param name="rect">The original Rect.</param>
        /// <param name="horizontal">The desired expansion on the X-axis.</param>
        /// <param name="vertical">The desired expansion on the Y-axis.</param>
        public static Rect Expand(this Rect rect, float horizontal, float vertical) {
            rect.position -= new Vector2(horizontal, vertical);
            rect.size += new Vector2(horizontal, vertical) * 2f;
            return rect;
        }

        public static Rect GetAnchoredRect(this RectTransform rectTransform) {
            var sizeDelta = rectTransform.sizeDelta;
            return new Rect(
                rectTransform.anchoredPosition.x - sizeDelta.x * rectTransform.pivot.x,
                rectTransform.anchoredPosition.y - sizeDelta.y * rectTransform.pivot.y,
                sizeDelta.x,
                sizeDelta.y
            );
        }

        public static Vector2 WorldToUISpace(this Canvas parentCanvas, Vector3 worldPos, Camera camera) {
            //Convert the world for screen point so that it can be used with ScreenPointToLocalPointInRectangle function
            Vector3 screenPos = camera.WorldToScreenPoint(worldPos);
            Vector2 movePos;

            //Convert the screenpoint to ui rectangle local point
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentCanvas.transform as RectTransform, screenPos, parentCanvas.worldCamera, out movePos);

            return movePos;
        }

        #endregion
    }
}