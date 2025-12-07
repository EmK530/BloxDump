# <img src="https://i.imgur.com/drqCT3O.png" alt="BloxDump" width="48"> BloxDump
[![Version](https://img.shields.io/github/v/release/EmK530/BloxDump?color=ff7700&nocache1)](https://github.com/EmK530/BloxDump/releases/latest)
[<img src="https://img.shields.io/github/downloads/EmK530/BloxDump/total?color=0077ff" alt="Downloads">](https://github.com/EmK530/BloxDump)

A program that goes through Roblox's CDN cache and converts any recognized files.<br>
This essentially lets you dump assets from any Roblox game.

## How to use
* Visit the [Releases](https://github.com/EmK530/BloxDump/releases) page and download a version from there.<br>
* Make sure you extract the program and everything else somewhere safe, it will create extra folders for dumping.<br>
* After that just open `BloxDump.exe` and you should be good to go!<br>

When starting you will be prompted if you want to clear your cache.<br>
This is to prevent any old assets from being caught. Any new cache files that show up will be dumped in real-time.

## Support
List of everything BloxDump can currently dump:

    • 2D Textures (PNG / JFIF / WebP)
    • 3D Textures (KTX => PNG)
    • Fonts (TTF)
    • Meshes (v1-v7 => OBJ)
    • Sounds (OGG)
    • Videos (WEBM)
    • Translations (JSON)
    • RBXM Files

## Contributions
BloxDump is fairly complete and most file types are dumped now.<br>If there is something you want to improve feel free to make a pull request!

## Special thanks to
<b>[ApexdaGamer](https://github.com/ApexdaGamer)</b>, for co-developing the <b>Roblox Mesh to OBJ</b> converter.<br>
<b>[MaximumADHD](https://github.com/MaximumADHD)</b>, for documenting the [Roblox Mesh Format](https://devforum.roblox.com/t/roblox-mesh-format/326114)<br>
<b>[Nominom](https://github.com/Nominom)</b>, for creating [BCnEncoder.NET](https://github.com/Nominom/BCnEncoder.NET) which can convert BCn textures<br>
<b>[EvergineTeam](https://github.com/EvergineTeam)</b>, for creating [Draco.Net](https://github.com/EvergineTeam/Draco.Net) which made Roblox Mesh v7 support possible<br>
<b>[deccer](https://github.com/deccer)</b>, for creating [Ktx2Sharp](https://github.com/deccer/Ktx2Sharp) which provided bindings for the KTX library
