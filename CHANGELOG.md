# Changelog

All notable changes to this project will be documented in this file.

## [3.0.0]

- Upgraded to Optimizely CMS 13.1.0 and .NET 10; dropped support for CMS 12 / .NET 6
- Replaced the obsolete `@Html.CreatePlatformNavigationMenu()` / `@Html.ApplyPlatformNavigation()` calls with the CMS 13 `<platform-navigation />` tag helper in the admin shell layout
- Tag group key resolution now accepts `IContentData`, so tags work on blocks as well as pages
- Updated content export to use `TypeOfTransfer.Exporting` (CMS 13 removed `MirroringExporting`)
- Removed SonarCloud from the CI build pipeline
- Replaced the sandbox with Geta Foundation Core (CMS 13 / Commerce 15) run via .NET Aspire
- Added the Optimizely NuGet feed to `NuGet.config` so CMS 13 / Graph / Commerce 15 packages restore on a clean build

> Earlier releases (2.0.x for `Geta.Optimizely.Tags`, and the pre-rename `Geta.Tags` 3.x–5.0 line) are available in the [GitHub Releases](https://github.com/Geta/geta-optimizely-tags/releases) and git history.
