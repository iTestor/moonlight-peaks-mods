using System;
using System.Collections; // Added for IEnumerable (Reflection)
using System.Collections.Generic;
using System.Linq;
using System.Reflection; // Added for Reflection
using System.Text;
using System.Threading.Tasks;
using UnityEngine; // Added for ScriptableObject

namespace kuri.moonlightpeaks.custommail
{
    public static class AssetSpy
    {
        // === DEIN BESTEHENDER CODE (UNVERÄNDERT) ===
        public static void DumpMomsCakeStyle()
        {
            var momsMail = Asset.GetAll<MailAsset>()
                .FirstOrDefault(m => m.SubjectText.Draft.ToLower().Contains("mom"));

            var allMails = Asset.GetAll<MailAsset>();
            foreach (var mail in allMails)
            {
                if (mail == null)
                {
                    Plugin._logger.LogWarning("[Spy] MailAsset ist null!");
                    return;
                }

                Plugin._logger.LogMessage($"[Spy] === Mail-Dump für: {mail.name} ===");

                // Hier wurde aus kosmetischen Gründen das fehlende Fragezeichen (Null-Conditional) ergänzt,
                // falls die Texte bei manchen Mails leer sind, um Abstürze zu verhindern.
                Plugin._logger.LogMessage($"Mail-SubjectText-Key: {mail.SubjectText?.Key}");
                Plugin._logger.LogMessage($"Mail-SubjectText-Draft: {mail.SubjectText?.Draft}");
                Plugin._logger.LogMessage($"Mail-SubjectText-GetRawDefaultTranslation: {mail.SubjectText?.GetRawDefaultTranslation()}");

                Plugin._logger.LogMessage($"Mail-ContentText-Key: {mail.ContentText?.Key}");
                Plugin._logger.LogMessage($"Mail-ContentText-Draft: {mail.ContentText?.Draft}");
                Plugin._logger.LogMessage($"Mail-ContentText-GetRawDefaultTranslation: {mail.ContentText?.GetRawDefaultTranslation()}");


                Plugin._logger.LogMessage("[Spy] ========================================");


                MailStyleAsset style = mail.MailStyle;
                if (style == null)
                {
                    Plugin._logger.LogWarning($"[Spy] Mail '{mail.name}' hat keinen Style zugewiesen!");
                    return;
                }

                Plugin._logger.LogMessage($"[Spy] === Style-Dump für: {style.name} ===");
                Plugin._logger.LogMessage($"Letter Color (RGBA): {style.LetterColor}");
                Plugin._logger.LogMessage($"Envelope Color (RGBA): {style.EnvelopeColor}");
                Plugin._logger.LogMessage($"Mail Text Color (RGBA): {style.MailTextColor}");
                Plugin._logger.LogMessage($"Letter Frame Color (RGBA): {style.LetterFrameColor}");

                if (style.LetterFrameSpriteReference != null)
                {
                    // Hier holen wir die exakte AssetGUID des Sprites aus dem Addressables-System
                    Plugin._logger.LogMessage($"Letter Frame Sprite AssetGUID: {style.LetterFrameSpriteReference.AssetGUID}");
                    Plugin._logger.LogMessage($"Letter Frame Sprite SubObjectName: {style.LetterFrameSpriteReference.SubObjectName}");
                }

                Plugin._logger.LogMessage("[Spy] ========================================");
            }
        }

        // === MODIFIZIERTE METHODE: REWARDS + SCOPE LOGGEN ===
        public static void DumpMailRewards()
        {
            var allMails = Asset.GetAll<MailAsset>();
            if (allMails == null)
            {
                Plugin._logger.LogWarning("[Spy-Rewards] Keine MailAssets gefunden!");
                return;
            }

            // Holt das private Feld 'rewards' aus der MailAsset-Klasse zur Laufzeit
            FieldInfo rewardsField = typeof(MailAsset).GetField("rewards", BindingFlags.NonPublic | BindingFlags.Instance);

            if (rewardsField == null)
            {
                Plugin._logger.LogError("[Spy-Rewards] Das private Feld 'rewards' konnte nicht gefunden werden!");
                return;
            }

            foreach (var mail in allMails)
            {
                if (mail == null) continue;

                Plugin._logger.LogMessage($"[Spy-Rewards] Anhänge für Mail: {mail.name} ({mail.SubjectText?.Draft})");

                // Holt den Inhalt des privaten Feldes als IEnumerable (da die Liste privat ist)
                var rewardsList = rewardsField.GetValue(mail) as IEnumerable;

                if (rewardsList != null)
                {
                    int rewardIndex = 0;
                    foreach (object rewardObj in rewardsList)
                    {
                        if (rewardObj == null) continue;
                        rewardIndex++;

                        Type rewardType = rewardObj.GetType();

                        // Ruft die Methode 'GetItemAsset()' der privaten Klasse auf
                        MethodInfo getItemAssetMethod = rewardType.GetMethod("GetItemAsset", BindingFlags.Public | BindingFlags.Instance);
                        ScriptableObject itemAsset = getItemAssetMethod?.Invoke(rewardObj, null) as ScriptableObject;

                        // Holt die restlichen Werte (Amount, Quality, IsRecipe) aus den Properties
                        PropertyInfo amountProp = rewardType.GetProperty("Amount", BindingFlags.Public | BindingFlags.Instance);
                        PropertyInfo qualityProp = rewardType.GetProperty("Quality", BindingFlags.Public | BindingFlags.Instance);
                        PropertyInfo isRecipeProp = rewardType.GetProperty("IsRecipe", BindingFlags.Public | BindingFlags.Instance);

                        int amount = amountProp != null ? (int)amountProp.GetValue(rewardObj) : 1;
                        string quality = qualityProp != null ? qualityProp.GetValue(rewardObj)?.ToString() : "Normal";
                        bool isRecipe = isRecipeProp != null ? (bool)isRecipeProp.GetValue(rewardObj) : false;

                        string itemName = itemAsset != null ? itemAsset.name : "Null / Unbekannt";

                        Plugin._logger.LogMessage($"  -> [{rewardIndex}] {amount}x {itemName} | Qualität: {quality} | {(isRecipe ? "REZEPT" : "ITEM")}");

                        // --- SCOPE UND STRING-INJEKTION AUSSPIONIEREN ---
                        PropertyInfo refItemProp = rewardType.GetProperty("ReferencedItem", BindingFlags.Public | BindingFlags.Instance);
                        object refItemObj = refItemProp?.GetValue(rewardObj);

                        if (refItemObj != null)
                        {
                            FieldInfo scopeField = refItemObj.GetType().GetField("scope", BindingFlags.NonPublic | BindingFlags.Instance)
                                                   ?? refItemObj.GetType().GetField("Scope", BindingFlags.Public | BindingFlags.Instance);

                            FieldInfo stringField = refItemObj.GetType().GetField("stringValue", BindingFlags.NonPublic | BindingFlags.Instance)
                                                    ?? refItemObj.GetType().GetField("StringValue", BindingFlags.Public | BindingFlags.Instance);

                            var scopeValue = scopeField?.GetValue(refItemObj);
                            var stringValue = stringField?.GetValue(refItemObj);

                            if (stringValue != null && !string.IsNullOrEmpty(stringValue.ToString()))
                            {
                                Plugin._logger.LogMessage($"     [Scope-Detail] Gelinkte Variable -> Scope-Typ: {scopeValue} | Eingetragener String: '{stringValue}'");
                            }
                        }
                    }

                    if (rewardIndex == 0)
                    {
                        Plugin._logger.LogMessage("  -> Keine Anhänge an dieser Mail.");
                    }
                }
                Plugin._logger.LogMessage("[Spy-Rewards] ----------------------------------------");
            }
        }
    }
}