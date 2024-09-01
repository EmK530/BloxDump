# <img src="https://i.imgur.com/drqCT3O.png" alt="BloxDump" width="48"> BloxDump
[![Version](https://img.shields.io/github/v/release/EmK530/BloxDump?color=ff7700)](https://github.com/EmK530/BloxDump/releases/latest)
[<img src="https://img.shields.io/github/downloads/EmK530/BloxDump/total?color=0077ff" alt="Downloads">](https://github.com/EmK530/BloxDump)

A program that goes through Roblox's CDN cache and converts any recognized files.<br>
This essentially lets you dump assets from any Roblox game.

## How to use
* Visit the [Releases](https://github.com/EmK530/BloxDump/releases) page and download a version from there.<br>
If you want more details on BloxDump activity, download the debug zip file.<br>

* Make sure you extract the program into an empty folder, it will create extra folders for dumping.<br>
You also have to extract `PVRTexToolCLI.exe` and `ffmpeg.exe` as they are necessary for conversion.

* After that just open `BloxDump.exe` and you should be good to go!<br>

When starting you will be prompted if you want to use multithreading, this uses more CPU but dumps assets faster.<br>
To avoid dumping assets from previous sessions, you have the option to clear your cache. Any new cache files will be dumped in real-time.

## Support
List of everything BloxDump can currently dump:<br>
<b>2D Textures (PNG / JFIF)</b><br>
<b>3D Textures (KTX => PNG)</b><br>
<b>Animations (RBXM)</b><br>
<b>Fonts (TTF)</b><br>
<b>Meshes (OBJ)</b><br>
<b>Sounds (OGG)</b><br>
<b>Videos (WEBM)</b><br>
<b>Translations (JSON)</b><br>

## Contributions
BloxDump is fairly complete and most file types are dumped now.<br>If there is something you want to improve feel free to make a pull request!

## Credits
<b>Roblox Mesh to OBJ</b> co-developed by <b>[ApexdaGamer](https://github.com/ApexdaGamer)</b><br>
<b>Roblox Mesh documentation</b> by <b>[MaximumADHD](https://github.com/MaximumADHD)</b> - [View here](https://devforum.roblox.com/t/roblox-mesh-format/326114)
