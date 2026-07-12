using System.Collections.Generic;

namespace FishingPlus
{
    /// <summary>
    /// Remembers each SpawnConfig's original (un-overridden) values the first time an override
    /// is ever applied to it, so they can be fully restored later if the player disables the
    /// override again while the room stays loaded (i.e. without a Setup() re-run to reset things
    /// naturally). Without this, toggling a fish's override off left it permanently stuck on
    /// whatever values were last written - it would never respawn again with its real defaults.
    ///
    /// Keyed by spawnConfig reference (not value), since the same override may be toggled on/off
    /// many times for the same SpawnConfig instance during a single room session.
    /// </summary>
    internal static class FishOriginalValueStore
    {
        internal sealed class Snapshot
        {
            internal float RespawnPopulationMax;
            internal object RespawnRarity; // the original, un-cloned RespawnRarity asset reference
            internal float? SpawnStartChance; // null if the SpawnConfig has no SpawnStartBehaviour
        }

        private static readonly Dictionary<object, Snapshot> snapshots =
            new Dictionary<object, Snapshot>(ReferenceEqualityComparer.Instance);

        /// <summary>Stores the original values for this spawnConfig, but only the first time - later calls are no-ops.</summary>
        internal static void CaptureIfMissing(object spawnConfig, float respawnPopulationMax, object respawnRarity, float? spawnStartChance)
        {
            if (spawnConfig == null || snapshots.ContainsKey(spawnConfig))
                return;

            snapshots[spawnConfig] = new Snapshot
            {
                RespawnPopulationMax = respawnPopulationMax,
                RespawnRarity = respawnRarity,
                SpawnStartChance = spawnStartChance
            };
        }

        /// <summary>True if this spawnConfig has had an override applied to it at some point (and therefore has original values on file).</summary>
        internal static bool HasSnapshot(object spawnConfig)
        {
            return spawnConfig != null && snapshots.ContainsKey(spawnConfig);
        }

        internal static bool TryGet(object spawnConfig, out Snapshot snapshot)
        {
            snapshot = null;
            return spawnConfig != null && snapshots.TryGetValue(spawnConfig, out snapshot);
        }
    }
}