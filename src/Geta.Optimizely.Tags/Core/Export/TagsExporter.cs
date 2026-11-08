using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Core;
using EPiServer.Core.Transfer;
using EPiServer.Enterprise;
using EPiServer.Enterprise.Transfer;
using EPiServer.Web;

namespace Geta.Optimizely.Tags.Core.Export
{
    public class TagsExporter
    {
        private readonly ITagRepository _tagRepository;
        private readonly IPermanentLinkMapper _permanentLinkMapper;

        public TagsExporter(ITagRepository tagRepository, IPermanentLinkMapper permanentLinkMapper)
        {
            _tagRepository = tagRepository;
            _permanentLinkMapper = permanentLinkMapper;
        }

        /// <summary>
        /// Adds every tag in the tag store to the export package.
        /// </summary>
        public virtual void AddTagsToExport(ITransferContext transferContext)
        {
            AddToExport(transferContext, () => _tagRepository.GetAllTags());
        }

        /// <summary>
        /// Adds the tags that are linked to the exported content item to the export package.
        /// </summary>
        public virtual void AddTagsToExport(ITransferContext transferContext, ContentReference contentLink)
        {
            AddToExport(transferContext, () =>
            {
                var contentGuid = _permanentLinkMapper.Find(contentLink)?.Guid;
                return contentGuid.HasValue
                    ? _tagRepository.GetTagsByContent(contentGuid.Value)
                    : Enumerable.Empty<Tag>();
            });
        }

        private static void AddToExport(ITransferContext transferContext, Func<IEnumerable<Tag>> getTags)
        {
            if (transferContext is not ITransferHandlerContext { TransferType: TypeOfTransfer.Exporting } exporter)
            {
                return;
            }

            var ddsHandler = exporter.TransferHandlers.OfType<DynamicDataTransferHandler>().FirstOrDefault();
            if (ddsHandler == null)
            {
                return;
            }

            // A DDS export entry is a single stored object (identity + store name), so each tag is
            // added individually. The handler de-duplicates entries across exported content items.
            foreach (var tag in getTags())
            {
                if (tag?.Id != null)
                {
                    ddsHandler.AddToExport(tag.Id.ExternalId, Tag.StoreName);
                }
            }
        }
    }
}
