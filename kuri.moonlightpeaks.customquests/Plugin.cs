using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using Director.Core;
using Director.Nodes;
using UnityEngine;

namespace kuri.moonlightpeaks.customquests
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger;
        private StartQuestNode customQuest;

        private void Awake()
        {
            // Plugin startup logic
            Logger = base.Logger;
            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            
        }

        private void Update()
        {
            // Plugin update logic
            if (UnityEngine.Input.GetKeyDown(KeyCode.F8))
            {

                customQuest = new();
                QuestObjectiveNode myObjective = new();

                // Nutze ECHTE GUID-Strings, sonst liefert FromString() nur "Empty" zurück!
                SerializedGuid questId = SerializedGuid.NewGuid();
                SerializedGuid objectiveId = SerializedGuid.NewGuid();

                SetPrivateField(customQuest, "serializedGuid", questId);
                SetPrivateField(myObjective, "serializedGuid", objectiveId);

                // Wichtig: Die Properties für Namen müssen befüllt sein, sonst stürzt das UI ab
                typeof(StartQuestNode).GetProperty("QuestName")?.SetValue(customQuest, "Meine Custom Mod Quest");
                typeof(QuestObjectiveNode).GetProperty("ObjectiveName")?.SetValue(myObjective, "Sammle 5 Pilze");

                MultilineLocalizedText questTitle = new();
                questTitle.Mode = MultilineLocalizedText.ModeType.Draft;
                questTitle.SetDraft("Meine Custom Mod Quest");
                SetPrivateField(customQuest, "<LocalizedTitle>k__BackingField", questTitle);
                SetPrivateField(customQuest, "<LocalizedDescription>k__BackingField", questTitle);


                // Verbindung herstellen via Reflection
                var childGuidsField = typeof(BaseParentDirectorNode).GetField("childNodeSerializedGuids", BindingFlags.NonPublic | BindingFlags.Instance);
                var childNodesField = typeof(BaseParentDirectorNode).GetField("childNodes", BindingFlags.NonPublic | BindingFlags.Instance);

                var childGuids = (List<SerializedGuid>)childGuidsField?.GetValue(customQuest);
                var childNodes = (List<BaseDirectorNode>)childNodesField?.GetValue(customQuest);

                if (childGuids != null && childNodes != null)
                {
                    childGuids.Add(objectiveId);
                    childNodes.Add(myObjective);
                }

                DirectorNode.Add(customQuest);
                DirectorNode.Add(myObjective);

                customQuest.StartQuest();
            }
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
            field?.SetValue(obj, value);
        }
    }
}