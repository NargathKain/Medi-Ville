using UnityEngine;

namespace MyProject.Audio
{
    /// <summary>
    /// Singleton manager for ambient background audio.
    /// Handles playing a looping ambient track and allows volume ducking
    /// when the player enters loud proximity zones (e.g., market, waterfall).
    ///
    /// Setup:
    /// 1. Create an empty GameObject named "AmbientSoundManager"
    /// 2. Add this script and AudioZoneDucker
    /// 3. Add an AudioSource component
    /// 4. Assign an ambient audio clip (birds, wind, village ambience)
    /// 5. Configure baseVolume and minDuckedVolume
    ///
    /// The AudioZoneDucker calls SetDuckAmount() to lower ambient volume
    /// when player is near loud audio zones.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class AmbientSoundManager : MonoBehaviour
    {
        //=============================================================================
        // SINGLETON
        //=============================================================================

        /// <summary>
        /// Singleton instance accessible from anywhere via AmbientSoundManager.Instance
        /// </summary>
        public static AmbientSoundManager Instance { get; private set; }

        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Audio Source")]

        /// <summary>
        /// The AudioSource that plays the looping ambient track.
        /// Should be set to Play On Awake and Loop.
        /// </summary>
        [Tooltip("AudioSource for ambient audio. Auto-found if not assigned.")]
        [SerializeField]
        private AudioSource ambientAudioSource;

        [Header("Volume Settings")]

        /// <summary>
        /// The base volume when no ducking is applied.
        /// This is the "normal" ambient volume.
        /// </summary>
        [Tooltip("Normal ambient volume when not ducked.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float baseVolume = 0.5f;

        /// <summary>
        /// The minimum volume when fully ducked (player is right next to a loud zone).
        /// Ambient audio will lerp between baseVolume and this value.
        /// </summary>
        [Tooltip("Minimum volume when fully ducked by a loud zone.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float minDuckedVolume = 0.1f;

        /// <summary>
        /// How fast the volume transitions when duck amount changes.
        /// Higher values = faster transitions.
        /// </summary>
        [Tooltip("Speed of volume transitions (higher = faster).")]
        [Range(1f, 20f)]
        [SerializeField]
        private float duckLerpSpeed = 5f;

        //=============================================================================
        // PRIVATE FIELDS
        //=============================================================================

        /// <summary>
        /// The current duck amount (0 = no ducking, 1 = fully ducked).
        /// Set by AudioZoneDucker based on proximity to loud zones.
        /// </summary>
        private float currentDuckAmount;

        /// <summary>
        /// Target duck amount we're lerping towards.
        /// Smooths out sudden volume changes.
        /// </summary>
        private float targetDuckAmount;

        /// <summary>
        /// The current actual volume being applied to the AudioSource.
        /// Calculated from baseVolume and duck amount.
        /// </summary>
        private float currentVolume;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        private void Awake()
        {
            // Singleton setup - ensure only one instance exists
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AmbientSoundManager] Duplicate instance found. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Find AudioSource if not assigned
            if (ambientAudioSource == null)
            {
                ambientAudioSource = GetComponent<AudioSource>();
            }

            // Configure AudioSource for ambient playback
            if (ambientAudioSource != null)
            {
                ambientAudioSource.loop = true;
                ambientAudioSource.playOnAwake = true;
                ambientAudioSource.volume = baseVolume;
                currentVolume = baseVolume;

                // Start playing if not already
                if (!ambientAudioSource.isPlaying && ambientAudioSource.clip != null)
                {
                    ambientAudioSource.Play();
                }
            }
            else
            {
                Debug.LogError("[AmbientSoundManager] No AudioSource found!");
            }
        }

        private void Update()
        {
            // Smoothly lerp the duck amount towards target
            currentDuckAmount = Mathf.Lerp(currentDuckAmount, targetDuckAmount, duckLerpSpeed * Time.deltaTime);

            // Calculate target volume based on duck amount
            // Duck amount of 0 = baseVolume, duck amount of 1 = minDuckedVolume
            float targetVolume = Mathf.Lerp(baseVolume, minDuckedVolume, currentDuckAmount);

            // Apply volume to AudioSource
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = targetVolume;
                currentVolume = targetVolume;
            }
        }

        private void OnDestroy()
        {
            // Clear singleton reference when destroyed
            if (Instance == this)
            {
                Instance = null;
            }
        }

        //=============================================================================
        // PUBLIC METHODS
        //=============================================================================

        /// <summary>
        /// Sets the target duck amount. Called by AudioZoneDucker.
        /// </summary>
        /// <param name="amount">Duck amount from 0 (no ducking) to 1 (fully ducked).</param>
        public void SetDuckAmount(float amount)
        {
            // Clamp to valid range
            targetDuckAmount = Mathf.Clamp01(amount);
        }

        /// <summary>
        /// Immediately sets the ambient volume without ducking calculations.
        /// Useful for initialization or cutscenes.
        /// </summary>
        /// <param name="volume">Volume to set (0-1).</param>
        public void SetVolumeImmediate(float volume)
        {
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = Mathf.Clamp01(volume);
                currentVolume = ambientAudioSource.volume;
            }
        }

        //=============================================================================
        // PROPERTIES
        //=============================================================================

        /// <summary>
        /// Gets the current ambient volume (after ducking applied).
        /// </summary>
        public float CurrentVolume => currentVolume;

        /// <summary>
        /// Gets the base (un-ducked) volume.
        /// </summary>
        public float BaseVolume => baseVolume;

        /// <summary>
        /// Gets the current duck amount (0-1).
        /// </summary>
        public float CurrentDuckAmount => currentDuckAmount;
    }
}
