﻿// Copyright (c) Geta Digital. All rights reserved.
// Licensed under Apache-2.0. See the LICENSE file in the project root for more information

using System;
using System.Linq;
using EPiServer.Cms.Shell.Extensions;
using EPiServer.DataAnnotations;
using EPiServer.Shell;
using EPiServer.Shell.ObjectEditing;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;

namespace Geta.Optimizely.Tags.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class GetaTagsAttribute : Attribute, IDisplayMetadataProvider
    {
        public bool AllowSpaces { get; set; }
        public bool CaseSensitive { get; set; }
        public bool AllowDuplicates { get; set; }
        public int TagLimit { get; set; }

        public bool ReadOnly { get; set; }

        public GetaTagsAttribute()
        {
            AllowDuplicates = false;
            AllowSpaces = false;
            CaseSensitive = true;
            ReadOnly = false;
            TagLimit = -1;
        }

        public virtual void CreateDisplayMetadata(DisplayMetadataProviderContext context)
        {
            if (!(context.DisplayMetadata.AdditionalValues["epi:extendedmetadata"] is ExtendedMetadata extendedMetadata))
                return;

            var groupKeyAttribute = extendedMetadata
                .Attributes
                .FirstOrDefault(a => typeof(TagsGroupKeyAttribute) == a.GetType()) as TagsGroupKeyAttribute;
            var cultureSpecificAttribute = extendedMetadata
                .Attributes
                .FirstOrDefault(a => typeof(CultureSpecificAttribute) == a.GetType()) as CultureSpecificAttribute;
            var ownerContent = extendedMetadata.FindOwnerContent();

            extendedMetadata.ClientEditingClass = "geta-tags/TagsSelection";
            extendedMetadata.CustomEditorSettings["uiType"] = extendedMetadata.ClientEditingClass;
            extendedMetadata.CustomEditorSettings["uiWrapperType"] = UiWrapperType.Floating;
            extendedMetadata.EditorConfiguration["GroupKey"] =
                TagsHelper.GetGroupKeyFromAttributes(groupKeyAttribute, cultureSpecificAttribute, ownerContent);
            extendedMetadata.EditorConfiguration["allowSpaces"] = AllowSpaces;
            extendedMetadata.EditorConfiguration["allowDuplicates"] = AllowDuplicates;
            extendedMetadata.EditorConfiguration["caseSensitive "] = CaseSensitive;
            extendedMetadata.EditorConfiguration["readOnly "] = ReadOnly;
            extendedMetadata.EditorConfiguration["tagLimit"] = TagLimit;
        }
    }
}
