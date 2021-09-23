# BOI
BepInEx4RW Mod manager

Current state of the project: acceptable stability. Assistance in troubleshooting is appreciated.

**HOW TO INSTALL:**

Download latest release, unpack somewhere (does not have to be inside game's folder).

**HOW TO USE:**

Press "Select path", navigate to Rain World's root folder, press Apply. If you do not have BepInEx installed, you can use AUDB tab `Download mods / install bepinex` to get it in one click.

Mod list is refreshed every time you focus main window. Allows setting tags and descriptions for mods to search through later, in case you have a gigantic modset that's too annoying to get through manually.

The app has a bunch of additional tools, check Options menu.

Starting from 0.1.7, self-updates to latest stable release from this repo if needed.

**PARAMETERS**
- Opening a new console window to display log output: add `-nc` or `--new-console` to launch arguments. Alternatively, put a file called `showConsole.txt` next to the exe.
- Attaching parent process console (if you are running from command prompt for some reason): `-ac` or `--attach-console`.
- Disabling self-updater: `-nu` or `--no-update`. Alternatively, put a file called `neverUpdate.txt` next to the exe.