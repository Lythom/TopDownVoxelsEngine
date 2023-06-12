using System;
using System.Text.RegularExpressions;
using MessagePack;

namespace LoneStoneStudio.Tools {
    [Serializable, MessagePackObject]
    public struct BuildVersion {
        [Key(0)]
        public int Major;

        [Key(1)]
        public int Minor;

        [Key(2)]
        public int Patch;

        [Key(3)]
        public string CommitHash;

        public override string ToString() {
            return $"{Major}.{Minor}.{Patch}.{CommitHash}";
        }

        public string ToStringWithoutHash() {
            return $"{Major}.{Minor}.{Patch}";
        }

        public string ToUnderscoreString() {
            return $"{Major}_{Minor}_{Patch}_{CommitHash}";
        }

        // Expects a version in the Major.Minor.Patch.CommitHash or Major.Minor.Patch-CommitHash format
        public static BuildVersion FromString(string targetVersion) {
            BuildVersion buildVersion;
            var strings = targetVersion.Replace("-", ".").Split('.');

            buildVersion.Major = int.Parse(strings[0]);
            buildVersion.Minor = int.Parse(strings[1]);
            buildVersion.Patch = int.Parse(strings[2]);
            buildVersion.CommitHash = strings.Length > 3 ? strings[3] : "";
            return buildVersion;
        }

        public bool IsSuperior(BuildVersion otherVersion) => (
            Major > otherVersion.Major ||
            (Major == otherVersion.Major && Minor > otherVersion.Minor) ||
            (Major == otherVersion.Major && Minor == otherVersion.Minor && Patch >= otherVersion.Patch)
        );

        /// <summary>
        /// Retrieves the build version from git based on the most recent matching tag and
        /// commit history. This returns the version as: {major.minor.build} where 'build'
        /// represents the nth commit after the tagged commit.
        /// </summary>
        public static BuildVersion GetCurrentFromGit() {
            var version = Git.Run(@"describe --tags --long --match ""v[0-9]*""");
            version = version.Replace('-', '.');

            // Remove initial 'v'
            version = version.Substring(1);

            BuildVersion buildVersion;
            var strings = version.Split('.');

            buildVersion.Major = int.Parse(strings[0]);
            buildVersion.Minor = int.Parse(strings[1]);
            buildVersion.Patch = int.Parse(strings[2]);
            // We remove the "g" in front of the commit
            buildVersion.CommitHash = Regex.Replace(strings[3].Substring(1), @"\t|\n|\r", "");

            return buildVersion;
        }

        public string AsDotNetString() {
            return $"{Major}.{Minor}.{Patch}-{CommitHash}";
        }
    }
}