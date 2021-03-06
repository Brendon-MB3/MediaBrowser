﻿using MediaBrowser.Common.IO;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Providers.TV
{
    /// <summary>
    /// Class EpisodeProviderFromXml
    /// </summary>
    public class EpisodeProviderFromXml : BaseMetadataProvider
    {
        private readonly IItemRepository _itemRepo;
        private readonly IFileSystem _fileSystem;

        public EpisodeProviderFromXml(ILogManager logManager, IServerConfigurationManager configurationManager, IItemRepository itemRepo, IFileSystem fileSystem)
            : base(logManager, configurationManager)
        {
            _itemRepo = itemRepo;
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// Supportses the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        public override bool Supports(BaseItem item)
        {
            return item is Episode && item.LocationType == LocationType.FileSystem;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override MetadataProviderPriority Priority
        {
            get { return MetadataProviderPriority.First; }
        }

        /// <summary>
        /// Fetches metadata and returns true or false indicating if any work that requires persistence was done
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="force">if set to <c>true</c> [force].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{System.Boolean}.</returns>
        public override async Task<bool> FetchAsync(BaseItem item, bool force, BaseProviderInfo providerInfo, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadataFile = Path.Combine(item.MetaLocation, Path.ChangeExtension(Path.GetFileName(item.Path), ".xml"));

            var file = item.ResolveArgs.Parent.ResolveArgs.GetMetaFileByPath(metadataFile);

            if (file != null)
            {
                await XmlParsingResourcePool.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    await new EpisodeXmlParser(Logger, _itemRepo).FetchAsync((Episode)item, metadataFile, cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    XmlParsingResourcePool.Release();
                }
            }

            SetLastRefreshed(item, DateTime.UtcNow, providerInfo);
            return true;
        }

        /// <summary>
        /// Needses the refresh based on compare date.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="providerInfo">The provider info.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        protected override bool NeedsRefreshBasedOnCompareDate(BaseItem item, BaseProviderInfo providerInfo)
        {
            var metadataFile = Path.Combine(item.MetaLocation, Path.ChangeExtension(Path.GetFileName(item.Path), ".xml"));

            var file = item.ResolveArgs.Parent.ResolveArgs.GetMetaFileByPath(metadataFile);

            if (file == null)
            {
                return false;
            }

            return _fileSystem.GetLastWriteTimeUtc(file) > item.DateLastSaved;
        }
    }
}
