using System;
using System.Configuration;
using System.IO;

namespace Nuvers.Test
{
    public static class Util
    {
        public static string GetNuversExePath()
        {
            return NuverseExePath.Value;
        }

        private static readonly Lazy<string> NuverseExePath = new Lazy<string>(GetNuversExePathCore);

        private static string GetNuversExePathCore()
        {
            var targetDir = 
                ConfigurationManager.AppSettings["TestTargetDir"] ?? 
                Directory.GetCurrentDirectory();
            var nugetexe = Path.Combine(targetDir, "Nuvers.exe");
            return nugetexe;
        }
    }
}