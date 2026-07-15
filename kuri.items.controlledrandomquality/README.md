# Controlled Random Quality

A BepInEx 5.4.x (x64) mod for Moonlight Peaks to control the quality level of generated items.

## Features
- **Quality Control:** Force specific quality levels (Standard, Silver, Gold, etc.) for any item generated in the game.
- **Maintain Randomness:** Option to keep the default game randomness for quality.
- **Configurable:** Easily change settings in-game using the BepInEx Configuration Manager.

## Requirements
- [BepInEx 5.4.x (x64)](https://github.com/BepInEx/BepInEx/releases)
- [Optional: BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases)

## Installation
1. Ensure [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) is installed in your Moonlight Peaks game folder.
2. Download the mod.
3. Extract the contents into your `MoonlightPeaks/BepInEx/plugins/` folder.

## Configuration
Upon first run, the mod will generate a configuration file located at `MoonlightPeaks/BepInEx/config/`. 

**Available Settings:**
- `Quality Override`: Choose which quality level should be forced when an item is generated. Set to 'Random' to keep normal game randomness.

*Optional: Use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to edit settings conveniently in-game (default hotkey: F1).*

---
*Built with BepInEx.*
