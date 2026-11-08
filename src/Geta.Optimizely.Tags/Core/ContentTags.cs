// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using Geta.Optimizely.Tags.Core.Attributes;

namespace Geta.Optimizely.Tags.Core
{
    /// <summary>
    /// Reads the tags a content item holds in its tag properties.
    /// </summary>
    internal static class ContentTags
    {
        /// <summary>
        /// Returns the tag names of every tag property on the content, keyed by the group key the tags are stored under.
        /// A group key is present (possibly with no names) for every tag property, so callers can tell which groups this
        /// content version owns. Tag names and group keys are compared case-insensitively, like the tag store lookups.
        /// </summary>
        /// <param name="content">The content version to read.</param>
        /// <param name="contentType">The content type of the content.</param>
        /// <param name="cultureSpecificOnly">Only read properties marked with <see cref="CultureSpecificAttribute"/>.</param>
        public static Dictionary<string, HashSet<string>> GetTagsByGroupKey(
            IContent content,
            ContentType contentType,
            bool cultureSpecificOnly = false)
        {
            var result = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

            if (content is not ContentData contentData || contentType?.ModelType == null)
            {
                return result;
            }

            foreach (var propertyDefinition in contentType.PropertyDefinitions.Where(TagsHelper.IsTagProperty))
            {
                var propertyInfo = contentType.ModelType.GetProperty(propertyDefinition.Name);
                if (propertyInfo == null)
                {
                    continue;
                }

                var cultureSpecificAttribute = propertyInfo.GetCustomAttribute<CultureSpecificAttribute>();
                if (cultureSpecificOnly && cultureSpecificAttribute?.IsCultureSpecific != true)
                {
                    continue;
                }

                var groupKey = TagsHelper.GetGroupKeyFromAttributes(
                    propertyInfo.GetCustomAttribute<TagsGroupKeyAttribute>(),
                    cultureSpecificAttribute,
                    content);

                if (!result.TryGetValue(groupKey, out var names))
                {
                    names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    result.Add(groupKey, names);
                }

                names.UnionWith(ParseTagNames(contentData[propertyDefinition.Name] as string));
            }

            return result;
        }

        /// <summary>
        /// Shared (not culture-specific) properties can only be edited in the master language, so a version in another
        /// language only owns the tags of its culture-specific properties.
        /// </summary>
        public static bool IsMasterLanguageVersion(IContent content)
        {
            return content is not ILocalizable { Language: not null, MasterLanguage: not null } localizable
                   || localizable.Language.Equals(localizable.MasterLanguage);
        }

        private static IEnumerable<string> ParseTagNames(string tagNames)
        {
            return tagNames?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Enumerable.Empty<string>();
        }
    }
}
