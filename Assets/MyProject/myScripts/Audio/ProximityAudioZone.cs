using UnityEngine;

namespace MyProject.Audio
{
    /// <summary>
    /// Creates a proximity-based audio zone around a world object.
    /// Volume increases as player gets closer, decreases as they move away.
    ///
    /// Use cases:
    /// - Campfire crackling
    /// - Waterfall/stream sounds
    /// - Market crowd noise
    /// - Blacksmith hammering
    /// - Church bells
    ///
    /// Setup:
    /// 1. Add this script to any world object that should emit sound
    /// 2. Add an AudioSource component (or assign existing one)
    /// 3. Assign an audio clip to the AudioSource
    /// 4. Set AudioSource: Play On Awake = true, Loop = true
    /// 5. Configure radius and maxVolume in Inspector
    ///
    /// The AudioZoneDucker uses CurrentVolumeRatio to duck the ambient audio
    /// when player is near loud zones.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class ProximityAudioZone : MonoBehaviour
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Audio Source")]

        /// <summary>
        /// The AudioSource that plays this zone's audio.
        /// Should be set to Play On Awake and Loop for continuous sounds.
        /// </summary>
        [Tooltip("AudioSource for this zone. Auto-found if not assigned.")]
        [SerializeField]
        private AudioSource audioSource;

        [Header("Zone Settings")]

        /// <summary>
        /// The radius of the audio zone in world units.
        /// Player hears audio at full volume when at center (distance 0),
        /// and volume fades to 0 at this radius distance.
        /// </summary>
        [Tooltip("Radius of the audio zone. Sound fades from center to this distance.")]
        [Range(1f, 100f)]
        [SerializeField]
        private float radius = 15f;

        /// <summary>
        /// The maximum volume when player is at the center of the zone.
        /// Different zones can have different loudness ceilings.
        /// Examples: 0.9 for loud market, 0.35 for subtle fire crackle.
        /// </summary>
        [Tooltip("Maximum volume at zone center. Adjust per zone (e.g., 0.9 for market, 0.35 for fire).")]
        [Range(0f, 1f)]
        [SerializeField]
        private float maxVolume = 0.8f;

        [Header("Player Detection")]

        /// <summary>
        /// Tag used to find the player. Defaults to "Player".
        /// </summary>
        [Tooltip("Tag to identify the player GameObject.")]
        [SerializeField]
        private string playerTag = "Player";

        //=============================================================================
        // PRIVATE FIELDS
        //=============================================================================

        /// <summary>
        /// Cached reference to the player's Transform.
        /// Found automatically in Start() using the player tag.
        /// </summary>
        private Transform playerTransform;

        /// <summary>
        /// The current volume ratio (0-1) based on player distance.
        /// 0 = player outside radius, 1 = player at center.
        /// Used by AudioZoneDucker for ambient ducking calculations.
        /// </summary>
        private float currentVolumeRatio;

        //=============================================================================
        // PROPERTIES
        //=============================================================================

        /// <summary>
        /// Gets the current volume ratio (0-1) based on player proximity.
        /// Used by AudioZoneDucker to determine ambient ducking.
        /// 0 = player outside zone or at edge
        /// 1 = player at zone center
        /// </summary>
        public float CurrentVolumeRatio => currentVolumeRatio;

        /// <summary>
        /// Gets the configured radius of this zone.
        /// </summary>
        public float Radius => radius;

        /// <summary>
        /// Gets the configured max volume of this zone.
        /// </summary>
        public float MaxVolume => maxVolume;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        private void Awake()
        {
            // Find AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Configure AudioSource for zone playback
            if (audioSource != null)
            {
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                audioSource.spatialBlend = 0f; // 2D audio - we handle volume manually
                audioSource.volume = 0f; // Start silent, Update will set correct volume
            }
            else
            {
                Debug.LogError($"[ProximityAudioZone] {gameObject.name}: No AudioSource found!");
            }
        }

        private void Start()
        {
            // Find player by tag
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"[ProximityAudioZone] {gameObject.name}: No GameObject with tag '{playerTag}' found!");
            }

            // Ensure audio is playing
            if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void Update()
        {
            // Skip if no player or audio source
            if (playerTransform == null || audioSource == null)
            {
                return;
            }

            // Calculate distance from player to this zone's center
            float distance = Vector3.Distance(playerTransform.position, transform.position);

            // Map distance to a 0-1 range
            // InverseLerp(radius, 0, distance) returns:
            // - 0 when distance >= radius (player at edge or outside)
            // - 1 when distance <= 0 (player at center)
            // - Values in between for intermediate distances
            currentVolumeRatio = Mathf.InverseLerp(radius, 0f, distance);

            // Calculate actual volume: lerp from 0 to maxVolume based on ratio
            float targetVolume = Mathf.Lerp(0f, maxVolume, currentVolumeRatio);

            // Apply volume to AudioSource
            audioSource.volume = targetVolume;
        }

        //=============================================================================
        // DEBUG VISUALIZATION
        //=============================================================================

        /// <summary>
        /// Draws the zone radius in the Scene view for easy visualization.
        /// Green when player is inside, yellow when outside.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Draw outer radius
            Gizmos.color = currentVolumeRatio > 0 ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);

            // Draw inner circle at 50% volume distance
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange
            Gizmos.DrawWireSphere(transform.position, radius * 0.5f);

            // Draw center point
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }

        /// <summary>
        /// Always draw a small indicator so zones are visible in Scene view.
        /// </summary>
        private void OnDrawGizmos()
        {
            // Draw a subtle wire sphere even when not selected
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
