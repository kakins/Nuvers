using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Resources = NuGet.Versioning.Resources;

namespace Nuvers
{

    [Command(typeof(Resources), "version", "IncrementVersionCommandDescription")]
    public class IncrementVersionCommand : Command
    {
        private static string _packageSourcePath = "K:\\ESI\\Staff\\Kris\\packages";
        private static string _projectPath = "C:\\ESI\\EsiServices\\Services_DEV_WD\\EsiServices.WebApi.Core";
        private static string _projectFileName = "EsiServices.WebApi.Core.csproj";
        private static string _projectFilePath = $"{_projectPath}\\{_projectFileName}";
        private static string _assemblyInfoFilePath = $"{_projectPath}\\Properties\\AssemblyInfo.cs";

        public override void ExecuteCommand()
        {
            ReadInput();
        }

        private void ReadInput()
        {

            // todo: if project path is empty, get the current directory
            string input = string.Empty;

            if (string.IsNullOrEmpty(input))
            {
                _projectFilePath = Directory.GetFiles(_projectPath, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
                input = _projectFilePath;
            }

            string assemblyName = GetAssemblyName(input);

            if (string.IsNullOrEmpty(assemblyName))
            {
                Console.WriteLine("Error loading .csproj");
                return;
            }

            Console.WriteLine(assemblyName);

            Task.Run(() => IncrementVersion(assemblyName, "major")).GetAwaiter().GetResult();
        }

        private static string GetAssemblyName(string path)
        {
            try
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.Load(path);
                var xmlNamespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);

                if (xmlDoc.DocumentElement == null)
                {
                    return string.Empty;
                }

                xmlNamespaceManager.AddNamespace("x", xmlDoc.DocumentElement.NamespaceURI);

                var assemblyNameNode = xmlDoc.SelectSingleNode("//x:AssemblyName", xmlNamespaceManager);

                if (assemblyNameNode == null)
                    return string.Empty;

                return assemblyNameNode.InnerText;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task IncrementVersion(string package, string versionType)
        {
            NuGetVersion latestVersion = await GetLatestPackageVersion(package);

            // if we don't find the nuget version, log and quit

            int major = latestVersion.Major;
            int minor = latestVersion.Minor;
            int patch = latestVersion.Patch;

            // increment AssemblyInfo version based on major, minor, patch
            switch (versionType.ToLower())
            {
                case "major":
                    major += 1;
                    minor = 0;
                    patch = 0;
                    break;
                case "minor":
                    minor += 1;
                    patch = 0;
                    break;
                case "patch":
                    patch += 1;
                    break;
            }

            var newVersion = $"{major}.{minor}.{patch}";

            WriteToAssemblyInfo(newVersion);

            Console.WriteLine($"Lateset version: {latestVersion}");
        }

        public void WriteToAssemblyInfo(string newVersion)
        {
            if (!File.Exists(_assemblyInfoFilePath))
            {
                System.Console.WriteLine("Could not find AssemblyInfo.cs for project");
                return;
            }

            Console.WriteLine("Replacing version...");

            File.WriteAllText(_assemblyInfoFilePath,
                Regex.Replace(
                    File.ReadAllText(_assemblyInfoFilePath),
                    @"(\[assembly: AssemblyInformationalVersion\(""[0-9].*""\)\])",
                    $"[assembly: AssemblyInformationalVersion(\"{newVersion}\")]")
            );
        }

        private static async Task<NuGetVersion> GetLatestPackageVersion(string packageName)
        {
            var logger = new Logger();
            var providers = new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3());
            //var packageSource = new PackageSource("https://api.nuget.org/v3/index.json");
            var packageSource = new PackageSource(_packageSourcePath);
            var sourceRepository = new SourceRepository(packageSource, providers);

            var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
            var filter = new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion);
            IEnumerable<IPackageSearchMetadata> searchMetadata = await searchResource.SearchAsync(packageName, filter, 0, 10, logger, CancellationToken.None);

            return searchMetadata.First().Identity.Version;
        }
    }
}