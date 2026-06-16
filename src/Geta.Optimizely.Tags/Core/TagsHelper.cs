// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPiServer.Core;
using EPiServer.DataAbstraction;
using EPiServer.DataAnnotations;
using EPiServer.Globalization;
using EPiServer.Web;
using Geta.Optimizely.Tags.Core.Attributes;

namespace Geta.Optimizely.Tags.Core
{
    public static class TagsHelper
    {
        public static string GetGroupKeyFromAttributes(
            TagsGroupKeyAttribute groupKeyAttribute,
            CultureSpecificAttribute cultureSpecificAttribute,
            IContentData content)
        {
            var groupKey = string.Empty;

            if (groupKeyAttribute == null && cultureSpecificAttribute == null && content is ILocalizable localizableContent)
            {
                groupKey += localizableContent.MasterLanguage;
            }

            if (groupKeyAttribute != null)
            {
                groupKey += groupKeyAttribute.Key;
            }

            if (cultureSpecificAttribute != null && cultureSpecificAttribute.IsCultureSpecific)
            {
                groupKey += ContentLanguage.PreferredCulture ?? CultureInfo.CurrentCulture;
            }

            return groupKey;
        }

        public static ContentReference GetContentReference(Guid contentGuid)
        {
            return PermanentLinkUtility.FindContentReference(contentGuid) ?? ContentReference.EmptyReference;
        }

        public static IEnumerable<ContentReference> GetContentReferences(IEnumerable<Guid> contentLinks)
        {
            return contentLinks.Select(GetContentReference).Where(reference => !ContentReference.IsNullOrEmpty(reference));
        }

        public static bool IsTagProperty(PropertyDefinition propertyDefinition)
        {
            return propertyDefinition != null
                   && propertyDefinition.TemplateHint == "Tags";
        }
    }
}
