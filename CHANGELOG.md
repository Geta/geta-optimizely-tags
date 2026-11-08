# Changelog

All notable changes to this project will be documented in this file.

## [3.1.0]

- Fixed content export failing with a `NullReferenceException` in `TagsExporter.AddTagsToExport`; tags are no longer added to the export package, because importing the content publishes it and links it to its tags, creating the tags that are missing ([#34](https://github.com/Geta/geta-optimizely-tags/issues/34))
- Fixed removed tags staying linked to content: publishing now unlinks removed tags for properties with `[TagsGroupKey]`, and removing a tag in the editor is now saved ([#20](https://github.com/Geta/geta-optimizely-tags/issues/20))
- Fixed the tags editor showing stale or empty tags after the CMS sets the value, for example after publishing a property in the page header ([#14](https://github.com/Geta/geta-optimizely-tags/issues/14))
- Fixed the maintenance job linking content to same-named tags in other groups and overwriting links added earlier in the same run; it now synchronizes tags per group across all language versions, and no longer re-saves every tag on each run ([#19](https://github.com/Geta/geta-optimizely-tags/issues/19), [#28](https://github.com/Geta/geta-optimizely-tags/issues/28))
- Fixed editing, updating and deleting tags in the admin UI beyond page 1 or in search results; the current page and search are kept, and the pager's previous link works ([#17](https://github.com/Geta/geta-optimizely-tags/issues/17))
- Updated the bundled jQuery to 3.7.1 and jQuery UI to 1.14.1 ([#18](https://github.com/Geta/geta-optimizely-tags/issues/18))
- Added the *Geta Tags: import tags from Geta.Tags (CMS 11)* scheduled job for upgrading from the CMS 11 package ([#15](https://github.com/Geta/geta-optimizely-tags/issues/15))
- Culture-specific group keys now use the language of the published content instead of the request's preferred culture
- `[GetaTags(CaseSensitive = ..., ReadOnly = ...)]` settings are now passed to the editor under the right keys
- Documented group keys, localized tags, the maintenance job and upgrading from Geta.Tags ([#29](https://github.com/Geta/geta-optimizely-tags/issues/29))
- Added a unit test project; the tests run in the CI build and before every release
- The sandbox now starts without Optimizely Graph credentials, as documented
- `Tag.ItemPageNumber` is obsolete and no longer set

## [3.0.0]

- Upgraded to Optimizely CMS 13.1.0 and .NET 10; dropped support for CMS 12 / .NET 6
- Replaced the obsolete `@Html.CreatePlatformNavigationMenu()` / `@Html.ApplyPlatformNavigation()` calls with the CMS 13 `<platform-navigation />` tag helper in the admin shell layout
- Tag group key resolution now accepts `IContentData`, so tags work on blocks as well as pages
- Updated content export to use `TypeOfTransfer.Exporting` (CMS 13 removed `MirroringExporting`)
- Removed SonarCloud from the CI build pipeline
- Replaced the sandbox with Geta Foundation Core (CMS 13 / Commerce 15) run via .NET Aspire
- Added the Optimizely NuGet feed to `NuGet.config` so CMS 13 / Graph / Commerce 15 packages restore on a clean build

> Earlier releases (2.0.x for `Geta.Optimizely.Tags`, and the pre-rename `Geta.Tags` 3.x–5.0 line) are available in the [GitHub Releases](https://github.com/Geta/geta-optimizely-tags/releases) and git history.
