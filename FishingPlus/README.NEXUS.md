[center][size=5]FishingPlus[/size][/center]

[b]A BepInEx 5.4.x (x64) mod for Moonlight Peaks to customize fishing spawn rates.[/b]

[line]

[size=4][color=#6F51B1][b]Features[/b][/color][/size]
[list]
[*][b]Spawn Rate Customization:[/b] Adjust the spawning frequency of various fish species.[/*]
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
Alternatively, you can edit the generated configuration file located in [i]MoonlightPeaks/BepInEx/config/[/i].

[b]Available Settings per Fish:[/b]
[list]
[*][b]Override:[/b] Enable or disable custom spawn rates (true = Custom, false = Default).[/*]
[*][b]SpawnChance:[/b] Adjust spawn chance (Range: 0 to 100).[/*]
[*][b]MaxLimit:[/b] Set the maximum limit for a fish (0, 1, 2...).[/*]
[/list]

[size=5][color=#6F51B1][b]⚠️ IMPORTANT NOTE ON INITIAL CONFIGURATION:[/b][/color][/size]
When you start the game with this mod for the first time, the in-game menu (F1) will display placeholder values. This is completely normal, as the game only provides the original spawn rates when the map is loaded.

[b]How to set it up:[/b]
[list=1]
[*]Start the game and load your save file normally.[/*]
[*]The mod will automatically read the original vanilla fish spawn rates in the background when you enter a map and save them permanently to your [i].cfg[/i] file.[/*]
[*]From that moment on, you will have access to the real default values in the F1 menu [i](or .cfg-file)[/i] (and at every future game start) and can adjust them as you like using the sliders![/*]
[/list]

[line]

[i]Built with BepInEx.[/i]
[i]Source code available on [url=https://github.com/iTestor/moonlight-peaks-mods]GitHub[/url].[/i]
