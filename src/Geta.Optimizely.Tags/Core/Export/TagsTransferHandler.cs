// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using EPiServer.Core;
using EPiServer.Core.Transfer;

namespace Geta.Optimizely.Tags.Core.Export
{
    /// <summary>
    /// Carries the tags of exported content in the export package. On import, every tag is merged into the stored tag
    /// with the same name and group key (or created), and linked to the imported content, so the tags do not depend on
    /// the import publishing the content.
    /// </summary>
    public class TagsTransferHandler : TransferHandlerBase
    {
        // The meta data property that holds the content GUID of exported content (pages, blocks and media).
        private const string ContentGuidProperty = "PageGUID";

        private readonly ITagService _tagService;
        private readonly HashSet<Guid> _exportedContent = new();

        public TagsTransferHandler(ITagService tagService)
        {
            _tagService = tagService;
        }

        public override void ContentTransferring(RawContent content)
        {
            var contentGuid = content?.Property?.FirstOrDefault(property => property.Name == ContentGuidProperty)?.Value;
            if (Guid.TryParse(contentGuid, out var guid))
            {
                _exportedContent.Add(guid);
            }
        }

        public override void Write(Stream writer)
        {
            var tags = _exportedContent.Count == 0
                ? Enumerable.Empty<XElement>()
                : _tagService.GetAllTags()
                    .ToList()
                    .Where(tag => !string.IsNullOrEmpty(tag.Name) && tag.PermanentLinks != null)
                    .Select(tag => ToElement(tag, tag.PermanentLinks.Where(_exportedContent.Contains).Distinct().ToList()))
                    .Where(element => element != null);

            new XElement("tags", tags).Save(writer);
        }

        public override void Read(Stream reader)
        {
            Import(reader, TransferContentContext?.LinkGuidMap ?? new Dictionary<Guid, Guid>());
        }

        /// <summary>
        /// Adds the tags in the package to the tag store, linked to the imported content.
        /// </summary>
        /// <param name="reader">The tags written by <see cref="Write"/>.</param>
        /// <param name="linkGuidMap">Maps the GUIDs of the exported content to the GUIDs of the imported content.</param>
        /// <returns>The number of tags that were created or changed.</returns>
        internal int Import(Stream reader, IDictionary<Guid, Guid> linkGuidMap)
        {
            var changedTags = 0;

            foreach (var element in XElement.Load(reader).Elements("tag"))
            {
                var name = (string)element.Attribute("name");
                var links = element.Elements("link")
                    .Select(link => Guid.TryParse(link.Value, out var exported) && linkGuidMap.TryGetValue(exported, out var imported)
                        ? imported
                        : Guid.Empty)
                    .Where(link => link != Guid.Empty)
                    .ToList();

                if (string.IsNullOrEmpty(name) || links.Count == 0)
                {
                    continue;
                }

                var groupKey = (string)element.Attribute("groupKey") ?? string.Empty;
                var tag = _tagService.GetTagByNameAndGroup(name, groupKey) ?? new Tag { Name = name, GroupKey = groupKey };
                var existingLinks = tag.PermanentLinks ?? new List<Guid>();
                var missingLinks = links.Where(link => !existingLinks.Contains(link)).Distinct().ToList();
                var slug = (string)element.Attribute("slug");
                var description = (string)element.Element("description");

                if (missingLinks.Count == 0
                    && (slug == null || tag.Slug != null)
                    && (description == null || tag.Description != null))
                {
                    continue;
                }

                // Keep the slug and description of a tag that already exists.
                tag.Slug ??= slug;
                tag.Description ??= description;
                tag.PermanentLinks = existingLinks.Concat(missingLinks).ToList();
                _tagService.Save(tag);
                changedTags++;
            }

            return changedTags;
        }

        private static XElement ToElement(Tag tag, IList<Guid> links)
        {
            if (links.Count == 0)
            {
                return null;
            }

            return new XElement(
                "tag",
                new XAttribute("name", tag.Name),
                new XAttribute("groupKey", tag.GroupKey ?? string.Empty),
                tag.Slug != null ? new XAttribute("slug", tag.Slug) : null,
                tag.Description != null ? new XElement("description", tag.Description) : null,
                links.Select(link => new XElement("link", link)));
        }
    }
}
