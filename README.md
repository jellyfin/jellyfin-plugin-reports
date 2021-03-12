<h1 align="center">Jellyfin Reports Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org">Jellyfin Project</a></h3>

<p align="center">
<img alt="Logo Banner" src="https://raw.githubusercontent.com/jellyfin/jellyfin-ux/master/branding/SVG/banner-logo-solid.svg?sanitize=true"/>
<br/>
<br/>
<a href="https://github.com/jellyfin/jellyfin-plugin-reports/actions?query=workflow%3A%22Test+Build+Plugin%22">
<img alt="GitHub Workflow Status" src="https://img.shields.io/github/workflow/status/jellyfin/jellyfin-plugin-reports/Test%20Build%20Plugin.svg">
</a>
<a href="https://github.com/jellyfin/jellyfin-plugin-reports">
<img alt="MIT License" src="https://img.shields.io/github/license/jellyfin/jellyfin-plugin-reports.svg"/>
</a>
<a href="https://github.com/jellyfin/jellyfin-plugin-reports/releases">
<img alt="Current Release" src="https://img.shields.io/github/release/jellyfin/jellyfin-plugin-reports.svg"/>
</a>
</p>

## About

The Jellyfin Reports plugin generates activity and media reports for your library.

These reports can be exported to Excel and CSV formats.

## Installation via Plugin Repository

1. Open the Jellyfin Server Dashboard 
2. Go to Plugins 
3. In the Catalogue menu on the top search the "Reports" plugin and install it
4. Restart the server and the plugin will be available in the Advanced section in the server Dashboard. 

## Build & Manual Installation Process

1. Clone this repository

2. Ensure you have .NET Core SDK set up and installed

3. Build the plugin with your favorite IDE or the `dotnet` command:

```
dotnet publish --configuration Release --output bin
```

4. Place the resulting `Jellyfin.Plugin.Reports.dll` file in a folder called `plugins/` inside your Jellyfin data directory
