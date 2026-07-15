[center][size=5]FishingPlus[/size][/center]

[b]A mod to customize fishing spawn rates, with Live GUI.[/b]

[line]

[size=4][color=#6F51B1][b]Features[/b][/color][/size]
[list]
[*][b]Spawn Rate Customization:[/b] Adjust the spawning frequency of various fish species.[/*]
[*][b]Skip Animation:[/b] Optional setting to skip the 'fish present' animation for faster fishing.[/*]
[*][b]Live GUI:[/b] The mod provides a live GUI that displays all fish present in the current area, showing their current counts, maximum limits, and spawn chances: [i]Name (Current/Max) Chance%[/i].[/*]
[*][b]Configurable:[/b] Fine-tune the balance in-game using the BepInEx Configuration Manager.[/*]
[/list]

[size=4][color=#6F51B1][b]Requirements[/b][/color][/size]
[list]
[*][url=https://github.com/BepInEx/BepInEx/releases]BepInEx 5.4.x (x64)[/url][/*]
[*][url=https://github.com/BepInEx/BepInEx.ConfigurationManager/releases]BepInEx Configuration Manager[/url][/*]
[/list]

[size=4][color=#6F51B1][b]Installation[/b][/color][/size]
[list=1]
[*]Ensure [b]BepInEx 5.4.x[/b] is installed in your Moonlight Peaks game folder.[/*]
[*]Download the mod.[/*]
[*]Extract the contents into your [i]MoonlightPeaks/BepInEx/plugins/[/i] folder.[/*]
[/list]

[size=4][color=#6F51B1][b]Configuration[/b][/color][/size]
Upon first run, the mod will generate a configuration file. You can easily edit these values in-game by opening the [b]Configuration Manager[/b] (default hotkey: [i]F1[/i]).
The configuration entries are sorted alphabetically for better overview.
Alternatively, you can edit the generated configuration file located in [i]MoonlightPeaks/BepInEx/config/[/i].

[color=#6F51B1][b]Available Settings Global:[/b][/color]
[list]
[*][b]EnableDebugLogging:[/b] Enables verbose debug logging for troubleshooting. Keep this disabled during normal play - enabling it produces a lot of log output and can spam the debug console.[/*]
[*][b]RespawnIntervalMinutes:[/b] Respawn interval in minutes, applies to ALL fish with an active override. (lower = spawns more often)[/*]
[*][b]SkipFishPresentItemAnimation:[/b] If enabled, the game will skip the 'fish present' animation.[/*]
[/list]

[color=#6F51B1][b]Available Settings per Fish:[/b][/color]
[list]
[*][b]Override:[/b] Enable or disable custom spawn rates (true = Custom, false = Default).[/*]
[*][b]SpawnChance:[/b] Adjust spawn chance (Range: 0 to 100).[/*]
[*][b]MaxLimit:[/b] Set the maximum limit for a fish (0, 1, 2...).[/*]
[/list]

[line]

[i]Built with BepInEx.[/i]
[i]Source code available on [url=https://github.com/iTestor/moonlight-peaks-mods]GitHub[/url].[/i]
