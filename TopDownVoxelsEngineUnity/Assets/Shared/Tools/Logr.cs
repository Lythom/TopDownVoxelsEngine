using System;

#if !UNITY_2020_3_OR_NEWER
using Serilog;
using HideInCallstackAttribute = Shared.UnityCompat.HideInCallstackAttribute;
#else
using HideInCallstackAttribute = UnityEngine.HideInCallstackAttribute;
#endif

namespace Shared {
    public static class Tags {
        public const string Debug = "Debug";
        public const string Client = "Client";
        public const string Standalone = "Standalone";
        public const string Server = "Server";
    }

    public static class Logr {
        static Logr() {
#if !UNITY_2020_3_OR_NEWER
            Serilog.Log.Logger = new Serilog.LoggerConfiguration()
                .WriteTo.Console()
                .WriteTo.Debug()
                .CreateLogger();
#endif
        }

        [HideInCallstack]
        public static void Log(string e, string tag = "general") {
            var value = $"[{tag}] {e}";

#if UNITY_2020_3_OR_NEWER
            UnityEngine.Debug.Log(value);
#else
            Serilog.Log.Information(value);
#endif
        }

        [HideInCallstack]
        public static void LogException(Exception e, string? message = null, string tag = "general") {
            var eMessage = $"[{tag}] {(string.IsNullOrEmpty(message) ? "" : message + "\n")}{e.Message}\n{e}";
#if UNITY_2020_3_OR_NEWER
            UnityEngine.Debug.Log(eMessage);
            UnityEngine.Debug.LogException(e);
#else
            Serilog.Log.Information(eMessage);
            Console.WriteLine(eMessage);
#endif
        }

        [HideInCallstack]
        public static void LogError(string? message = null, string tag = "general") {
            var eMessage = $"[{tag}] {message}";
#if UNITY_2020_3_OR_NEWER
            UnityEngine.Debug.Log(eMessage);
#else
            Serilog.Log.Error(eMessage);
            Console.WriteLine(eMessage);
#endif
        }
    }
}