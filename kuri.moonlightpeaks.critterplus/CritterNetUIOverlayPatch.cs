using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Chicken.UI;
using Chicken.Utilities;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace kuri.moonlightpeaks.critterplus
{
    [HarmonyPatch]
    public static class CritterNetUIOverlayPatch
    {
        private static CritterUIComponent _uiComponent;
        private static PlayerEquipment _playerEquipment;

        [HarmonyPatch(typeof(PlayerEquipment), "ShowGrabbedItemScreenIfNeeded")]
        [HarmonyPostfix]
        public static void PostfixShowScreen(PlayerEquipment __instance)
        {
            _playerEquipment = __instance;

            if (__instance.GrabbedItemView == null)
            {
                Plugin.LogDebug("[CritterPlus] ShowGrabbedItemScreenIfNeeded: GrabbedItemView is null.");
                UpdateUIState(false);
                return;
            }

            if(isToolLActive())
            {
                Plugin.LogDebug("[CritterPlus] Net active. show own GUI...");
                UpdateUIState(Plugin.ShowPopulationGUI.Value);
                return;
            }

            UpdateUIState(false);
        }

        [HarmonyPatch(typeof(PlayerEquipment), "DestroyGrabbedItem")]
        [HarmonyPrefix]
        public static void PrefixDestroy()
        {
            Plugin.LogDebug("[CritterPlus] Hide custom UI.");
            UpdateUIState(false);
        }

        public static bool isToolLActive()
        {
            var itemAsset = _playerEquipment?.GrabbedItemView?.ItemAsset;
            if (itemAsset != null && itemAsset.ToolAddon != null)
            {
                var toolTypes = AddressableLibrary<ToolTypeLibrary>.Instance;
                if (toolTypes != null && itemAsset.ToolAddon.ToolType == toolTypes.Net)
                {
                    return true;
                }
            }
            return false;
        }

        public static void UpdateUIState(bool? show = null)
        {
            // Erstellen, falls noch nicht vorhanden und wir sie anzeigen wollen (oder toggeln)
            if (_uiComponent == null && (show == true || show == null))
            {
                GameObject uiObject = new GameObject("CritterPlus_UI");
                UnityEngine.Object.DontDestroyOnLoad(uiObject);
                _uiComponent = uiObject.AddComponent<CritterUIComponent>();
            }

            if (_uiComponent != null)
            {
                // Wenn 'show' übergeben wurde, nutze den Wert. Wenn null, invertiere den aktuellen Status (Toggle).
                _uiComponent.IsEnabled = show ?? !_uiComponent.IsEnabled;
            }
        }
    }

    /// <summary>
    /// Eigene UI-Komponente, die sich links perfekt in der Mitte platziert.
    /// </summary>
    public class CritterUIComponent : MonoBehaviour
    {
        public bool IsEnabled { get; set; } = false;
        private List<string> _critterToDisplay = new List<string>();
        private float _lastUpdate = 0f;

        // Wie oft die Liste aktualisiert wird (Sekunden). OnGUI läuft jeden Frame, ein
        // Reflection-Scan über alle SpawnConfigs jedes Frame wäre unnötig teuer.
        private const float UpdateIntervalSeconds = 1f;

        private void Update()
        {
            if (!IsEnabled)
                return;

            if (Time.time - _lastUpdate < UpdateIntervalSeconds)
                return;

            _lastUpdate = Time.time;

            try
            {
                RefreshCritterList();
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[CritterUIComponent] Error updating the critter list: {ex}");
            }
        }

        /// <summary>
        /// Liest den aktuell aktiven EntityBasicCritterSpawner aus und befüllt _critterToDisplay mit jedem
        /// bekannten Fisch, der gerade tatsächlich im Bereich instanziiert ist (InstantiatedSpawns
        /// pro SpawnConfig). Das funktioniert bewusst auch bei MaxLimit=0: der Limit-Patch
        /// verhindert nur NEUE Spawns, entfernt aber keine bereits vorhandenen Fische - die
        /// tauchen also weiterhin korrekt in der Liste auf, solange sie physisch existieren.
        /// </summary>
        private void RefreshCritterList()
{
    _critterToDisplay.Clear();

    // Statt nur eines Spawners: über alle iterieren und pro Fisch aggregieren
    var counts = new Dictionary<string, (int instantiated, int maxLimit)>();

    foreach (EntityBasicCritterSpawner spawner in CritterSpawnPatch.GetAllActiveSpawners())
    {
        FieldInfo spawnConfigsField = ReflectionHelpers.FindFieldInHierarchy(spawner.GetType(), "spawnConfigs");
        FieldInfo spawnEntriesField = ReflectionHelpers.FindFieldInHierarchy(spawner.GetType(), "spawnEntries");
        if (spawnConfigsField == null || spawnEntriesField == null) continue;

        if (!(spawnConfigsField.GetValue(spawner) is IEnumerable configList)) continue;
        IDictionary spawnEntries = spawnEntriesField.GetValue(spawner) as IDictionary;

        foreach (object spawnConfig in configList)
        {
            if (spawnConfig == null) continue;

            object itemAssetObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "SpawnItemAsset");
            if (!(itemAssetObj is UnityEngine.Object itemAssetUnityObj)) continue;

            string assetName = itemAssetUnityObj.name;
            if (!CritterConfigManager.OverrideCritterConfigs.ContainsKey(assetName)) continue;

            int instantiatedCount = 0;
            if (spawnEntries != null && spawnEntries.Contains(spawnConfig))
            {
                object spawnEntry = spawnEntries[spawnConfig];
                instantiatedCount = ReflectionHelpers.GetCollectionCount(spawnEntry, "InstantiatedSpawns");
            }

            object liveMaxLimitObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "RespawnPopulationMax");
            int maxLimit = liveMaxLimitObj != null ? Mathf.RoundToInt(Convert.ToSingle(liveMaxLimitObj)) : 0;

            if (counts.TryGetValue(assetName, out var existing))
                counts[assetName] = (existing.instantiated + instantiatedCount, existing.maxLimit + maxLimit);
            else
                counts[assetName] = (instantiatedCount, maxLimit);
        }
    }

    foreach (var kvp in counts)
    {
        string cleanName = kvp.Key.Replace("Item_Critter_", "").Replace("_", " ");
        _critterToDisplay.Add($"{cleanName} ({(kvp.Value.maxLimit > 0 ? kvp.Value.instantiated : 0)} / {kvp.Value.maxLimit})");
    }
}

        /// <summary>
        /// Wählt den für die UI relevanten Spawner aus: den räumlich nächsten aktiven
        /// EntityBasicCritterSpawner zur aktuellen Kameraposition (Näherung für die Spielerposition).
        /// Fällt auf null zurück, wenn keine Kamera oder kein aktiver Spawner gefunden wird.
        /// </summary>
        private static EntityBasicCritterSpawner ResolveTargetSpawner()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Plugin.LogDebug("[CritterUIComponent] Camera.main is null, cannot determine nearest spawner.");
                return null;
            }

            return CritterSpawnPatch.GetNearestActiveSpawner(cam.transform.position);
        }

        private void OnGUI()
        {
            if (!IsEnabled || _critterToDisplay.Count == 0) return;

            // Box-Dimensionen festlegen
            float boxWidth = 300f;
            float boxHeight = 45f + (_critterToDisplay.Count * 22f);

            // Perfekt links in der Mitte platziert:
            float posX = 20f;
            float posY = (Screen.height / 2f) - (boxHeight / 2f);

            // Transparenter, dunkler Hintergrund für edlen Look
            Texture2D backgroundTexture = new Texture2D(1, 1);
            backgroundTexture.SetPixel(0, 0, new Color(0.302f, 0.149f, 0.525f, 0.8f));
            backgroundTexture.Apply();

            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = backgroundTexture;

            GUI.Box(new Rect(posX, posY, boxWidth, boxHeight), "", boxStyle);

            // Goldene Überschrift
            GUIStyle headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14,
                alignment = TextAnchor.UpperCenter
            };
            headerStyle.normal.textColor = new Color(0.988f, 0.922f, 0.682f, 1f);

            // Weiße Fisch-Labels
            GUIStyle style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            style.normal.textColor = Color.white;

            // Titel zeichnen
            GUI.Label(new Rect(posX, posY + 10f, boxWidth, 25f), "Critter population:", headerStyle);

            // Fische untereinander auflisten
            float currentY = posY + 38f;
            foreach (string text in _critterToDisplay)
            {
                GUI.Label(new Rect(posX + 15f, currentY, boxWidth - 20f, 22f), text, style);
                currentY += 22f;
            }
        }
    }
}