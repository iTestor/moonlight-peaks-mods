using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using UnityEngine;

namespace EclipseFramework
{
    /// <summary>
    /// Core management class for handling custom mail assets and rewards within the Eclipse Framework.
    /// Provides methods for third-party modders to load asset bundles and append reward items dynamically.
    /// </summary>
    public class MailSystemManager
    {
        private static readonly List<MailAsset> RegisteredMails = new List<MailAsset>();
        private static readonly Dictionary<string, AssetBundle> LoadedAssetBundles = new Dictionary<string, AssetBundle>();

        private static List<Action> _deferredAddItemActions = new List<Action>();

        private BepInEx.PluginInfo _owner;

        public MailSystemManager(BepInEx.PluginInfo owner)
        {
            _owner = owner;
        }

        /// <summary>
        /// Loads and registers a custom Unity AssetBundle from the specified plugin directory.
        /// </summary>
        /// <param name="bundleName">The exact file name of the AssetBundle to load.</param>
        /// <param name="pluginFolderPath">The absolute directory path containing the bundle (e.g., BepInEx plugin path).</param>
        /// <returns>True if the bundle was loaded successfully or was already registered; otherwise, false.</returns>
        public bool Register(string bundleName)
        {
            string fullPath = Path.Combine(Paths.PluginPath, _owner.Metadata.GUID, bundleName);

            if (!File.Exists(fullPath))
            {
                Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] AssetBundle not found at: {fullPath}");
                return false;
            }

            if (LoadedAssetBundles.ContainsKey(bundleName))
            {
                Debug.LogWarning($"[EclipseFramework][{_owner.Metadata.Name}] AssetBundle '{bundleName}' is already registered.");
                return true;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
                if (bundle == null)
                {
                    Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] Failed to load AssetBundle: {bundleName}");
                    return false;
                }

                LoadedAssetBundles.Add(bundleName, bundle);
                Debug.Log($"[EclipseFramework][{_owner.Metadata.Name}] Successfully registered AssetBundle: {bundleName}");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] Exception while registering AssetBundle: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Dynamically creates a MailReward instance via reflection and adds it to the target MailAsset.
        /// </summary>
        /// <param name="mailAsset">The target MailAsset where the reward will be added.</param>
        /// <param name="itemAssetName">The exact internal name of the ItemAsset (e.g., "Item_Resource_Coin").</param>
        /// <param name="amount">The quantity of the item.</param>
        /// <param name="isRecipe">Determines if the reward is a recipe unlock.</param>
        /// <param name="quality">The quality tier of the item.</param>
        public void AddItem(MailAsset mailAsset, string itemAssetName, int amount = 1, bool isRecipe = false, ItemQualityLevel quality = ItemQualityLevel.Regular)
        {

            var allItems = Asset.GetAll<ItemAsset>();
            var targetItem = allItems?.FirstOrDefault(i => i != null && i.name == itemAssetName);

            if (targetItem == null)
            {
                Debug.Log($"[EclipseFramework][{_owner.Metadata.Name}] Item '{itemAssetName}' not yet found, deferring AddItem...");
                _deferredAddItemActions.Add(() => AddItem(mailAsset, itemAssetName, amount, isRecipe, quality));
                return;
            }

            if (mailAsset == null)
            {
                Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] MailAsset is null!");
                return;
            }

            try
            {
                Type mailRewardType = typeof(MailAsset).GetNestedType("MailReward", BindingFlags.NonPublic | BindingFlags.Instance);
                if (mailRewardType == null)
                {
                    Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] Critical Error: 'MailReward' nested type not found in game code!");
                    return;
                }

                object newReward = Activator.CreateInstance(mailRewardType, true);

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
                        Debug.LogWarning($"[EclipseFramework][{_owner.Metadata.Name}] Quality '{quality}' is invalid. Using default fallback value.");
                    }
                }

                if (targetItem == null)
                {
                    Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] Critical Error: Item '{itemAssetName}' does not exist in the game database!");
                    return;
                }

                FieldInfo itemField = mailRewardType.GetField("<Item>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
                if (itemField != null)
                {
                    itemField.SetValue(newReward, targetItem);
                }
                else
                {
                    PropertyInfo itemProp = mailRewardType.GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                    itemProp?.SetValue(newReward, targetItem);
                }

                FieldInfo rewardsField = typeof(MailAsset).GetField("rewards", BindingFlags.NonPublic | BindingFlags.Instance);
                IList rewardsList = rewardsField?.GetValue(mailAsset) as IList;

                if (rewardsList != null)
                {
                    rewardsList.Add(newReward);
                    Debug.Log($"[EclipseFramework][{_owner.Metadata.Name}] Successfully added {amount}x '{itemAssetName}' ({quality}) to '{mailAsset.name}'!");
                }
                else
                {
                    Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] Failed to retrieve internal 'rewards' collection from MailAsset.");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EclipseFramework][{_owner.Metadata.Name}] Unexpected exception while injecting mail reward: {ex.Message}");
            }
        }

        /// <summary>
        /// Attaches a collection of predefined reward configurations to an active MailAsset.
        /// </summary>
        /// <param name="mailAsset">The target MailAsset where the items will be added.</param>
        /// <param name="mailRewards">The list collection containing the explicit reward parameters.</param>
        public void AddItems(MailAsset mailAsset, List<MailRewardData> mailRewards)
        {
            if (mailAsset == null || mailRewards == null || mailRewards.Count == 0) return;

            foreach (var reward in mailRewards)
            {
                AddItem(mailAsset, reward.ItemAssetName, reward.Amount, reward.IsRecipe, reward.Quality);
            }
        }

        /// <summary>
        /// Attaches a variable number of inline reward configurations to an active MailAsset.
        /// </summary>
        /// <param name="mailAsset">The target MailAsset where the items will be added.</param>
        /// <param name="mailRewards">A comma-separated list or array of explicit reward parameters.</param>
        public void AddItems(MailAsset mailAsset, params MailRewardData[] mailRewards)
        {
            if (mailAsset == null || mailRewards == null || mailRewards.Length == 0) return;

            foreach (var reward in mailRewards)
            {
                AddItem(mailAsset, reward.ItemAssetName, reward.Amount, reward.IsRecipe, reward.Quality);
            }
        }

        /// <summary>
        /// Retrieves a previously registered AssetBundle cache instance by its filename.
        /// </summary>
        /// <param name="bundleName">The filename of the requested AssetBundle.</param>
        /// <returns>The loaded AssetBundle instance if found; otherwise, null.</returns>
        public AssetBundle GetAssetBundle(string bundleName)
        {
            if (LoadedAssetBundles.TryGetValue(bundleName, out AssetBundle bundle))
            {
                return bundle;
            }
            return null;
        }


        /// <summary>
        /// Retrieves a previously registered MailAsset instance by its unique identifier.
        /// </summary>
        /// <param name="mailId">The unique identifier of the requested MailAsset.</param>
        /// <returns>The loaded MailAsset instance if found; otherwise, null.</returns>
        public MailAsset GetMailAsset(string bundleName, string assetName)
        {
            var cachedMail = RegisteredMails.FirstOrDefault(m => m.name == assetName);
            if (cachedMail != null) return cachedMail;

            var bundle = GetAssetBundle(bundleName);
            if (bundle == null) return null;

            var asset = bundle.LoadAsset<MailAsset>(assetName);
            if (asset != null)
            {
                RegisteredMails.Add(asset);
            }
            return asset;
        }

        public static void ProcessDeferredActions()
        {
            if (_deferredAddItemActions.Count > 0 && Asset.GetAll<ItemAsset>() != null && Asset.GetAll<ItemAsset>().Any())
            {
                var actions = new List<Action>(_deferredAddItemActions);
                _deferredAddItemActions.Clear();
                foreach (var action in actions) action.Invoke();
            }
        }

        public void RegisterMailReadAction(MailAsset mailAsset, Action onReadAction)
        {
            if (mailAsset == null) return;

            // Suche nach dem Feld EventOnRead (da es meist privat ist)
            FieldInfo eventField = typeof(MailAsset).GetField("<EventOnRead>k__BackingField", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            EventAsset readEvent = eventField?.GetValue(mailAsset) as EventAsset;

            if (readEvent != null)
            {
                readEvent.AddListener(() =>
                {
                    Debug.Log($"[EclipseFramework][{_owner.Metadata.Name}] Mail '{mailAsset.name}' was read. Execute action...");
                    onReadAction?.Invoke();
                });
            }
            else
            {
                Debug.LogWarning($"[EclipseFramework][{_owner.Metadata.Name}] No EventOnRead in Mail '{mailAsset.name}' found!");
            }
        }
    }

    /// <summary>
    /// Data container structure to pass explicit mail reward configurations safely into the framework API.
    /// </summary>
    public struct MailRewardData
    {
        public string ItemAssetName { get; set; }
        public int Amount { get; set; }
        public bool IsRecipe { get; set; }
        public ItemQualityLevel Quality { get; set; }

        public MailRewardData(string itemAssetName, int amount = 1, bool isRecipe = false, ItemQualityLevel quality = ItemQualityLevel.Regular)
        {
            ItemAssetName = itemAssetName;
            Amount = amount;
            IsRecipe = isRecipe;
            Quality = quality;
        }
    }
}