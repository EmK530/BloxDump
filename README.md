<h1 align="center">
  <img src="https://i.imgur.com/drqCT3O.png" alt="BloxRip" width="150">
  <br>
  <b>BloxDump</b>
  <br>
</h1>
<p align="center">
  A simple Python script that goes through Roblox's CDN cache and dumps any recognized files.<br>
  This essentially lets you dump assets from any Roblox game.
</p>

## Support
Currently these file formats are supported:<br>
<b>Audio files (OGG)</b><br>
<b>2D Textures (PNG)</b><br>
<b>3D Textures (KTX => PNG)</b><br>

## Contributions
If there is any file format that you want to add support for, feel free to make a pull request!<br>
This is just a simple base for potentially a bigger dumping system.

## How to use
Download the source code as a ZIP and I advise you extract it into an empty folder.<br>
BloxDump will create additional folders inside for dumping.<br>
Make sure all modules are installed, seen in [Dependencies](https://github.com/EmK530/BloxDump#dependencies).<br>
Then launch BloxDump though Main.py and it will prompt you about deleting your cache.<br>
Only do this if you don't want to dump assets from previous gameplay sessions.<br>
<b>After the prompt it should start ripping! If you cleared your cache just start playing a game and it will begin.</b>

## Dependencies
Currently two modules are required:
```
pip install requests
pip install Pillow
```
Pillow is required for srgb2lin to function, this fixes strange colors with Khronos Texture PNGs.
