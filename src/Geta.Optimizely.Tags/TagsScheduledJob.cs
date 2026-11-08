// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using EPiServer;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using Geta.Optimizely.Tags.Core;

namespace Geta.Optimizely.Tags
{
    /// <summary>
    /// Re-synchronizes the tag store with the tag properties of the content it references: every content item linked
    /// from a tag is re-read (in all of its languages) and its links are added to or removed from tags so that they match
    /// the tags the content actually has. Deleted content is unlinked from all tags. The job never changes content.
    /// </summary>
    [ScheduledPlugIn(
        DisplayName = "Geta Tags maintenance",
        Description = "Synchronizes the tag store with the tags used by content: removes links to deleted content and to tags that are no longer used by the content.",
        DefaultEnabled = true)]
    public class TagsScheduledJob : ScheduledJobBase
    {
        private bool _stop;

        private readonly ITagService _tagService;
        private readonly IContentTypeRepository _contentTypeRepository;
        private readonly IContentLoader _contentLoader;

        public TagsScheduledJob(
            IContentTypeRepository contentTypeRepository,
            ITagService tagService,
            IContentLoader contentLoader)
        {
            _contentTypeRepository = contentTypeRepository;
            _tagService = tagService;
            _contentLoader = contentLoader;
            IsStoppable = true;
        }

        public override string Execute()
        {
            _stop = false;

            // All changes are made to this single set of tag instances and saved once at the end, so a tag that is
            // touched for several content items is never overwritten with a stale copy.
            var tags = _tagService.GetAllTags().ToList();
            var tagsByKey = tags
                .Where(tag => tag?.Name != null)
                .GroupBy(tag => TagKey(tag.GroupKey, tag.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var tagsByContent = tags
                .Where(tag => tag?.PermanentLinks != null)
                .SelectMany(tag => tag.PermanentLinks.Distinct().Select(contentGuid => (contentGuid, tag)))
                .ToLookup(link => link.contentGuid, link => link.tag);

            var changedTags = new HashSet<Tag>(ReferenceEqualityComparer.Instance);

            foreach (var linkedTags in tagsByContent)
            {
                if (_stop)
                {
                    SaveTags(changedTags);
                    return $"Geta Tags maintenance was stopped after updating {changedTags.Count} tag(s)";
                }

                var contentGuid = linkedTags.Key;
                var expectedTags = GetExpectedTags(contentGuid, tagsByKey);

                foreach (var tag in linkedTags.Where(tag => !expectedTags.Contains(tag)))
                {
                    tag.PermanentLinks.Remove(contentGuid);
                    changedTags.Add(tag);
                }

                foreach (var tag in expectedTags.Except(linkedTags))
                {
                    tag.PermanentLinks ??= new List<Guid>();
                    tag.PermanentLinks.Add(contentGuid);
                    changedTags.Add(tag);
                }
            }

            SaveTags(changedTags);

            return $"Geta Tags maintenance completed: checked {tagsByContent.Count} content item(s), updated {changedTags.Count} tag(s)";
        }

        /// <summary>
        /// Returns the tags the content should be linked to, based on the tag properties of all its language versions.
        /// Tags that the content uses but that are missing from the tag store (for example deleted in the admin UI) are
        /// created, the same way publishing the content would.
        /// </summary>
        private HashSet<Tag> GetExpectedTags(Guid contentGuid, Dictionary<string, List<Tag>> tagsByKey)
        {
            var expectedTags = new HashSet<Tag>(ReferenceEqualityComparer.Instance);
            var content = LoadContent(contentGuid);

            if (content == null || content.IsDeleted)
            {
                return expectedTags;
            }

            var contentType = _contentTypeRepository.Load(content.ContentTypeID);

            foreach (var version in GetLanguageVersions(content))
            {
                foreach (var (groupKey, names) in ContentTags.GetTagsByGroupKey(version, contentType))
                {
                    foreach (var name in names)
                    {
                        var key = TagKey(groupKey, name);
                        if (!tagsByKey.TryGetValue(key, out var matchingTags))
                        {
                            matchingTags = new List<Tag> { new() { Name = name, GroupKey = groupKey } };
                            tagsByKey.Add(key, matchingTags);
                        }

                        expectedTags.UnionWith(matchingTags);
                    }
                }
            }

            return expectedTags;
        }

        private IContent LoadContent(Guid contentGuid)
        {
            return _contentLoader.TryGet<IContent>(contentGuid, out var content) ? content : null;
        }

        private IEnumerable<IContent> GetLanguageVersions(IContent content)
        {
            if (content is not ILocalizable localizable)
            {
                return new[] { content };
            }

            return localizable.ExistingLanguages
                .Select(language => _contentLoader.TryGet<IContent>(content.ContentGuid, language, out var version) ? version : null)
                .Where(version => version != null);
        }

        private void SaveTags(IEnumerable<Tag> tags)
        {
            foreach (var tag in tags)
            {
                _tagService.Save(tag);
            }
        }

        private static string TagKey(string groupKey, string name) => $"{groupKey}\n{name}";

        public override void Stop()
        {
            _stop = true;
        }
    }
}
