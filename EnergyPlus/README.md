# EnergyPlus

A BepInEx 5.4.x (x64) mod for Moonlight Peaks to manage energy consumption.
This mod provides tools to customize the energy usage of your character, allowing for a more tailored gameplay experience.

## Features
- **Energy Optimization:** Adjust energy depletion rates.
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

**Available Settings:**
- `EnergyDrainMultiplier`: Energy drain (Range: 0 to 10). 0 = Infinite energy, 10 = Normal Drain * 10. Default: 1.
- `EnergyGainMultiplier`: Energy gain (Range: 0 to 10). 0 = No energy gain, 10 = Normal Gain * 10. Default: 1.

*Optional: Use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to edit settings conveniently in-game (default hotkey: F1).*

---
*Built with BepInEx.*
