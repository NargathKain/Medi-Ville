using UnityEngine;

namespace MyProject.Audio
{
    /// Δημιουργεί μια "ζώνη ήχου" βασισμένη στη proximity του παίκτη.
    /// Η ένταση αυξάνεται όσο ο παίκτης πλησιάζει στο κέντρο και μειώνεται.
    ///
    /// Ρύθμιση:
    /// 1. Δημιουργήστε ένα κενό GameObject στη θέση του ήχου (κέντρο αγοράς, καταρράκτης, φωτιά κτλ)
    /// 2. Προσθέστε AudioSource και αυτό το component, θέστε το AudioSource σε loop (Play On Awake, Loop)
    /// 3. Προσαρμόστε ακτίνα ζώνης στο AudioSource
    /// 4. AudioSource -> Play On Awake και Loop πρέπει να είναι ενεργά
    /// 5. Ρυθμίστε ακτίνα και μέγιστη ένταση στον Inspector για κάθε ζώνη
    [RequireComponent(typeof(AudioSource))]
    public class ProximityAudioZone : MonoBehaviour
    {
        [Header("Audio Source")]

        /// AudioSource τοπικό που παίζει τον ήχο.
        /// Πρέπει να ρυθμιστεί σε Play On Awake και Loop.
        [Tooltip("AudioSource για αυτή τη ζώνη. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private AudioSource audioSource;

        [Header("Zone Settings")]

        /// Ακτίνα ζώνης ήχου. Ο παίκτης ακούει τον ήχο μόνο μέσα στην ακτίνα.
        /// Η ένταση αυξάνεται προς το κέντρο και μειώνεται.
        [Tooltip("Ακτίνα της ζώνης ήχου. Ο ήχος σβήνει σταδιακά από το κέντρο έως αυτή την απόσταση.")]
        [Range(1f, 100f)]
        [SerializeField]
        private float radius = 15f;

        /// Μέγιστη ένταση στο κέντρο.
        /// Διαφορετική ανάλογα με τον τύπο περιβάλλοντος:
        /// π.χ. 0.9 = δυνατό (αγορά), 0.35 = πιο ήρεμο (φωτιά).
        [Tooltip("Μέγιστη ένταση στο κέντρο της ζώνης. Ρυθμίστε ανά ζώνη (π.χ. 0.9 για αγορά, 0.35 για φωτιά).")]
        [Range(0f, 1f)]
        [SerializeField]
        private float maxVolume = 0.8f;

        [Header("Player Detection")]

        /// Tag για τον παίκτη. Προεπιλογή -> "Player".
        [Tooltip("Tag για αναγνώριση του παίκτη GameObject.")]
        [SerializeField]
        private string playerTag = "Player";

        /// Αποθηκευμένη αναφορά στο Transform του παίκτη
        /// για αποφυγή εύρεσης κάθε frame.
        private Transform playerTransform;

        /// Τρέχουσα αναλογία έντασης (0-1) με βάση την απόσταση του παίκτη.
        private float currentVolumeRatio;

        /// Επιστρέφει την τρέχουσα αναλογία έντασης (0-1) με βάση την απόσταση του παίκτη.
        public float CurrentVolumeRatio => currentVolumeRatio;

        /// Επιστρέφει την ρυθμισμένη ακτίνα ζώνης για τον ήχο.
        public float Radius => radius;

        /// Επιστρέφει τη ρυθμισμένη μέγιστη ένταση στο κέντρο της ζώνης.
        public float MaxVolume => maxVolume;

        private void Awake()
        {
            // Εύρεση AudioSource αν δεν ανατέθηκε
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Ρύθμιση AudioSource για αναπαραγωγή ζώνης
            if (audioSource != null)
            {
                audioSource.loop = true;
                audioSource.playOnAwake = true;
                audioSource.spatialBlend = 0f; // 2D ήχος - διαχειριζόμαστε την ένταση χειροκίνητα
                audioSource.volume = 0f; // Ξεκινά σιωπηλά, το Update θα ορίσει τη σωστή ένταση
            }
            else
            {
                Debug.LogError($"[ProximityAudioZone] {gameObject.name}: Δεν βρέθηκε AudioSource!");
            }
        }

        private void Start()
        {
            // Εύρεση παίκτη με tag
            GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
            }
            else
            {
                Debug.LogWarning($"[ProximityAudioZone] {gameObject.name}: Δεν βρέθηκε GameObject με tag '{playerTag}'!");
            }

            // Διασφάλιση ότι ο ήχος παίζει
            if (audioSource != null && audioSource.clip != null && !audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }

        private void Update()
        {
            // Παράλειψη αν δεν υπάρχει παίκτης ή audio source
            if (playerTransform == null || audioSource == null)
            {
                return;
            }

            // Υπολογισμός απόστασης από τον παίκτη στο κέντρο της ζώνης
            float distance = Vector3.Distance(playerTransform.position, transform.position);

            // Αντιστοίχιση απόστασης σε εύρος 0-1
            // InverseLerp(radius, 0, distance) επιστρέφει:
            // - 0 όταν η απόσταση >= ακτίνα (ο παίκτης στην άκρη ή έξω)
            // - 1 όταν η απόσταση <= 0 (ο παίκτης στο κέντρο)
            // - Τιμές ενδιάμεσα για ενδιάμεσες αποστάσεις
            currentVolumeRatio = Mathf.InverseLerp(radius, 0f, distance);

            // Υπολογισμός πραγματικής έντασης: lerp από 0 έως maxVolume βάσει ratio
            float targetVolume = Mathf.Lerp(0f, maxVolume, currentVolumeRatio);

            // Εφαρμογή έντασης στο AudioSource
            audioSource.volume = targetVolume;
        }

        /// Σχεδιάζει την ακτίνα ζώνης στο Scene view όταν επιλεγεί.
        /// Αλλάζει χρώμα αν ο παίκτης βρίσκεται μέσα, κίτρινο αν είναι έξω.
        private void OnDrawGizmosSelected()
        {
            // Σχεδίαση εξωτερικής ακτίνας
            Gizmos.color = currentVolumeRatio > 0 ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, radius);

            // Σχεδίαση εσωτερικού κύκλου στο 50% της απόστασης έντασης
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Πορτοκαλί
            Gizmos.DrawWireSphere(transform.position, radius * 0.5f);

            // Σχεδίαση κεντρικού σημείου
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position, 0.3f);
        }

        /// Σχεδιάζει πάντα μια ήπια ακτίνα ζώνης στο Scene view ακόμα και όταν δεν είναι επιλεγμένο.
        private void OnDrawGizmos()
        {
            // Σχεδίαση ενός διακριτικού wire sphere ακόμα και όταν δεν είναι επιλεγμένο
            Gizmos.color = new Color(0f, 1f, 0f, 0.2f);
            Gizmos.DrawWireSphere(transform.position, radius);
        }
    }
}
