using System;
using System.IO;
using LoneStoneStudio.Tools;
using MessagePack;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Shared;
#if !DEBUG
using System.Reflection;
#endif

namespace Server {
    public class Program {
        public static BuildVersion Version;

        public static void Main(string[] args) {
#if DEBUG
            Console.WriteLine("Running in DEBUG mode");
#else
            Console.WriteLine("Running in RELEASE mode");
#endif

            // Read environment variables (if any)
            DotEnv.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

            Version = SetupVersion();

            var option = MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray);
            MessagePackSerializer.DefaultOptions = option;
            Logr.Log("Server starting. Version " + Version, Tags.Server);
            var host = CreateHostBuilder(args).Build();
            host.Run();
        }

        private static BuildVersion SetupVersion() {
#if DEBUG
            var version = BuildVersion.GetCurrentFromGit();
            version.CommitHash = "dev";
            return version;
#else
            var assembly = Assembly.GetExecutingAssembly();
            var informationVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            return informationVersion == null ? new BuildVersion() : BuildVersion.FromString(informationVersion);
#endif
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder => {
                    webBuilder.UseStartup(_ => new Startup());
                });
    }
}