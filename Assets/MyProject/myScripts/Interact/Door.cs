using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// Μια αλληλεπιδραστική πόρτα που περιστρέφεται ανοίγοντας και κλείνοντας όταν ο παίκτης αλληλεπιδρά.
    /// Υποστηρίζει ρυθμιζόμενη γωνία περιστροφής, ταχύτητα και κατεύθυνση pivot.
    ///
    /// Ρύθμιση:
    /// 1. Προσθέστε αυτό το script στο πόρτα GameObject (ή σε ένα parent pivot object)
    /// 2. Προσθέστε ένα Collider component για ανίχνευση raycast
    /// 3. Βάλτε tag στο GameObject ως "Interactable"
    /// 4. Βεβαιωθείτε ότι το pivot point της πόρτας είναι στη θέση του μεντεσέ
    /// 5. Ρυθμίστε τη γωνία περιστροφής και την ταχύτητα στον Inspector
    ///
    /// Σημείωση: Η πόρτα περιστρέφεται γύρω από τον τοπικό Y-άξονα (πάνω). Τοποθετήστε το pivot
    /// στην άκρη του μεντεσέ για ρεαλιστική κίνηση πόρτας.
    public class Door : MonoBehaviour, IInteractable
    {
        [Header("Visual Feedback")]

        /// Προαιρετική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει με την πόρτα.
        [Tooltip("Οπτική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Door Settings")]

        /// Η γωνία σε μοίρες που περιστρέφεται η πόρτα όταν ανοίγει.
        /// Θετικές τιμές περιστρέφουν αντίθετα από τη φορά του ρολογιού (όταν κοιτάς από πάνω).
        /// Τυπικές τιμές: 90 για κανονικές πόρτες, 110 για ευρύ άνοιγμα.
        [Tooltip("Γωνία περιστροφής σε μοίρες όταν η πόρτα ανοίγει.")]
        [SerializeField]
        private float openAngle = 90f;

        /// Πόσο γρήγορα περιστρέφεται η πόρτα σε μοίρες ανά δευτερόλεπτο.
        /// Υψηλότερες τιμές δημιουργούν ταχύτερη κίνηση πόρτας.
        [Tooltip("Ταχύτητα περιστροφής σε μοίρες ανά δευτερόλεπτο.")]
        [SerializeField]
        private float rotationSpeed = 180f;

        /// Ο άξονας γύρω από τον οποίο περιστρέφεται η πόρτα.
        /// Προεπιλογή είναι Vector3.up (Y-άξονας) για οριζόντιες πόρτες με μεντεσέ.
        [Tooltip("Τοπικός άξονας περιστροφής (συνήθως Y-up για πόρτες).")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.up;

        [Header("Audio (Optional)")]

        /// Ήχος που παίζει όταν η πόρτα αρχίζει να ανοίγει.
        [Tooltip("Ήχος που παίζει όταν η πόρτα ανοίγει.")]
        [SerializeField]
        private AudioClip openSound;

        /// Ήχος που παίζει όταν η πόρτα αρχίζει να κλείνει.
        [Tooltip("Ήχος που παίζει όταν η πόρτα κλείνει.")]
        [SerializeField]
        private AudioClip closeSound;

        /// AudioSource component για αναπαραγωγή ήχων πόρτας.
        /// Αν δεν ανατεθεί, προσπαθεί να βρει ένα σε αυτό το GameObject.
        [Tooltip("AudioSource για αναπαραγωγή ήχων. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private AudioSource audioSource;

        /// Παρακολουθεί αν η πόρτα είναι τώρα ανοιχτή ή κλειστή.
        private bool isOpen = false;

        /// Παρακολουθεί αν η πόρτα κινείται αυτή τη στιγμή.
        /// Αποτρέπει πολλαπλές αλληλεπιδράσεις κατά την κίνηση.
        private bool isMoving = false;

        /// Η αρχική περιστροφή της πόρτας, χρησιμοποιείται ως η "κλειστή" κατάσταση.
        private Quaternion closedRotation;

        /// Η περιστροφή-στόχος της πόρτας όταν είναι πλήρως ανοιχτή.
        /// Υπολογίζεται από closedRotation + openAngle.
        private Quaternion openRotation;

        /// Αναφορά στο τρέχον coroutine περιστροφής.
        /// Επιτρέπει διακοπή στη μέση του animation αν χρειαστεί.
        private Coroutine rotationCoroutine;

        /// Αρχικοποιεί τιμές περιστροφής και αποθηκεύει components.
        private void Awake()
        {
            // Αποθήκευση αρχικής (κλειστής) περιστροφής
            closedRotation = transform.localRotation;

            // Υπολογισμός ανοιχτής περιστροφής προσθέτοντας τη γωνία ανοίγματος γύρω από τον άξονα περιστροφής
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            // Προσπάθεια εύρεσης AudioSource αν δεν ανατέθηκε
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        /// Το κείμενο που εμφανίζεται στο interaction prompt.
        /// Αλλάζει ανάλογα με το αν η πόρτα είναι ανοιχτή ή κλειστή.
        public string InteractionPrompt => isOpen ? "Close Door" : "Open Door";

        /// Καλείται όταν ο παίκτης αλληλεπιδρά με την πόρτα.
        /// Εναλλάσσει μεταξύ ανοιχτής και κλειστής κατάστασης.
        /// <param name="interactor">Ο Interactor που ξεκίνησε την αλληλεπίδραση.</param>
        public void OnInteract(Interactor interactor)
        {
            // Μην επιτρέπεις αλληλεπίδραση ενώ η πόρτα κινείται
            if (isMoving)
            {
                return;
            }

            // Εναλλαγή κατάστασης πόρτας
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }

            // Τερμάτισε την αλληλεπίδραση αμέσως (η πόρτα δεν χρειάζεται συνεχή αλληλεπίδραση)
            interactor.EndInteract(this);
        }

        /// Καλείται όταν τελειώνει η αλληλεπίδραση. Δεν χρησιμοποιείται για πόρτες.
        public void OnEndInteract()
        {
            // Οι πόρτες δεν χρειάζονται συνεχή αλληλεπίδραση, άρα δεν υπάρχει τίποτα να καθαριστεί
        }

        /// Καλείται όταν ο παίκτης κοιτάξει αλλού από την πόρτα.
        /// Κρύβει την οπτική ένδειξη.
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// Καλείται όταν ο παίκτης αρχίσει να κοιτάζει την πόρτα.
        /// Εμφανίζει την οπτική ένδειξη.
        public void OnReadyInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        /// Ανοίγει την πόρτα περιστρέφοντας στην ανοιχτή θέση.
        public void Open()
        {
            if (isOpen || isMoving)
            {
                return;
            }

            // Αναπαραγωγή ήχου ανοίγματος
            PlaySound(openSound);

            // Έναρξη coroutine περιστροφής
            rotationCoroutine = StartCoroutine(RotateDoor(openRotation, true));
        }

        /// Κλείνει την πόρτα περιστρέφοντας στην κλειστή θέση.
        public void Close()
        {
            if (!isOpen || isMoving)
            {
                return;
            }

            // Αναπαραγωγή ήχου κλεισίματος
            PlaySound(closeSound);

            // Έναρξη coroutine περιστροφής
            rotationCoroutine = StartCoroutine(RotateDoor(closedRotation, false));
        }

        /// Εναλλάσσει την πόρτα μεταξύ ανοιχτής και κλειστής κατάστασης.
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        /// Περιστρέφει ομαλά την πόρτα στην περιστροφή-στόχο με την πάροδο του χρόνου.
        /// Χρησιμοποιεί Quaternion.RotateTowards για σταθερή γωνιακή ταχύτητα.
        /// <param name="targetRotation">Η περιστροφή προς την οποία περιστρέφεται.</param>
        /// <param name="opening">True αν ανοίγει, false αν κλείνει.</param>
        /// <returns>IEnumerator για εκτέλεση coroutine.</returns>
        private IEnumerator RotateDoor(Quaternion targetRotation, bool opening)
        {
            isMoving = true;

            // Περιστροφή μέχρι να φτάσουμε τον στόχο
            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                // Περιστροφή προς τον στόχο με σταθερή ταχύτητα
                transform.localRotation = Quaternion.RotateTowards(
                    transform.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Κούμπωμα στην ακριβή περιστροφή-στόχο
            transform.localRotation = targetRotation;

            // Ενημέρωση κατάστασης
            isOpen = opening;
            isMoving = false;
        }

        /// Αναπαράγει ένα ηχητικό clip αν υπάρχει διαθέσιμο AudioSource.
        /// <param name="clip">Το audio clip προς αναπαραγωγή.</param>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// Επιστρέφει αν η πόρτα είναι τώρα ανοιχτή.
        public bool IsOpen => isOpen;

        /// Επιστρέφει αν η πόρτα κινείται τώρα.
        public bool IsMoving => isMoving;
    }
}
