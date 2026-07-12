using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace Kuri.SRDebugger
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static ManualLogSource _logger;
        internal static Harmony _harmony;

        private bool _hasSetupRun = false;

        private void Awake()
        {
            // Plugin startup logic
            _logger = base.Logger;
            _logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            _harmony = new Harmony("dev.kuri.moonlightpeaks.srdebugger");
            _harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        void Update()
        {
            // Ich benutze hier mal das Update-Event, da du vorhin F9 erwähnt hast
            if (UnityEngine.Input.GetKeyDown(KeyCode.F9))
            {
                TriggerMenu();
            }
        }

        private void TriggerMenu()
        {
            // 1. Falls SRDebug noch schläft, aufwecken
            if (SRDebug.Instance == null)
            {
                Debug.Log("[VampireCheat] Initialisiere SRDebug...");
                SRDebug.Init();
            }

            if (SRDebug.Instance != null)
            {
                // 2. Setup nur ausführen, wenn es noch nicht lief
                if (!_hasSetupRun)
                {
                    try
                    {
                        Debug.Log("[VampireCheat] Führe SROptionsVampire.Setup() aus...");
                        SROptionsVampire.Setup();
                    }
                    catch (System.Exception ex)
                    {
                        // Falls das Spiel es heimlich schon geladen hatte, ignorieren wir den Fehler
                        Debug.LogWarning("[VampireCheat] Setup war wohl schon aktiv: " + ex.Message);
                    }
                    _hasSetupRun = true; // Markieren, damit wir es nicht nochmal versuchen
                }

                // 3. Menü öffnen
                SRDebug.Instance.ShowDebugPanel();
                Debug.Log("[VampireCheat] Menü geöffnet!");
            }
        }
    }
}
