using System.IO;
using System.Threading.Tasks;
using NuGet.Versioning;
using Resources = NuGet.Versioning.Resources;

namespace Nuvers
{

    [Command(typeof(Resources), "version", "IncrementVersionCommandDescription", MaxArgs = 2)]
    public class IncrementVersionCommand : Command
    {
        [Option(typeof(Resources), "")]
        public string Project { get; set; }

        // nuvers version minor
        // nuvers version minor -project myproj.csproj
        // nuvers version minor -project c:\dir\myproj.csproj

        public override void ExecuteCommand()
        {
            Task.Run(IncrementVersion).GetAwaiter().GetResult();
        }

        public async Task IncrementVersion()
        {
            CsProjHelper csProjHelper = new CsProjHelper(Console, Project);

            NuGetVersion latestNugetVersion = await csProjHelper.GetLatestNuGetVersion();

            SemanticVersion assemblyVersion = csProjHelper.GetAssemblyVersion();

            string versionType = Arguments[0];

            // increment AssemblyInfo version based on major, minor, patch
            switch (versionType.ToLower())
            {
                case "major":
                    assemblyVersion = assemblyVersion.IncrementMajor();
                    break;
                case "minor":
                    assemblyVersion = assemblyVersion.IncrementMinor();
                    break;
                case "patch":
                    assemblyVersion = assemblyVersion.IncrementPatch();
                    break;
            }

            csProjHelper.UpdateAssemblyVersion($"{assemblyVersion.Major}.{assemblyVersion.Minor}.{assemblyVersion.Patch}");

            Console.WriteLine($"Updated AssemblyInfo version: {assemblyVersion}");
        }

        private void ValidateVersionUpdate(string packageName, SemanticVersion assemblyVersion, NuGetVersion nugetVersion)
        {
            var comparison = VersionComparer.Compare(assemblyVersion, nugetVersion,
                VersionComparison.Version);

            if (comparison != 0)
            {
                throw new CommandLineException(
                    $"The latest published NuGet version for {packageName} is {nugetVersion}.  Your version is {assemblyVersion}.  Please ensure your AssemblyInfo.cs specifies the latest published NuGet version before incrementing the version.");
            }

        }
    }
}