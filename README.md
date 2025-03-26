# <img src="https://i.imgur.com/drqCT3O.png" alt="BloxDump" width="48"> BloxDump
[![Version](https://img.shields.io/github/v/release/EmK530/BloxDump?color=ff7700)](https://github.com/EmK530/BloxDump/releases/latest)
[<img src="https://img.shields.io/github/downloads/EmK530/BloxDump/total?color=0077ff" alt="Downloads">](https://github.com/EmK530/BloxDump)

A program that goes through Roblox's CDN cache and converts any recognized files.<br>
This essentially lets you dump assets from any Roblox game.

## How to use
* Visit the [Releases](https://github.com/EmK530/BloxDump/releases) page and download a version from there.<br>
If it's your first time, download the `dependencies.zip` file as well and extract the contents with BloxDump.<br>

* Make sure you extract the program into an empty folder, it will create extra folders for dumping.<br>
Also extract the dependencies into this folder as well. It contains programs used for certain conversions.

* After that just open `BloxDump.exe` and you should be good to go!<br>

When starting you will be prompted if you want to clear your cache.<br>
This is to prevent any old assets from being caught. Any new cache files that show up will be dumped in real-time.

## Support
List of everything BloxDump can currently dump:

    • 2D Textures (PNG / JFIF)
    • 3D Textures (KTX => PNG)
    • Animations (RBXM)
    • Fonts (TTF)
    • Sounds (OGG)
    • Videos (WEBM)
    • Translations (JSON)
Meshes are currently unavailable due to Roblox releasing a new mesh version and migrating all meshes to it.
I will try to add support eventually, but for now you may run into issues!

## Contributions
BloxDump is fairly complete and most file types are dumped now.<br>If there is something you want to improve feel free to make a pull request!

## Credits
<b>Roblox Mesh to OBJ</b> co-developed by <b>[ApexdaGamer](https://github.com/ApexdaGamer)</b><br>
<b>Roblox Mesh documentation</b> by <b>[MaximumADHD](https://github.com/MaximumADHD)</b> - [View here](https://devforum.roblox.com/t/roblox-mesh-format/326114)
