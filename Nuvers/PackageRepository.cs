using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NuGet.Common;
using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;

namespace Nuvers
{
    public class PackageRepository
    {
        private readonly ILogger _logger;

        public PackageRepository(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<NuGetVersion> GetLatestVersionById(string packageId)
        {
            var providers = new List<Lazy<INuGetResourceProvider>>(Repository.Provider.GetCoreV3());
            string packageSourcePath = GetPackageSourcePath();
            var packageSource = new PackageSource(packageSourcePath);
            var sourceRepository = new SourceRepository(packageSource, providers);

            var searchResource = await sourceRepository.GetResourceAsync<PackageSearchResource>();
            var filter = new SearchFilter(true, SearchFilterType.IsAbsoluteLatestVersion);
            IEnumerable<IPackageSearchMetadata> searchMetadata = 
                await searchResource.SearchAsync(packageId, filter, 0, 10, _logger, CancellationToken.None);

            IPackageSearchMetadata package = searchMetadata.First();

            if (package == null)
            {
                throw new Exception($"{packageId} not found in NuGet repository location {packageSourcePath}");
            }

            return package.Identity.Version;
        }

        private string GetPackageSourcePath()
        {
            var configPackageSourcePath = ConfigurationManager.AppSettings["NuversPackageSourcePath"];
            if (configPackageSourcePath.Any())
                return configPackageSourcePath;

            return "https://api.nuget.org/v3/index.json";
        }
    }
}