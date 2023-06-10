using System;

namespace Shared {
    public class Logr {
        public static void Log(string e) {
            Console.WriteLine(e);
#if UNITY_2020_3_OR_NEWER
            UnityEngine.Debug.Log(e);
#endif
        }

        public static void LogException(Exception e, string? message = null) {
            var eMessage = (string.IsNullOrEmpty(message) ? "" : message + "\n") + e.Message + "\n" + e;
            Console.WriteLine(eMessage);
#if UNITY_2020_3_OR_NEWER
            UnityEngine.Debug.LogException(e);
#endif
        }
    }
}