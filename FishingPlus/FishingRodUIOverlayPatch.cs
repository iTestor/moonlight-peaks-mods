using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Chicken.UI;
using Chicken.Utilities;
using EntitySpawner;
using HarmonyLib;
using UnityEngine;

namespace FishingPlus
{
    [HarmonyPatch]
    public static class FishingRodUIOverlayPatch
    {
        private static FishingUIComponent _uiComponent;

        [HarmonyPatch(typeof(PlayerEquipment), "ShowGrabbedItemScreenIfNeeded")]
        [HarmonyPostfix]
        public static void PostfixShowScreen(PlayerEquipment __instance)
        {
            if (__instance.GrabbedItemView == null)
            {
                Plugin._logger.LogDebug("[FishingPlus] ShowGrabbedItemScreenIfNeeded: GrabbedItemView ist null.");
                HideFishingUI();
                return;
            }

            var itemAsset = __instance.GrabbedItemView.ItemAsset;
            if (itemAsset != null && itemAsset.ToolAddon != null)
            {
                var toolTypes = AddressableLibrary<ToolTypeLibrary>.Instance;
                if (toolTypes != null && itemAsset.ToolAddon.ToolType == toolTypes.FishingRod)
                {
                    Plugin._logger.LogDebug("[FishingPlus] Angel aktiv. Zeige eigene UI an...");
                    ShowFishingUI();
                    return;
                }
            }

            HideFishingUI();
        }

        [HarmonyPatch(typeof(PlayerEquipment), "DestroyGrabbedItem")]
        [HarmonyPrefix]
        public static void PrefixDestroy()
        {
            Plugin._logger.LogDebug("[FishingPlus] Gegenstand weggesteckt. Eigene UI ausblenden.");
            HideFishingUI();
        }

        private static void ShowFishingUI()
        {
            if (_uiComponent == null)
            {
                GameObject uiObject = new GameObject("FishingPlus_UI");
                UnityEngine.Object.DontDestroyOnLoad(uiObject);
                _uiComponent = uiObject.AddComponent<FishingUIComponent>();
            }
            _uiComponent.IsEnabled = true;
        }

        private static void HideFishingUI()
        {
            if (_uiComponent != null)
            {
                _uiComponent.IsEnabled = false;
            }
        }
    }

    /// <summary>
    /// Eigene UI-Komponente, die sich links perfekt in der Mitte platziert.
    /// </summary>
    public class FishingUIComponent : MonoBehaviour
    {
        public bool IsEnabled { get; set; } = false;
        private List<string> _fishToDisplay = new List<string>();
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
                RefreshFishList();
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[FishingUIComponent] Fehler beim Aktualisieren der Fischliste: {ex}");
            }
        }

        /// <summary>
        /// Liest den aktuell aktiven EntityFishSpawner aus und befüllt _fishToDisplay mit jedem
        /// bekannten Fisch, der gerade tatsächlich im Bereich instanziiert ist (InstantiatedSpawns
        /// pro SpawnConfig). Das funktioniert bewusst auch bei MaxLimit=0: der Limit-Patch
        /// verhindert nur NEUE Spawns, entfernt aber keine bereits vorhandenen Fische - die
        /// tauchen also weiterhin korrekt in der Liste auf, solange sie physisch existieren.
        /// </summary>
        private void RefreshFishList()
        {
            _fishToDisplay.Clear();

            EntityFishSpawner spawner = ResolveTargetSpawner();
            if (spawner == null)
            {
                Plugin.LogDebug("[FishingUIComponent] No active EntityFishSpawner found nearby, list will remain empty.");
                return;
            }

            FieldInfo spawnConfigsField = ReflectionHelpers.FindFieldInHierarchy(spawner.GetType(), "spawnConfigs");
            FieldInfo spawnEntriesField = ReflectionHelpers.FindFieldInHierarchy(spawner.GetType(), "spawnEntries");

            if (spawnConfigsField == null || spawnEntriesField == null)
            {
                Plugin.LogDebug("[FishingUIComponent] Field 'spawnConfigs' or 'spawnEntries' not found, cannot populate list.");
                return;
            }

            if (!(spawnConfigsField.GetValue(spawner) is IEnumerable configList))
            {
                Plugin.LogDebug("[FishingUIComponent] 'spawnConfigs' is not iterable or null.");
                return;
            }

            IDictionary spawnEntries = spawnEntriesField.GetValue(spawner) as IDictionary;

            foreach (object spawnConfig in configList)
            {
                if (spawnConfig == null)
                    continue;

                object itemAssetObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "SpawnItemAsset");
                if (!(itemAssetObj is UnityEngine.Object itemAssetUnityObj))
                    continue;

                string assetName = itemAssetUnityObj.name;

                // Nur Fische anzeigen, die dieses Plugin überhaupt kennt/verwaltet.
                if (!FishConfigManager.OverrideFishConfigs.ContainsKey(assetName))
                    continue;

                int instantiatedCount = 0;
                if (spawnEntries != null && spawnEntries.Contains(spawnConfig))
                {
                    object spawnEntry = spawnEntries[spawnConfig];
                    instantiatedCount = ReflectionHelpers.GetCollectionCount(spawnEntry, "InstantiatedSpawns");
                }

                // Bewusst das ROH-konfigurierte MaxLimit (nicht GetEffectiveMaxLimit) als Nenner:
                // GetEffectiveMaxLimit zwingt bei SpawnChance=0 auf 0, was hier eine
                // Division-durch-0/undefinierte Anzeige erzeugen würde, obwohl noch
                // "übrig gebliebene" Fische von vor der Sperre sichtbar sein sollen.
                object liveMaxLimitObj = ReflectionHelpers.GetPropertyValue(spawnConfig, "RespawnPopulationMax");
                int maxLimit = liveMaxLimitObj != null ? Mathf.RoundToInt(Convert.ToSingle(liveMaxLimitObj)) : 0;

                // PERCENT% = der LIVE-Wert auf dem spawnConfig-Objekt selbst
                // (RespawnRarity.RespawnIntervalChance), NICHT der Wert aus dem Config-Menü.
                // Diese beiden können auseinanderlaufen: FishSpawnPatch/
                // FishRespawnPopulationMaxSyncPatch schreiben den Config-Wert erst live auf das
                // Objekt, wenn der Override aktiv ist bzw. beim nächsten Tick nachgezogen wird -
                // und bei deaktiviertem Override steht dort der vanilla-Originalwert, nicht der
                // (dann irrelevante) Konfig-Wert. Wir lesen also exakt das, was UpdateSpawns()
                // tatsächlich für den Respawn-Roll verwendet.
                object respawnRarity = ReflectionHelpers.GetPropertyValue(spawnConfig, "RespawnRarity");
                float liveChance01 = 0f;
                if (respawnRarity != null)
                {
                    object liveChanceObj = ReflectionHelpers.GetPropertyValue(respawnRarity, "RespawnIntervalChance");
                    if (liveChanceObj != null)
                        liveChance01 = Convert.ToSingle(liveChanceObj);
                }

                string cleanName = assetName.Replace("Item_Fish_", "").Replace("_", " ");
                _fishToDisplay.Add($"{cleanName} ({instantiatedCount} / {maxLimit})");
            }

            Plugin.LogDebug($"[FishingUIComponent] List refreshed: {_fishToDisplay.Count} (Spawner InstanceID: {spawner.GetInstanceID()}).");
        }

        /// <summary>
        /// Wählt den für die UI relevanten Spawner aus: den räumlich nächsten aktiven
        /// EntityFishSpawner zur aktuellen Kameraposition (Näherung für die Spielerposition).
        /// Fällt auf null zurück, wenn keine Kamera oder kein aktiver Spawner gefunden wird.
        /// </summary>
        private static EntityFishSpawner ResolveTargetSpawner()
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Plugin.LogDebug("[FishingUIComponent] Camera.main is null, cannot determine nearest spawner.");
                return null;
            }

            return FishSpawnPatch.GetNearestActiveSpawner(cam.transform.position);
        }

        private void OnGUI()
        {
            if (!IsEnabled || _fishToDisplay.Count == 0) return;

            // Box-Dimensionen festlegen
            float boxWidth = 300f;
            float boxHeight = 45f + (_fishToDisplay.Count * 22f);

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
            GUIStyle fishStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            fishStyle.normal.textColor = Color.white;

            // Titel zeichnen
            GUI.Label(new Rect(posX, posY + 10f, boxWidth, 25f), "Fish population:", headerStyle);

            // Fische untereinander auflisten
            float currentY = posY + 38f;
            foreach (string fishText in _fishToDisplay)
            {
                GUI.Label(new Rect(posX + 15f, currentY, boxWidth - 20f, 22f), fishText, fishStyle);
                currentY += 22f;
            }
        }
    }
}