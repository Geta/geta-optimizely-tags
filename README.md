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
    sudo env "PATH=$PATH" bash
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
   sudo env "PATH=$PATH" bash
   cd sub/geta-foundation-core
   chmod +x *.sh
   ./setup.sh
   cd ../../src/Geta.Optimizely.Tags.Web
   dotnet run
```

If you run into any issues, check the FAQ section [here](https://github.com/Geta/geta-foundation-core?tab=readme-ov-file#faq)

---

CMS username: admin@example.com

Password: Episerver123!

## Package maintainer

https://github.com/marisks

## Changelog

[Changelog](CHANGELOG.md)
