# CritterPlus

Take full control over critter spawn rates, skip catching animations, and track live local spawns.

## Features
- **Spawn Rate Customization:** Adjust the spawning frequency of various critter species.
- **Skip Animation:** Optional setting to skip the 'present' animation for faster catch.
- **Live GUI:** The mod provides a live GUI that displays all critters present in the current area, showing their current counts and maximum limits: `Name (Current/Max)`.
- **Configurable:** Fine-tune the balance in-game using the BepInEx Configuration Manager.

## Requirements
- [BepInEx 5.4.x (x64)](https://github.com/BepInEx/BepInEx/releases)
- [Optional: BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases)

## Installation
1. Ensure [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) is installed in your Moonlight Peaks game folder.
2. Download the mod.
3. Extract the contents into your `MoonlightPeaks/BepInEx/plugins/` folder.

## Configuration
Upon first run, the mod will generate a configuration file. You can easily edit these values in-game by opening the BepInEx Configuration Manager (default hotkey: F1). The configuration is sorted alphabetically for easier navigation.
Alternatively, you can edit the generated configuration file located in `MoonlightPeaks/BepInEx/config/`.

**Available Settings Global:**
- `EnableDebugLogging`: Enables verbose debug logging for troubleshooting. Keep this disabled during normal play - enabling it produces a lot of log output and can spam the debug console.
- `RespawnIntervalMinutes`: Respawn interval in minutes, applies to ALL critters with an active override. (lower = spawns more often)
- `SkipPresentItemAnimation`: If enabled, the game will skip the 'present' animation.

**Available Settings per Critter:**
- `Override`: Enable or disable custom spawn rates (true = Custom, false = Default).
- `SpawnChance`: Adjust spawn chance (Range: 0 to 100).
- `MaxLimit`: Set the maximum limit for a critter (0, 1, 2...).

*Optional: Use [BepInEx Configuration Manager](https://github.com/BepInEx/BepInEx.ConfigurationManager/releases) to edit settings conveniently in-game (default hotkey: F1).*

---
*Built with BepInEx.*
