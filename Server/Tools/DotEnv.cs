using System;
using System.IO;

namespace Server {
    public static class DotEnv {
        public static bool Loaded = false;

        public static void Load(string filePath) {
            if (!File.Exists(filePath))
                return;

            foreach (var line in File.ReadAllLines(filePath)) {
                var idx = line.IndexOf('=');
                if (idx == -1 || idx >= line.Length - 1)
                    continue;

                var variable = line.Substring(0, idx);
                var value = line.Substring(idx + 1, line.Length - idx - 1);
                Environment.SetEnvironmentVariable(variable, value);
            }

            Loaded = true;
        }
    }
}