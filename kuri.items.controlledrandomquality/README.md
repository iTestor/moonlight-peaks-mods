# Controlled Random Quality

A BepInEx 5.4.x (x64) mod for Moonlight Peaks to control the quality level of items with random quality.

## Features
- **Quality Control:** Force specific quality levels (Standard, Silver, Gold, etc.) for items that usually have random quality.
- **Configurable:** Easily change settings in-game using the BepInEx Configuration Manager.

## Fixes
- **Lost Item Job Quests:** Prevents Lost Item Quests from accidentally spawning with forced higher quality levels, ensuring NPCs always accept them.
- **Save Cleanup:** Includes an automated retro-fix to clear bugged quest items from your save file.

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
- `Reset Bugged Lost Item Quests`: Enabled by default. Sleeping in bed once automatically cleans up bugged quest items from your inventory/chests and resets the quest so it can show up on the Job Board again. Auto-disables itself after running.

*Optional: Use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to edit settings conveniently in-game (default hotkey: F1).*

---
*Built with BepInEx.*
*Source code available on [GitHub](https://github.com/iTestor/moonlight-peaks-mods).* 
*Join my Mod Community on [Discord](https://discord.gg/2F6VXeZYHK)*
