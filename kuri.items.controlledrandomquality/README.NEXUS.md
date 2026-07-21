[center][size=5]Controlled Random Quality[/size][/center]

[b]A mod to control the quality level of items with random quality.[/b]

[line]

[size=4][color=#6F51B1][b]Features[/b][/color][/size]
[list]
[*][b]Quality Control:[/b] Force specific quality levels (Regular, Good, Perfect) for items that usually have random quality.[/*]
[*][b]Configurable:[/b] Easily change settings in-game using the BepInEx Configuration Manager.[/*]
[/list]

[size=4][color=#6F51B1][b]Fixes[/b][/color][/size]
[list]
[*][b]Lost Item Job Quests:[/b] Prevents Lost Item Quests from accidentally spawning with forced higher quality levels, ensuring NPCs always accept them.[/*]
[*][b]Save Cleanup:[/b] Includes an automated retro-fix to clear bugged quest items from your save file.[/*]
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

[color=#6F51B1][b]Available Settings:[/b][/color]
[list]
[*][b]Quality Override:[/b] Choose which quality level should be forced when an item is generated. Set to 'Random' to keep normal game randomness.[/*]
[*][b]Reset Bugged Lost Item Quests:[/b] Enabled by default. Sleeping in bed once automatically cleans up bugged quest items from your inventory/chests and resets the quest so it can show up on the Job Board again. Auto-disables itself after running.[/*]
[/list]

[line]

[i]Built with BepInEx.[/i]
[i]Source code available on [url=https://github.com/iTestor/moonlight-peaks-mods]GitHub[/url].[/i]
[i]Join my Mod Community on [url=https://discord.gg/2F6VXeZYHK]Discord[/url][/i]
