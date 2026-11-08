# Geta Tags for Optimizely

[![Platform](https://img.shields.io/badge/Platform-.NET%2010-blue.svg?style=flat)](https://docs.microsoft.com/en-us/dotnet/)
[![Platform](https://img.shields.io/badge/Optimizely-%2013-orange.svg?style=flat)](http://world.episerver.com/cms/)

## Description

Geta Tags is a library that adds tagging functionality to Optimizely content.

## Features

- Define tag properties
- Query for data
- Admin page for managing tags
- Tags maintenance schedule job

See the [editor guide](docs/editor-guide.md) for more information.

## How to get started?

Start by installing NuGet package (use [Optimizely NuGet](https://nuget.optimizely.com/)):

```
dotnet add package Geta.Optimizely.Tags
```

Geta Tags library uses [tag-it](https://github.com/aehlke/tag-it) jQuery UI plugin for selecting tags.
To add Tags as a new property to your page types, you need to use the UIHint attribute like in this example:

```csharp
[UIHint("Tags")]
public virtual string Tags { get; set; }

[TagsGroupKey("mykey")]
[UIHint("Tags")]
public virtual string Tags { get; set; }

[CultureSpecific]
[UIHint("Tags")]
public virtual string Tags { get; set; }
```

Register tags in Startup.cs using folllowing service extension 

```csharp
services.AddGetaTags();
```

Then, call `UseGetaTags` in the `Configure` method:

```csharp
app.UseGetaTags();
```

Also, you have to add Razor pages routing support.

```csharp
app.UseEndpoints(endpoints =>
{
    endpoints.MapRazorPages();
});
```

Use `ITagEngine` to query for data:

```csharp
IEnumerable<ContentData> GetContentByTag(string tagName);
IEnumerable<ContentData> GetContentsByTag(Tag tag);
IEnumerable<ContentData> GetContentsByTag(string tagName, ContentReference rootContentReference);
IEnumerable<ContentData> GetContentsByTag(Tag tag, ContentReference rootContentReference);
IEnumerable<ContentReference> GetContentReferencesByTags(string tagNames);
IEnumerable<ContentReference> GetContentReferencesByTags(IEnumerable<Tag> tags);
IEnumerable<ContentReference> GetContentReferencesByTags(string tagNames, ContentReference rootContentReference);
IEnumerable<ContentReference> GetContentReferencesByTags(IEnumerable<Tag> tags, ContentReference rootContentReference);
```

## Customize Tag-it behaviour
You can customize the [Tag-it.js](https://github.com/aehlke/tag-it) settings by using the GetaTagsAttribute.
The following settings can currently be customized

- allowSpaces - defaults to **false**
- allowDuplicates - defaults to **false**
- caseSensitive - defaults to **true**
- readOnly - defaults to **false**
- tagLimit - defaults to **-1** (none)

```csharp
[CultureSpecific]
[UIHint("Tags")]
[GetaTags(AllowSpaces = true, AllowDuplicates = true, CaseSensitive = false, ReadOnly = true)]
public virtual string Tags { get; set; }
```

## Tag groups and languages

Every tag belongs to a *group key*, so the same tag name can exist independently in several groups. The group key of a
tag property is resolved like this:

| Property attributes | Group key | Example |
|---|---|---|
| none | the content's master language | `en` |
| `[TagsGroupKey("ArticleTags")]` | the key | `ArticleTags` |
| `[CultureSpecific]` | the language of the content version | `en`, `sv` |
| `[TagsGroupKey("ArticleTags")]` and `[CultureSpecific]` | the key followed by the language | `ArticleTagsen`, `ArticleTagssv` |

Autocomplete in the editor only suggests tags from the property's group, while `ITagEngine` methods that take tag names
(such as `GetContentReferencesByTags`) search by name across all groups.

**Localized tags.** A tag is a plain string; there is no built-in translation of a tag into other languages. To let
editors maintain different tags per language, mark the property with `[CultureSpecific]`. Each language version then
has its own tags in its own group. If you need one tag with translated display names, store a language-neutral tag
(for example `cancer-care`) and translate it when rendering, for example with the CMS localization service.

## Tags maintenance job

The *Geta Tags maintenance* scheduled job keeps the tag store in sync with the content. For every content item that a
tag links to, the job reads the tag properties of all its language versions and then:

- removes links from tags the content no longer uses, and from content that has been deleted or moved to the trash,
- adds missing links to tags the content does use, creating the tag if it was deleted in the admin UI.

The job never changes content. That has two consequences:

- Deleting a tag in the admin UI removes it from the tag store, but not from content. Content that still uses the tag
  creates it again the next time the content is published, or when the job runs if the content is still linked to
  another tag. To remove a tag everywhere, remove it from the content.
- Renaming a tag in the admin UI without ticking *Check this box to edit existing tags in the content* only renames the
  stored tag. Content that still uses the old name links to it again when the content is published or the job runs,
  which re-creates the old tag and unlinks the renamed one. Tick the box to also update and republish the content that
  uses the tag.

Tags are also kept in sync on every publish, so the job is mainly needed after imports, bulk changes or upgrades, and
after changing the group key of a tag property or removing a tag property: publishing only updates the groups of the
content's current tag properties.

## Upgrading from Geta.Tags (CMS 11)

The CMS 11 package `Geta.Tags` stored tags in the dynamic data store `Geta.Tags.Models.Tag`; this package uses
`Geta.Optimizely.Tags.Core.Tag`. After upgrading, no tags are shown until the old tags are imported:

1. Run the scheduled job *Geta Tags: import tags from Geta.Tags (CMS 11)* once. It copies the old tags into the new
   store and merges them with tags that already exist. It is safe to run more than once, and the old store is left
   untouched.
2. Run the *Geta Tags maintenance* job to re-link the tags with the current content and group keys.

## 🏁 Getting Started

### 📦 Prerequisites

Ensure your system is properly configured to meet all prerequisites for Geta Foundation Core listed [here](https://github.com/Geta/geta-foundation-core#%EF%B8%8F-prerequisites)

### 🐑 Cloning the repository

```bash
    git clone https://github.com/Geta/geta-optimizely-tags.git
    cd geta-optimizely-tags
    git submodule update --init
```

### 🚀 Running with Aspire (Recommended)
```bash
    # Windows
    cd sub/geta-foundation-core/src/Foundation.AppHost
    dotnet run

    # Linux / MacOS
    # Make sure your user can access Docker (member of the 'docker' group) so 'dotnet run' can start the containers.
    chmod +x sub/geta-foundation-core/src/Foundation/docker/build-script/*.sh
    cd sub/geta-foundation-core/src/Foundation.AppHost
    dotnet run
```

### 🖥️ Running as Standalone
```bash
   # Windows
   cd sub/geta-foundation-core
   ./setup.cmd
   cd ../../src/Geta.Optimizely.Tags.Web
   dotnet run

   # Linux / MacOS
   # Make sure your user can access Docker (member of the 'docker' group) so the setup/build can start the containers.
   cd sub/geta-foundation-core
   chmod +x *.sh
   ./setup.sh
   cd ../../src/Geta.Optimizely.Tags.Web
   dotnet run
```

### 🔍 Optimizely Graph (optional)

The sample runs **without** Graph credentials out of the box — the `Optimizely:ContentGraph` keys in
`src/Geta.Optimizely.Tags.Web/appsettings.json` are empty, so the Graph sync client is replaced with a
no-op and the site starts normally (no "Invalid credentials" startup failure). To enable Graph indexing
and search, fill in `AppKey`, `Secret`, and `SingleKey` from your Optimizely Graph subscription.

### 🧪 Running the tests

```bash
    dotnet test tests/Geta.Optimizely.Tags.Tests
```

The unit tests run in the CI build on every push and pull request, and before every release.

If you run into any issues, check the FAQ section [here](https://github.com/Geta/geta-foundation-core?tab=readme-ov-file#faq)

---

CMS username: admin@example.com

Password: Episerver123!

## Package maintainer

https://github.com/marisks

## Changelog

[Changelog](CHANGELOG.md)
