using System.Globalization;
using System.Resources;
using System.Threading;
using Nuvers.Properties;

namespace Nuvers
{
    internal static class LocalizedResourceManager
    {
        private static readonly ResourceManager _resourceManager = new ResourceManager(typeof(Resources));

        public static string GetString(string resourceName) => _resourceManager.GetString(resourceName + '_' + GetLanguageName(), CultureInfo.InvariantCulture) ??
                                                               _resourceManager.GetString(resourceName, CultureInfo.InvariantCulture);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "the convention is to used lower case letter for language name.")]
        public static string GetLanguageName()
        {
            var culture = Thread.CurrentThread.CurrentUICulture;
            while (!culture.IsNeutralCulture)
            {
                if (culture.Parent == culture)
                {
                    break;
                }

                culture = culture.Parent;
            }

            return culture.ThreeLetterWindowsLanguageName.ToLowerInvariant();
        }
    }
}