<h1 align="center">Jellyfin Reports Plugin</h1>
<h3 align="center">Part of the <a href="https://jellyfin.org/">Jellyfin Project</a></h3>

## About
The Jellyfin Reports plugin generates activity and media reports for your library.

These reports can be exported to Excel and CSV formats.

## Build & Installation Process
1. Clone this repository
2. Ensure you have .NET Core SDK setup and installed
3. Build the plugin with following command:
```
dotnet publish --configuration Release --output bin
```
4. Place the resulting `Jellyfin.Plugin.Reports.dll` file in a folder called `plugins/` inside your Jellyfin installation / data directory.

### Screenshot
<img src=screenshot.png>
