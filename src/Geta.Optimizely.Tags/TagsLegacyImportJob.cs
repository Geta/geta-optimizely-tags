// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EPiServer.Data.Dynamic;
using EPiServer.Scheduler;
using Geta.Optimizely.Tags.Core;

namespace Geta.Optimizely.Tags
{
    /// <summary>
    /// Imports tags from the CMS 11 Geta.Tags package. Geta.Tags stored tags in the dynamic data store
    /// "Geta.Tags.Models.Tag"; this package uses <see cref="Tag.StoreName"/>, so the old tags are not visible after an
    /// upgrade until they are imported. The import merges into existing tags and can safely be run more than once.
    /// </summary>
    [ScheduledJob(
        GUID = "8B3A4E2C-5D7F-4E1A-9C6B-2F0D1E3A5B71",
        DisplayName = "Geta Tags: import tags from Geta.Tags (CMS 11)",
        Description = "Copies tags from the Geta.Tags (CMS 11) tag store into the Geta Optimizely Tags store. Run it once after upgrading, then run the Geta Tags maintenance job.",
        DefaultEnabled = false)]
    public class TagsLegacyImportJob : ScheduledJobBase
    {
        public const string LegacyStoreName = "Geta.Tags.Models.Tag";

        private readonly ITagRepository _tagRepository;
        private readonly DynamicDataStoreFactory _storeFactory;

        public TagsLegacyImportJob(ITagRepository tagRepository, DynamicDataStoreFactory storeFactory)
        {
            _tagRepository = tagRepository;
            _storeFactory = storeFactory;
        }

        public override string Execute()
        {
            var legacyStore = _storeFactory.GetStore(LegacyStoreName);
            if (legacyStore == null)
            {
                return $"No legacy tag store '{LegacyStoreName}' found, nothing to import";
            }

            var legacyTags = legacyStore.LoadAll<PropertyBag>().Select(ToTag);
            var count = Import(legacyTags, _tagRepository);

            return $"Imported {count} tag(s) from '{LegacyStoreName}'. Run the Geta Tags maintenance job to re-synchronize them with the content.";
        }

        /// <summary>
        /// Merges the tags into the repository: content links are added to an existing tag with the same name and
        /// group key, otherwise a new tag is created.
        /// </summary>
        internal static int Import(IEnumerable<Tag> legacyTags, ITagRepository tagRepository)
        {
            var count = 0;

            foreach (var legacyTag in legacyTags.Where(tag => !string.IsNullOrEmpty(tag.Name)))
            {
                var tag = tagRepository.GetTagByNameAndGroup(legacyTag.Name, legacyTag.GroupKey)
                          ?? new Tag
                          {
                              Name = legacyTag.Name,
                              GroupKey = legacyTag.GroupKey,
                              Slug = legacyTag.Slug,
                              Description = legacyTag.Description
                          };

                tag.PermanentLinks ??= new List<Guid>();
                foreach (var contentGuid in legacyTag.PermanentLinks.Except(tag.PermanentLinks).ToList())
                {
                    tag.PermanentLinks.Add(contentGuid);
                }

                tagRepository.Save(tag);
                count++;
            }

            return count;
        }

        internal static Tag ToTag(PropertyBag bag)
        {
            return new Tag
            {
                Name = bag.TryGetValue("Name", out var name) ? name as string : null,
                GroupKey = bag.TryGetValue("GroupKey", out var groupKey) ? groupKey as string ?? string.Empty : string.Empty,
                Slug = bag.TryGetValue("Slug", out var slug) ? slug as string : null,
                Description = bag.TryGetValue("Description", out var description) ? description as string : null,
                PermanentLinks = bag.TryGetValue("PermanentLinks", out var links) ? ToGuids(links) : new List<Guid>()
            };
        }

        private static IList<Guid> ToGuids(object value)
        {
            if (value is not IEnumerable items || value is string)
            {
                return new List<Guid>();
            }

            return items
                .Cast<object>()
                .Select(item => item switch
                {
                    Guid guid => guid,
                    string text when Guid.TryParse(text, out var parsed) => parsed,
                    _ => Guid.Empty
                })
                .Where(guid => guid != Guid.Empty)
                .Distinct()
                .ToList();
        }
    }
}
