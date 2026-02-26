using UnityEngine;

namespace MyProject.Audio
{
    /// Singleton manager για ambient ήχο υπόβαθρου.
    /// Διαχειρίζεται αναπαραγωγή ενός looping ambient κομματιού και επιτρέπει volume ducking
    /// όταν ο παίκτης εισέρχεται σε δυνατές ζώνες εγγύτητας (π.χ., αγορά, καταρράκτης).
    ///
    /// Ρύθμιση:
    /// 1. Δημιουργήστε ένα κενό GameObject με όνομα "AmbientSoundManager"
    /// 2. Προσθέστε αυτό το script και το AudioZoneDucker
    /// 3. Προσθέστε ένα AudioSource component
    /// 4. Αναθέστε ένα ambient audio clip (πουλιά, άνεμος, ατμόσφαιρα χωριού)
    /// 5. Ρυθμίστε baseVolume και minDuckedVolume
    ///
    /// Το AudioZoneDucker καλεί SetDuckAmount() για να μειώσει την ένταση ambient
    /// όταν ο παίκτης είναι κοντά σε δυνατές ζώνες ήχου.
    [RequireComponent(typeof(AudioSource))]
    public class AmbientSoundManager : MonoBehaviour
    {
        /// Singleton instance προσβάσιμο από οπουδήποτε μέσω AmbientSoundManager.Instance
        public static AmbientSoundManager Instance { get; private set; }

        [Header("Audio Source")]

        /// Το AudioSource που παίζει το looping ambient κομμάτι.
        /// Πρέπει να έχει ρυθμιστεί σε Play On Awake και Loop.
        [Tooltip("AudioSource για ambient ήχο. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private AudioSource ambientAudioSource;

        [Header("Volume Settings")]

        /// Η βασική ένταση όταν δεν εφαρμόζεται ducking.
        /// Αυτή είναι η "κανονική" ένταση ambient.
        [Tooltip("Κανονική ένταση ambient όταν δεν γίνεται ducking.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float baseVolume = 0.5f;

        /// Η ελάχιστη ένταση όταν γίνεται πλήρες ducking (ο παίκτης είναι ακριβώς δίπλα σε δυνατή ζώνη).
        /// Ο ambient ήχος κάνει lerp μεταξύ baseVolume και αυτής της τιμής.
        [Tooltip("Ελάχιστη ένταση όταν γίνεται πλήρες ducking από δυνατή ζώνη.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float minDuckedVolume = 0.1f;

        /// Πόσο γρήγορα μεταβαίνει η ένταση όταν αλλάζει το duck amount.
        /// Υψηλότερες τιμές = ταχύτερες μεταβάσεις.
        [Tooltip("Ταχύτητα μεταβάσεων έντασης (υψηλότερο = ταχύτερο).")]
        [Range(1f, 20f)]
        [SerializeField]
        private float duckLerpSpeed = 5f;

        /// Το τρέχον duck amount (0 = χωρίς ducking, 1 = πλήρες ducking).
        /// Ορίζεται από τον AudioZoneDucker βάσει εγγύτητας σε δυνατές ζώνες.
        private float currentDuckAmount;

        /// Το duck amount-στόχος προς το οποίο κάνουμε lerp.
        /// Εξομαλύνει τις απότομες αλλαγές έντασης.
        private float targetDuckAmount;

        /// Η τρέχουσα πραγματική ένταση που εφαρμόζεται στο AudioSource.
        /// Υπολογίζεται από baseVolume και duck amount.
        private float currentVolume;

        private void Awake()
        {
            // Ρύθμιση singleton - διασφαλίζει ότι υπάρχει μόνο ένα instance
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[AmbientSoundManager] Βρέθηκε διπλό instance. Καταστρέφεται αυτό.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Εύρεση AudioSource αν δεν ανατέθηκε
            if (ambientAudioSource == null)
            {
                ambientAudioSource = GetComponent<AudioSource>();
            }

            // Ρύθμιση AudioSource για ambient αναπαραγωγή
            if (ambientAudioSource != null)
            {
                ambientAudioSource.loop = true;
                ambientAudioSource.playOnAwake = true;
                ambientAudioSource.volume = baseVolume;
                currentVolume = baseVolume;

                // Έναρξη αναπαραγωγής αν δεν παίζει ήδη
                if (!ambientAudioSource.isPlaying && ambientAudioSource.clip != null)
                {
                    ambientAudioSource.Play();
                }
            }
            else
            {
                Debug.LogError("[AmbientSoundManager] Δεν βρέθηκε AudioSource!");
            }
        }

        private void Update()
        {
            // Ομαλό lerp του duck amount προς τον στόχο
            currentDuckAmount = Mathf.Lerp(currentDuckAmount, targetDuckAmount, duckLerpSpeed * Time.deltaTime);

            // Υπολογισμός έντασης-στόχου βάσει duck amount
            // Duck amount 0 = baseVolume, duck amount 1 = minDuckedVolume
            float targetVolume = Mathf.Lerp(baseVolume, minDuckedVolume, currentDuckAmount);

            // Εφαρμογή έντασης στο AudioSource
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = targetVolume;
                currentVolume = targetVolume;
            }
        }

        private void OnDestroy()
        {
            // Καθαρισμός αναφοράς singleton όταν καταστραφεί
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// Ορίζει το duck amount-στόχο. Καλείται από τον AudioZoneDucker.
        /// <param name="amount">Duck amount από 0 (χωρίς ducking) έως 1 (πλήρες ducking).</param>
        public void SetDuckAmount(float amount)
        {
            // Περιορισμός σε έγκυρο εύρος
            targetDuckAmount = Mathf.Clamp01(amount);
        }

        /// Ορίζει αμέσως την ένταση ambient χωρίς υπολογισμούς ducking.
        /// Χρήσιμο για αρχικοποίηση ή cutscenes.
        /// <param name="volume">Ένταση προς ρύθμιση (0-1).</param>
        public void SetVolumeImmediate(float volume)
        {
            if (ambientAudioSource != null)
            {
                ambientAudioSource.volume = Mathf.Clamp01(volume);
                currentVolume = ambientAudioSource.volume;
            }
        }

        /// Επιστρέφει την τρέχουσα ένταση ambient (μετά την εφαρμογή ducking).
        public float CurrentVolume => currentVolume;

        /// Επιστρέφει τη βασική (χωρίς ducking) ένταση.
        public float BaseVolume => baseVolume;

        /// Επιστρέφει το τρέχον duck amount (0-1).
        public float CurrentDuckAmount => currentDuckAmount;
    }
}
