using System;

namespace Shared {
    public class Logr {
        public static void Log(string e) {
            Console.WriteLine(e);
        }

        public static void LogException(Exception e) {
            Console.WriteLine(e.Message + "\n" + e);
        }
    }
}