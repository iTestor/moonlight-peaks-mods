using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace kuri.moonlightpeaks.custommail
{
    public static class MailRewardHelper
    {
        /// <summary>
        /// Fügt einem MailAsset dynamisch zur Laufzeit eine Belohnung (Item oder Rezept) hinzu.
        /// Perfekt für Modding, da keine ItemAssets im Unity-Editor benötigt werden!
        /// </summary>
        /// <param name="mail">Das MailAsset, das die Belohnung erhalten soll.</param>
        /// <param name="itemAssetName">Der exakte interne Name des Items (z.B. "Item_Resource_Coin").</param>
        /// <param name="amount">Anzahl der Items.</param>
        /// <param name="isRecipe">Gibt an, ob es sich um ein Rezept zum Freischalten handelt.</param>
        /// <param name="quality">Qualität des Items ("Regular", "Bronze", "Silver", "Gold", "Perfect").</param>
        public static void AddRewardToMail(MailAsset mail, string itemAssetName, int amount = 1, bool isRecipe = false, ItemQualityLevel quality = ItemQualityLevel.Regular)
        {
            if (mail == null)
            {
                Plugin._logger.LogError("[Reward-API] MailAsset ist null!");
                return;
            }

            try
            {
                // 1. Typen der privaten inneren Klasse via Reflection holen
                Type mailRewardType = typeof(MailAsset).GetNestedType("MailReward", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mailRewardType == null)
                {
                    Plugin._logger.LogError("[Reward-API] Kritischer Fehler: 'MailReward'-Typ wurde im Spiel-Code nicht gefunden!");
                    return;
                }

                // 2. Instanz von MailReward erstellen
                object newReward = Activator.CreateInstance(mailRewardType, true);

                // 3. Properties setzen (Amount, IsRecipe, Quality)
                PropertyInfo amountProp = mailRewardType.GetProperty("Amount", BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo isRecipeProp = mailRewardType.GetProperty("IsRecipe", BindingFlags.Public | BindingFlags.Instance);
                PropertyInfo qualityProp = mailRewardType.GetProperty("Quality", BindingFlags.Public | BindingFlags.Instance);

                amountProp?.SetValue(newReward, amount);
                isRecipeProp?.SetValue(newReward, isRecipe);

                if (qualityProp != null)
                {
                    try
                    {
                        qualityProp.SetValue(newReward, quality);
                    }
                    catch
                    {
                        Plugin._logger.LogWarning($"[Reward-API] Qualität '{quality}' ist ungültig. Nutze Standardwert.");
                    }
                }

                // 4. Das 'ReferencedItem' (DirectorVariableRef) konfigurieren
                var allItems = Asset.GetAll<ItemAsset>(); // Falls ItemAsset die Basisklasse ist
                var targetItem = allItems.FirstOrDefault(i => i != null && i.name == itemAssetName);

                if (targetItem == null)
                {
                    Plugin._logger.LogError($"[Reward-API] Kritischer Fehler: Das Item '{itemAssetName}' existiert nicht in den Spieldaten!");
                    return; // Abbrechen, da das Spiel sonst abstürzt
                }

                // Da 'Item' eine Property mit "private set" ist, holen wir das dahinterliegende Backing-Field,
                // um den Wert absolut sicher hineinzuschreiben.
                FieldInfo itemField = mailRewardType.GetField("<Item>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);

                if (itemField != null)
                {
                    itemField.SetValue(newReward, targetItem);
                }
                else
                {
                    // Fallback, falls der Compiler die Properties nicht via Backing-Fields generiert hat
                    PropertyInfo itemProp = mailRewardType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                    itemProp?.SetValue(newReward, targetItem);
                }

                // 5. In die private Liste 'rewards' des MailAssets eintragen
                FieldInfo rewardsField = typeof(MailAsset).GetField("rewards", BindingFlags.NonPublic | BindingFlags.Instance);
                IList rewardsList = rewardsField?.GetValue(mail) as IList;

                if (rewardsList != null)
                {
                    rewardsList.Add(newReward);
                    Plugin._logger.LogMessage($"[Reward-API] Erfolg: {amount}x '{itemAssetName}' ({quality}) zu '{mail.name}' hinzugefügt!");
                }
                else
                {
                    Plugin._logger.LogError("[Reward-API] Die 'rewards'-Liste des MailAssets konnte nicht geladen werden.");
                }
            }
            catch (Exception ex)
            {
                Plugin._logger.LogError($"[Reward-API] Unerwarteter Fehler beim Injizieren des Rewards: {ex.Message}");
            }
        }
    }
}
