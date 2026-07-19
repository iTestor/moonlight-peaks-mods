using System;
using System.IO;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using Director.Nodes;
using UnityEngine;

using EclipseFramework;

namespace kuri.moonlightpeaks.custommail
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    [BepInDependency("kuri.moonlightpeaks.eclipseframework")]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static EF _eclipseFramework;

        // List to keep track of all successfully loaded mail assets
        private List<MailAsset> myLoadedMails = new List<MailAsset>();

        // Array containing the exact names of the assets inside your Unity AssetBundle
        private readonly string[] mailAssetNames = new string[]
        {
            "MyMod_TestDefault",
            "MyMod_TestFiona",
            "MyMod_TestLuna",
            "MyMod_TestMom"
        };

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            _eclipseFramework = new EF(Info);

            _eclipseFramework.MailSystem.Register("kurimailtest");

            MailAsset mail = _eclipseFramework.MailSystem.GetMailAsset("kurimailtest", "MyMod_TestDefault");

            _eclipseFramework.MailSystem.AddItem(mail, "Item_Consumable_Beer", 50);

            _eclipseFramework.MailSystem.RegisterMailReadAction(mail, () =>
            {
                _logger.LogInfo($"[{mail.name}] was read. You can perform additional actions here, such as starting a quest or logging a message.");
            });

            MailAsset mail2 = _eclipseFramework.MailSystem.GetMailAsset("kurimailtest", "MyMod_TestFiona");

            _eclipseFramework.MailSystem.AddItems(mail2, 
                new MailRewardData("Item_Consumable_Beer", 50),
                new MailRewardData("Item_Consumable_Blackberry", 41, false, ItemQualityLevel.Perfect)
            );
        }

        private void Update()
        {
            // 4. Wenn wir im Spiel F5 drücken, werfen wir alle geladenen Mails ins Postfach
            if (UnityEngine.Input.GetKeyDown(KeyCode.F5))
            {

                MailAsset mail = _eclipseFramework.MailSystem.GetMailAsset("kurimailtest", "MyMod_TestDefault");

                var persistence = AddMailNode.AddMail(mail, true);

                if (persistence != null)
                {
                    _logger.LogInfo($"[{mail.name}] Post ist da! Schau mal in deinen Briefkasten.");
                }
                else
                {
                    _logger.LogWarning($"[{mail.name}] Fehler beim Hinzufügen (ist das Spiel schon fertig geladen?)");
                }

                MailAsset mail2 = _eclipseFramework.MailSystem.GetMailAsset("kurimailtest", "MyMod_TestFiona");

                var persistence2 = AddMailNode.AddMail(mail2, true);

                if (persistence2 != null)
                {
                    _logger.LogInfo($"[{mail2.name}] Post ist da! Schau mal in deinen Briefkasten.");
                }
                else
                {
                    _logger.LogWarning($"[{mail2.name}] Fehler beim Hinzufügen (ist das Spiel schon fertig geladen?)");
                }
            }
        }
    }
}