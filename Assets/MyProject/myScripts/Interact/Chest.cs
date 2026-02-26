using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// Ένα αλληλεπιδραστικό μπαούλο που ανοίγει το καπάκι του όταν ο παίκτης αλληλεπιδρά.
    /// Το καπάκι περιστρέφεται γύρω από ένα σημείο pivot για να προσομοιώσει άνοιγμα/κλείσιμο.
    ///
    /// Ρύθμιση:
    /// 1. Δημιουργήστε ένα μοντέλο μπαούλου με ξεχωριστό αντικείμενο καπάκι ως child
    /// 2. Τοποθετήστε το pivot point του καπακιού στον μεντεσέ (πίσω άκρη του καπακιού)
    /// 3. Προσθέστε αυτό το script στο σώμα του μπαούλου (με το collider)
    /// 4. Αναθέστε την αναφορά Transform του καπακιού
    /// 5. Βάλτε tag στο μπαούλο ως "Interactable"
    ///
    /// Παράδειγμα ιεραρχίας:
    ///   Chest (αυτό το script + collider)
    ///   └── Lid (αναφορά Transform)
    public class Chest : MonoBehaviour, IInteractable
    {
        [Header("Visual Feedback")]

        /// Προαιρετική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει με το μπαούλο.
        [Tooltip("Οπτική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Lid Settings")]

        /// Αναφορά στο Transform του καπακιού που θα περιστραφεί όταν ανοίξει.
        /// Αυτό πρέπει να είναι ένα child object με το pivot του στο σημείο του μεντεσέ.
        [Tooltip("Το Transform του καπακιού που περιστρέφεται όταν το μπαούλο ανοίγει.")]
        [SerializeField]
        private Transform lid;

        /// Η γωνία που περιστρέφεται το καπάκι όταν ανοίγει (σε μοίρες).
        /// Θετικές τιμές συνήθως περιστρέφουν το καπάκι προς τα πίσω.
        [Tooltip("Γωνία περιστροφής όταν το καπάκι ανοίγει (μοίρες).")]
        [SerializeField]
        private float openAngle = -110f;

        /// Πόσο γρήγορα περιστρέφεται το καπάκι σε μοίρες ανά δευτερόλεπτο.
        [Tooltip("Ταχύτητα περιστροφής καπακιού (μοίρες ανά δευτερόλεπτο).")]
        [SerializeField]
        private float rotationSpeed = 180f;

        /// Ο τοπικός άξονας γύρω από τον οποίο περιστρέφεται το καπάκι.
        /// Προεπιλογή είναι ο X-άξονας για καπάκι που ανοίγει προς τα πίσω.
        [Tooltip("Τοπικός άξονας περιστροφής καπακιού.")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.right;

        [Header("Chest Content (Optional)")]

        /// Κείμενο που εμφανίζεται όταν το μπαούλο ανοίγει.
        /// Μπορεί να περιγράφει τι υπάρχει μέσα στο μπαούλο.
        [Tooltip("Προαιρετικό μήνυμα που εμφανίζεται όταν το μπαούλο ανοίγει.")]
        [TextArea(2, 5)]
        [SerializeField]
        private string contentMessage = "";

        /// Αν είναι true, το μπαούλο μπορεί να ανοιχτεί μόνο μία φορά και μένει ανοιχτό.
        [Tooltip("Αν είναι true, το μπαούλο μένει ανοιχτό μόνιμα μετά την πρώτη αλληλεπίδραση.")]
        [SerializeField]
        private bool oneTimeOnly = false;

        [Header("Audio (Optional)")]

        /// Ήχος που παίζει όταν το μπαούλο ανοίγει.
        [Tooltip("Ήχος που παίζει όταν το μπαούλο ανοίγει.")]
        [SerializeField]
        private AudioClip openSound;

        /// Ήχος που παίζει όταν το μπαούλο κλείνει.
        [Tooltip("Ήχος που παίζει όταν το μπαούλο κλείνει.")]
        [SerializeField]
        private AudioClip closeSound;

        /// AudioSource για αναπαραγωγή ήχων.
        [Tooltip("AudioSource για ήχους. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private AudioSource audioSource;

        /// Παρακολουθεί αν το μπαούλο είναι ανοιχτό ή κλειστό.
        private bool isOpen = false;

        /// Παρακολουθεί αν το καπάκι κινείται αυτή τη στιγμή.
        private bool isMoving = false;

        /// Παρακολουθεί αν το μπαούλο έχει ανοιχτεί (για μπαούλα μίας χρήσης).
        private bool hasBeenOpened = false;

        /// Η αρχική (κλειστή) περιστροφή του καπακιού.
        private Quaternion closedRotation;

        /// Η περιστροφή-στόχος του καπακιού όταν είναι ανοιχτό.
        private Quaternion openRotation;

        /// Αναφορά στο τρέχον coroutine animation.
        private Coroutine animationCoroutine;

        /// Αρχικοποιεί περιστροφές καπακιού και επικυρώνει τη ρύθμιση.
        private void Awake()
        {
            // Επικύρωση αναφοράς καπακιού
            if (lid == null)
            {
                Debug.LogError($"[Chest] {gameObject.name}: Δεν έχει ανατεθεί το Transform του καπακιού!");
                enabled = false;
                return;
            }

            // Αποθήκευση κλειστής περιστροφής
            closedRotation = lid.localRotation;

            // Υπολογισμός ανοιχτής περιστροφής
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            // Εύρεση AudioSource αν δεν ανατέθηκε
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        /// Το prompt που εμφανίζεται στον παίκτη.
        /// Δείχνει "Open Chest" ή "Close Chest" ανάλογα με την κατάσταση.
        /// Για μπαούλα μίας χρήσης που είναι ανοιχτά, επιστρέφει κενό για απενεργοποίηση περαιτέρω αλληλεπίδρασης.
        public string InteractionPrompt
        {
            get
            {
                if (oneTimeOnly && hasBeenOpened)
                {
                    return ""; // Δεν είναι δυνατή άλλη αλληλεπίδραση
                }
                return isOpen ? "Close Chest" : "Open Chest";
            }
        }

        /// Καλείται όταν ο παίκτης αλληλεπιδρά με το μπαούλο.
        /// Εναλλάσσει το καπάκι ανοιχτό/κλειστό.
        /// <param name="interactor">Ο Interactor που ξεκίνησε την αλληλεπίδραση.</param>
        public void OnInteract(Interactor interactor)
        {
            // Μην επιτρέπεις αλληλεπίδραση κατά το animation
            if (isMoving)
            {
                return;
            }

            // Μην επιτρέπεις αλληλεπίδραση αν το μπαούλο μίας χρήσης έχει ήδη ανοιχτεί
            if (oneTimeOnly && hasBeenOpened)
            {
                return;
            }

            // Εναλλαγή κατάστασης μπαούλου
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open(interactor);
            }

            // Τερμάτισε την αλληλεπίδραση αμέσως
            interactor.EndInteract(this);
        }

        /// Καλείται όταν τελειώνει η αλληλεπίδραση.
        public void OnEndInteract()
        {
            // Δεν χρειάζεται καθαρισμός - το μπαούλο διαχειρίζεται τη δική του κατάσταση
        }

        /// Καλείται όταν ο παίκτης κοιτάξει αλλού από το μπαούλο.
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// Καλείται όταν ο παίκτης αρχίσει να κοιτάζει το μπαούλο.
        public void OnReadyInteract()
        {
            // Μην εμφανίσεις ένδειξη για μπαούλα μίας χρήσης που είναι ήδη ανοιχτά
            if (oneTimeOnly && hasBeenOpened)
            {
                return;
            }

            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        /// Ανοίγει το καπάκι του μπαούλου.
        /// <param name="interactor">Προαιρετικός interactor για λήψη μηνύματος περιεχομένου όταν ολοκληρωθεί το animation.</param>
        public void Open(Interactor interactor = null)
        {
            if (isOpen || isMoving)
            {
                return;
            }

            PlaySound(openSound);
            animationCoroutine = StartCoroutine(AnimateLid(openRotation, true, interactor));
        }

        /// Κλείνει το καπάκι του μπαούλου.
        public void Close()
        {
            if (!isOpen || isMoving)
            {
                return;
            }

            // Μην επιτρέπεις κλείσιμο μπαούλων μίας χρήσης
            if (oneTimeOnly && hasBeenOpened)
            {
                return;
            }

            PlaySound(closeSound);
            animationCoroutine = StartCoroutine(AnimateLid(closedRotation, false, null));
        }

        /// Κινεί το καπάκι ομαλά με την πάροδο του χρόνου.
        /// <param name="targetRotation">Περιστροφή-στόχος για το καπάκι.</param>
        /// <param name="opening">True αν ανοίγει, false αν κλείνει.</param>
        /// <param name="interactor">Interactor για λήψη μηνύματος περιεχομένου (μπορεί να είναι null).</param>
        /// <returns>IEnumerator για coroutine.</returns>
        private IEnumerator AnimateLid(Quaternion targetRotation, bool opening, Interactor interactor)
        {
            isMoving = true;

            // Animation μέχρι να φτάσει τον στόχο
            while (Quaternion.Angle(lid.localRotation, targetRotation) > 0.1f)
            {
                lid.localRotation = Quaternion.RotateTowards(
                    lid.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Κούμπωμα στην ακριβή περιστροφή
            lid.localRotation = targetRotation;

            // Ενημέρωση κατάστασης
            isOpen = opening;
            isMoving = false;

            // Σημείωση ως ανοιγμένο για μπαούλα μίας χρήσης
            if (opening)
            {
                hasBeenOpened = true;

                // Εμφάνιση μηνύματος περιεχομένου αν υπάρχει
                if (!string.IsNullOrEmpty(contentMessage) && interactor != null)
                {
                    interactor.ReceiveInteract(contentMessage);
                }
            }
        }

        /// Αναπαράγει ένα ηχητικό clip.
        /// <param name="clip">Το clip προς αναπαραγωγή.</param>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// Επιστρέφει αν το μπαούλο είναι τώρα ανοιχτό.
        public bool IsOpen => isOpen;

        /// Επιστρέφει αν το μπαούλο έχει ανοιχτεί τουλάχιστον μία φορά.
        public bool HasBeenOpened => hasBeenOpened;
    }
}
