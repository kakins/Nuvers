using NuGet.Versioning;

namespace Nuvers
{
    public static class SemanticVersionExtensions
    {
        public static SemanticVersion IncrementMajor(this SemanticVersion version)
        {
            return new SemanticVersion(version.Major + 1, 0, 0);
        }

        public static SemanticVersion IncrementMinor(this SemanticVersion version)
        {
            return new SemanticVersion(version.Major, version.Minor + 1, 0);
        }

        public static SemanticVersion IncrementPatch(this SemanticVersion version)
        {
            return new SemanticVersion(version.Major, version.Minor, version.Patch + 1);
        }
    }
}