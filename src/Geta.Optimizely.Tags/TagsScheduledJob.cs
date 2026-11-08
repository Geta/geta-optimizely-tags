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
        Description = "Synchronizes the tag store with the tags used by content: adds missing links, re-creates tags that content still uses, and removes links to deleted content and to tags that are no longer used by the content.",
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

            // The links are worked out against this single snapshot of the tags. Only the links that change are saved,
            // regularly and to a fresh copy of each tag, so links that publishing changes while the job runs are kept.
            var tags = _tagService.GetAllTags().Where(tag => !string.IsNullOrEmpty(tag.Name)).ToList();
            var tagsByKey = tags
                .GroupBy(tag => TagKey(tag.GroupKey, tag.Name), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            var tagsByContent = tags
                .Where(tag => tag.PermanentLinks != null)
                .SelectMany(tag => tag.PermanentLinks.Distinct().Select(contentGuid => (contentGuid, tag)))
                .ToLookup(link => link.contentGuid, link => link.tag);

            var changes = new Dictionary<Tag, LinkChanges>(ReferenceEqualityComparer.Instance);
            var checkedContent = 0;
            var changedLinks = 0;

            try
            {
                foreach (var linkedTags in tagsByContent)
                {
                    if (_stop)
                    {
                        return $"Geta Tags maintenance was stopped after checking {checkedContent} content item(s)";
                    }

                    var contentGuid = linkedTags.Key;
                    var expectedTags = GetExpectedTags(contentGuid, tagsByKey);

                    foreach (var tag in linkedTags.Where(tag => !expectedTags.Contains(tag)))
                    {
                        GetChanges(changes, tag).Removed.Add(contentGuid);
                    }

                    // Tag.Equals only compares names, so compare instances: the same name can be used in several groups.
                    foreach (var tag in expectedTags.Except<Tag>(linkedTags, ReferenceEqualityComparer.Instance))
                    {
                        GetChanges(changes, tag).Added.Add(contentGuid);
                    }

                    // Save regularly rather than once at the end, so a change worked out for a content item cannot undo
                    // a later publish of that content item for long.
                    if (++checkedContent % SaveInterval == 0)
                    {
                        changedLinks += SaveChanges(changes);
                    }
                }
            }
            finally
            {
                // Keep the progress made so far, also when the job is stopped or fails on a content item.
                changedLinks += SaveChanges(changes);
            }

            return $"Geta Tags maintenance completed: checked {checkedContent} content item(s), changed {changedLinks} link(s)";
        }

        /// <summary>
        /// The number of content items to check between saving the link changes.
        /// </summary>
        internal int SaveInterval { get; set; } = 100;

        /// <summary>
        /// Returns the tags the content should be linked to, based on the tag properties of all its language versions.
        /// Tags that the content uses but that are missing from the tag store (for example deleted in the admin UI) are
        /// created, the same way publishing the content would.
        /// </summary>
        private HashSet<Tag> GetExpectedTags(Guid contentGuid, Dictionary<string, List<Tag>> tagsByKey)
        {
            var expectedTags = new HashSet<Tag>(ReferenceEqualityComparer.Instance);
            if (!_contentLoader.TryGet<IContent>(contentGuid, out var content) || content.IsDeleted)
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

        private static LinkChanges GetChanges(Dictionary<Tag, LinkChanges> changes, Tag tag)
        {
            if (!changes.TryGetValue(tag, out var tagChanges))
            {
                tagChanges = new LinkChanges();
                changes.Add(tag, tagChanges);
            }

            return tagChanges;
        }

        /// <summary>
        /// Applies the link changes to the tags as they are stored now, not to the snapshot, so links that publishing
        /// changed for other content while the job was running are kept, and clears the changes. Removals are applied
        /// before additions, so a link that moves between two tags that are stored as one (for example names that only
        /// differ in a way the database collation ignores) is kept.
        /// </summary>
        /// <returns>The number of links that were added or removed.</returns>
        private int SaveChanges(Dictionary<Tag, LinkChanges> changes)
        {
            var changedLinks = 0;

            foreach (var (tag, tagChanges) in changes.Where(change => change.Value.Removed.Count > 0))
            {
                var current = GetStoredTag(tag);
                var removed = current?.PermanentLinks?.Count(tagChanges.Removed.Contains) ?? 0;
                if (removed == 0)
                {
                    continue;
                }

                current.PermanentLinks = current.PermanentLinks.Where(link => !tagChanges.Removed.Contains(link)).ToList();
                _tagService.Save(current);
                changedLinks += removed;
            }

            foreach (var (tag, tagChanges) in changes.Where(change => change.Value.Added.Count > 0))
            {
                var current = GetStoredTag(tag)
                              ?? new Tag { Name = tag.Name, GroupKey = tag.GroupKey };
                var links = current.PermanentLinks?.ToList() ?? new List<Guid>();
                var missing = tagChanges.Added.Where(link => !links.Contains(link)).ToList();
                if (missing.Count == 0)
                {
                    continue;
                }

                links.AddRange(missing);
                current.PermanentLinks = links;
                _tagService.Save(current);
                changedLinks += missing.Count;
            }

            changes.Clear();
            return changedLinks;
        }

        /// <summary>
        /// Re-reads a tag from the snapshot by its identity. A tag the job created has no identity yet; saving it merges
        /// it into the stored tag with the same name and group key, so that is the tag to read.
        /// </summary>
        private Tag GetStoredTag(Tag tag)
        {
            return tag.Id != null
                ? _tagService.GetTagById(tag.Id)
                : _tagService.GetTagByNameAndGroup(tag.Name, tag.GroupKey);
        }

        private static string TagKey(string groupKey, string name) => $"{groupKey}\n{name}";

        public override void Stop()
        {
            _stop = true;
        }

        private sealed class LinkChanges
        {
            public HashSet<Guid> Added { get; } = new();

            public HashSet<Guid> Removed { get; } = new();
        }
    }
}
