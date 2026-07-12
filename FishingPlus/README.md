# FishingPlus

A BepInEx 5.4.x (x64) mod for Moonlight Peaks to customize fishing spawn rates.

## Features
- **Spawn Rate Customization:** Adjust the spawning frequency of various fish species.
- **Configurable:** Fine-tune the balance in-game using the BepInEx Configuration Manager.

## Requirements
- [BepInEx 5.4.x (x64)](https://github.com/BepInEx/BepInEx/releases)
- [Optional: BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases)

## Installation
1. Ensure [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) is installed in your Moonlight Peaks game folder.
2. Download the mod.
3. Extract the contents into your `MoonlightPeaks/BepInEx/plugins/` folder.

## Configuration
Upon first run, the mod will generate a configuration file located at `MoonlightPeaks/BepInEx/config/`. 

*Optional: Use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to edit settings conveniently in-game (default hotkey: F1).*

⚠️ **IMPORTANT NOTE ON INITIAL CONFIGURATION:**
When you start the game with this mod for the first time, the in-game menu (F1) will display placeholder values. This is completely normal, as the game only provides the original spawn rates when the map is loaded.

**How to set it up:**
1. Start the game and load your save file normally.
2. The mod will automatically read the original vanilla fish spawn rates in the background when you enter a map and save them permanently to your `.cfg` file.
3. From that moment on, you will have access to the real default values in the F1 menu `(or .cfg-file)` (and at every future game start) and can adjust them as you like using the sliders!

---
*Built with BepInEx.*
