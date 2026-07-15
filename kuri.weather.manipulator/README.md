# Weather Manipulator

A BepInEx 5.4.x (x64) mod for Moonlight Peaks to control and override the current weather conditions.

## Features
- **Weather Override:** Manually set the weather to your preferred type.
- **Lock Weather:** Optionally lock the current weather to prevent the game from changing it automatically.
- **Configurable:** Fine-tune the weather settings in-game using the BepInEx Configuration Manager.

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
- `Enable Weather Override`: Enable or disable the weather override. Default: False.
- `Lock Weather (Always)`: If active, the game can no longer change the weather on its own. Default: False.
- `Selected Weather`: The weather to enforce. Default: Clear_Spring.

*Optional: Use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to edit settings conveniently in-game (default hotkey: F1).*

---
*Built with BepInEx.*
