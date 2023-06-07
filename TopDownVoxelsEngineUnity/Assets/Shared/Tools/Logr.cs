using System;

namespace Shared {
    public class Logr {
        public static void Log(string e) {
            Console.WriteLine(e);
        }

        public static void LogException(Exception e, string? message = null) {
            Console.WriteLine((string.IsNullOrEmpty(message) ? "" : message + "\n") + e.Message + "\n" + e);
        }
    }
}