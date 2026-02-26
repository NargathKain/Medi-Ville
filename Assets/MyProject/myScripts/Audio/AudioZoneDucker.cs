using System.Collections.Generic;
using UnityEngine;

namespace MyProject.Audio
{
    /// <summary>
    /// Monitors all ProximityAudioZone instances and ducks the ambient audio
    /// based on the loudest nearby zone.
    ///
    /// When the player enters a loud audio zone (like a market or waterfall),
    /// this script reduces the ambient background volume so the zone audio
    /// can be heard clearly without everything becoming too loud.
    ///
    /// Setup:
    /// 1. Add this script to the same GameObject as AmbientSoundManager
    /// 2. That's it! Zones are found automatically in Start()
    ///
    /// How it works:
    /// 1. Finds all ProximityAudioZone instances in the scene
    /// 2. Each frame, gets the CurrentVolumeRatio from each zone
    /// 3. Takes the highest ratio (loudest nearby zone)
    /// 4. Tells AmbientSoundManager to duck by that amount
    /// </summary>
    public class AudioZoneDucker : MonoBehaviour
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Zone Detection")]

        /// <summary>
        /// List of all ProximityAudioZones in the scene.
        /// Auto-populated in Start(), but can be manually assigned if needed.
        /// </summary>
        [Tooltip("All audio zones to monitor. Auto-populated in Start().")]
        [SerializeField]
        private List<ProximityAudioZone> audioZones = new List<ProximityAudioZone>();

        [Header("Ducking Settings")]

        /// <summary>
        /// Multiplier applied to the duck amount.
        /// 1.0 = zone at full volume causes full ducking
        /// 0.5 = zone at full volume causes half ducking
        /// Adjust to fine-tune how much zones affect ambient audio.
        /// </summary>
        [Tooltip("Multiplier for duck amount. Lower = less aggressive ducking.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float duckingIntensity = 0.8f;

        /// <summary>
        /// If true, automatically finds all ProximityAudioZones in Start().
        /// If false, you must manually assign zones in the Inspector.
        /// </summary>
        [Tooltip("Auto-find all ProximityAudioZones in the scene on Start.")]
        [SerializeField]
        private bool autoFindZones = true;

        //=============================================================================
        // PRIVATE FIELDS
        //=============================================================================

        /// <summary>
        /// The highest volume ratio among all zones this frame.
        /// Used for debugging and monitoring.
        /// </summary>
        private float currentHighestRatio;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        private void Start()
        {
            // Auto-find all ProximityAudioZones in the scene
            if (autoFindZones)
            {
                FindAllAudioZones();
            }

            // Validate that we have zones to monitor
            if (audioZones.Count == 0)
            {
                Debug.LogWarning("[AudioZoneDucker] No ProximityAudioZones found in scene. " +
                                "Ducking will not occur until zones are added.");
            }
            else
            {
                Debug.Log($"[AudioZoneDucker] Found {audioZones.Count} audio zone(s) to monitor.");
            }

            // Validate AmbientSoundManager exists
            if (AmbientSoundManager.Instance == null)
            {
                Debug.LogWarning("[AudioZoneDucker] AmbientSoundManager not found. " +
                                "Make sure it's in the scene and initializes before this script.");
            }
        }

        private void Update()
        {
            // Skip if no zones to monitor
            if (audioZones.Count == 0)
            {
                return;
            }

            // Find the highest volume ratio among all zones
            // This represents the "loudest" nearby zone
            float highestRatio = 0f;

            for (int i = 0; i < audioZones.Count; i++)
            {
                ProximityAudioZone zone = audioZones[i];

                // Skip null zones (may have been destroyed)
                if (zone == null)
                {
                    continue;
                }

                // Get this zone's current volume ratio
                float ratio = zone.CurrentVolumeRatio;

                // Track the highest ratio
                if (ratio > highestRatio)
                {
                    highestRatio = ratio;
                }
            }

            // Store for debugging
            currentHighestRatio = highestRatio;

            // Apply ducking intensity multiplier
            float duckAmount = highestRatio * duckingIntensity;

            // Tell AmbientSoundManager to duck by this amount
            if (AmbientSoundManager.Instance != null)
            {
                AmbientSoundManager.Instance.SetDuckAmount(duckAmount);
            }
        }

        //=============================================================================
        // ZONE MANAGEMENT
        //=============================================================================

        /// <summary>
        /// Finds all ProximityAudioZones in the scene and adds them to the list.
        /// Called automatically in Start() if autoFindZones is true.
        /// </summary>
        public void FindAllAudioZones()
        {
            // Clear existing list
            audioZones.Clear();

            // Find all ProximityAudioZone components in the scene
            ProximityAudioZone[] foundZones = FindObjectsByType<ProximityAudioZone>(FindObjectsSortMode.None);

            // Add to list
            audioZones.AddRange(foundZones);

            Debug.Log($"[AudioZoneDucker] Found and registered {foundZones.Length} audio zone(s).");
        }

        /// <summary>
        /// Manually registers a new audio zone.
        /// Useful for dynamically spawned zones.
        /// </summary>
        /// <param name="zone">The zone to register.</param>
        public void RegisterZone(ProximityAudioZone zone)
        {
            if (zone != null && !audioZones.Contains(zone))
            {
                audioZones.Add(zone);
                Debug.Log($"[AudioZoneDucker] Registered new zone: {zone.gameObject.name}");
            }
        }

        /// <summary>
        /// Manually unregisters an audio zone.
        /// Call this before destroying a zone to keep the list clean.
        /// </summary>
        /// <param name="zone">The zone to unregister.</param>
        public void UnregisterZone(ProximityAudioZone zone)
        {
            if (audioZones.Contains(zone))
            {
                audioZones.Remove(zone);
                Debug.Log($"[AudioZoneDucker] Unregistered zone: {zone.gameObject.name}");
            }
        }

        /// <summary>
        /// Removes any null entries from the zone list.
        /// Call this if zones have been destroyed without unregistering.
        /// </summary>
        public void CleanupNullZones()
        {
            int removed = audioZones.RemoveAll(zone => zone == null);
            if (removed > 0)
            {
                Debug.Log($"[AudioZoneDucker] Cleaned up {removed} null zone reference(s).");
            }
        }

        //=============================================================================
        // PROPERTIES
        //=============================================================================

        /// <summary>
        /// Gets the current highest volume ratio among all zones.
        /// Useful for debugging or UI display.
        /// </summary>
        public float CurrentHighestRatio => currentHighestRatio;

        /// <summary>
        /// Gets the number of registered audio zones.
        /// </summary>
        public int ZoneCount => audioZones.Count;
    }
}
