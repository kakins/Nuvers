using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using NuGet.Common;
using NuGet.Versioning;

namespace Nuvers
{
    public class CsProjHelper
    {
        private readonly ILogger _logger;
        private static string _assemblyInfoRegex = @"(\[assembly: AssemblyInformationalVersion\(""[0-9].*""\)\])";

        // matches something like 1.1.1
        // " indicates the end of the version sequence
        private static string _versionRegex = @"((\d+\.\d+\.\d+)(?=\""))";

        private readonly string _projectFilePath;
        private readonly string _packageName;

        public CsProjHelper(ILogger logger)
        {
            _logger = logger;
        }

        public CsProjHelper(ILogger logger, string startPath)
        {
            _logger = logger;
            _projectFilePath = GetPath(startPath);
            _packageName = GetAssemblyName(_projectFilePath);
        }

        public static string GetAssemblyName(string projectPath)
        {
            var xmlDoc = new XmlDocument();
            xmlDoc.Load(projectPath);
            var xmlNamespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);

            if (xmlDoc.DocumentElement == null)
                throw new XmlException("Error reading .csproj");

            xmlNamespaceManager.AddNamespace("x", xmlDoc.DocumentElement.NamespaceURI);

            XmlNode assemblyNameNode = xmlDoc.SelectSingleNode("//x:AssemblyName", xmlNamespaceManager);

            if (assemblyNameNode == null)
                throw new XmlException("AssemblyName not found in .csproj");

            return assemblyNameNode.InnerText;
        }

        public async Task<NuGetVersion> GetLatestNuGetVersion()
        {
            return await new PackageRepository(_logger).GetLatestVersionById(_packageName);
        }

        public static string GetPath(string startPath)
        {
            if (string.IsNullOrEmpty(startPath))
            {
                // get default project path
                var configProjectPath = ConfigurationManager.AppSettings["NuversProjectPath"];

                if (configProjectPath.Any())
                    return configProjectPath;

                // get csproj in current directory path
                return GetPathInCurrentDirectory();
            }

            if (!startPath.Contains(".csproj"))
                throw new CommandLineException("Specify a .csproj file");

            if (startPath.Contains("\\"))
                return startPath;

            return $"{Directory.GetCurrentDirectory()}\\{startPath}";
        }

        private static string GetPathInCurrentDirectory()
        {
            string[] projectFiles = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.csproj", SearchOption.TopDirectoryOnly);

            if (!projectFiles.Any())
                throw new CommandLineException("No .csproj files found in this directory.");

            if (projectFiles.Length > 1)
                throw new CommandLineException(
                    "More than one .csproj file found in this directory.  Please specify which project you want to version.");

            return $"{Directory.GetCurrentDirectory()}\\{projectFiles.First()}";
        }

        public void UpdateAssemblyVersion(string newVersion)
        {
            var assemblyInfoFilePath = $"{Path.GetDirectoryName(_projectFilePath)}\\Properties\\AssemblyInfo.cs";

            if (!File.Exists(assemblyInfoFilePath))
            {
                _logger.LogError($"Could not find file {assemblyInfoFilePath}");
                return;
            }

            _logger.LogInformation($"Incrementing version for {assemblyInfoFilePath}");

            File.WriteAllText(assemblyInfoFilePath,
                Regex.Replace(
                    File.ReadAllText(assemblyInfoFilePath),
                    _assemblyInfoRegex,
                    $"[assembly: AssemblyInformationalVersion(\"{newVersion}\")]")
            );
        }

        public SemanticVersion GetAssemblyVersion()
        {
            var assemblyInfoFilePath = $"{Path.GetDirectoryName(_projectFilePath)}\\Properties\\AssemblyInfo.cs";

            var contents = File.ReadAllText(assemblyInfoFilePath);

            var assemblyInfoDeclaration = Regex.Match(contents, _assemblyInfoRegex);

            if (!assemblyInfoDeclaration.Success)
                throw new CommandLineException("Could not find assembly info version");

            // todo: match "-pre", "rc", etc at end of version
            var assemblyInfo = Regex.Match(assemblyInfoDeclaration.Value, _versionRegex);

            if (!assemblyInfo.Success)
                throw new CommandLineException(
                    "Assembly info declaration found with an invlid assembly version format.");

            return SemanticVersion.Parse(assemblyInfo.Value);
        }
    }
}