using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// Ένας αλληλεπιδραστικός μοχλός που ελέγχει ένα CastleGate (ή άλλο μηχανισμό).
    /// Όταν τραβηχτεί, ο μοχλός περιστρέφεται και ενεργοποιεί τη συνδεδεμένη πύλη να ανέβει/κατέβει.
    ///
    /// Ρύθμιση:
    /// 1. Προσθέστε αυτό το script στο μοχλό GameObject
    /// 2. Προσθέστε ένα Collider για ανίχνευση raycast
    /// 3. Βάλτε tag ως "Interactable"
    /// 4. Αναθέστε το Transform της λαβής του μοχλού (το μέρος που περιστρέφεται)
    /// 5. Αναθέστε την αναφορά CastleGate για έλεγχο
    /// 6. Ρυθμίστε γωνία περιστροφής και ταχύτητα
    ///
    /// Ο μοχλός μπορεί να ελέγξει οποιοδήποτε αντικείμενο με component CastleGate,
    /// ή μπορείτε να τον επεκτείνετε για έλεγχο άλλων μηχανισμών μέσω UnityEvents.
    public class Lever : MonoBehaviour, IInteractable
    {

        [Header("Visual Feedback")]

        /// Προαιρετική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει με τον μοχλό.
        [Tooltip("Οπτική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Lever Handle")]

        /// Το Transform της λαβής του μοχλού που περιστρέφεται όταν τραβιέται.
        /// Αν δεν ανατεθεί, το script θα περιστρέψει αυτό το GameObject.
        [Tooltip("Η λαβή του μοχλού που περιστρέφεται. Χρησιμοποιεί αυτό το αντικείμενο αν δεν ανατεθεί.")]
        [SerializeField]
        private Transform leverHandle;

        /// Η γωνία περιστροφής όταν ο μοχλός τραβιέται (σε μοίρες).
        /// Θετική ή αρνητική ανάλογα με την επιθυμητή κατεύθυνση.
        [Tooltip("Γωνία περιστροφής όταν ο μοχλός τραβιέται (μοίρες).")]
        [SerializeField]
        private float pullAngle = 45f;

        /// Πόσο γρήγορα περιστρέφεται ο μοχλός σε μοίρες ανά δευτερόλεπτο.
        [Tooltip("Ταχύτητα περιστροφής μοχλού (μοίρες ανά δευτερόλεπτο).")]
        [SerializeField]
        private float rotationSpeed = 90f;

        /// Ο άξονας γύρω από τον οποίο περιστρέφεται ο μοχλός.
        /// Προεπιλογή είναι ο X-άξονας (τράβηγμα μπρος/πίσω).
        [Tooltip("Τοπικός άξονας περιστροφής για τον μοχλό.")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.right;

        [Header("Connected Gate")]

        /// Αναφορά στο CastleGate που ελέγχει αυτός ο μοχλός.
        /// Όταν ο μοχλός τραβιέται, η πύλη ανεβαίνει/κατεβαίνει.
        [Tooltip("Το CastleGate που ελέγχει αυτός ο μοχλός.")]
        [SerializeField]
        private CastleGate connectedGate;

        [Header("Behavior")]

        /// Αν είναι true, ο μοχλός εναλλάσσεται μεταξύ τραβηγμένης/μη τραβηγμένης κατάστασης.
        /// Αν είναι false, ο μοχλός επιστρέφει στη μη τραβηγμένη θέση αυτόματα.
        [Tooltip("Αν είναι true, ο μοχλός μένει στην τραβηγμένη θέση μέχρι να τραβηχτεί ξανά.")]
        [SerializeField]
        private bool toggleMode = true;

        /// Καθυστέρηση πριν αρχίσει να κινείται η πύλη μετά το τράβηγμα του μοχλού.
        /// Δημιουργεί πιο ρεαλιστική μηχανική αίσθηση.
        [Tooltip("Καθυστέρηση πριν κινηθεί η πύλη μετά το τράβηγμα του μοχλού (δευτερόλεπτα).")]
        [SerializeField]
        private float gateActivationDelay = 0.2f;

        [Header("Audio (Optional)")]

        /// Ήχος που παίζει όταν ο μοχλός τραβιέται.
        [Tooltip("Ήχος όταν ο μοχλός τραβιέται.")]
        [SerializeField]
        private AudioClip pullSound;

        /// AudioSource για αναπαραγωγή ήχων.
        [Tooltip("AudioSource για ήχους. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private AudioSource audioSource;

        /// Παρακολουθεί αν ο μοχλός είναι στην τραβηγμένη θέση.
        private bool isPulled = false;

        /// Παρακολουθεί αν ο μοχλός κινείται αυτή τη στιγμή.
        private bool isMoving = false;

        /// Η αρχική (μη τραβηγμένη) περιστροφή του μοχλού.
        private Quaternion unpulledRotation;

        /// Η τραβηγμένη περιστροφή του μοχλού.
        private Quaternion pulledRotation;

        /// Αναφορά στο τρέχον coroutine animation.
        private Coroutine animationCoroutine;

        /// Το Transform που πραγματικά περιστρέφεται (λαβή ή εαυτός).
        private Transform rotatingPart;

        /// Αρχικοποιεί περιστροφές μοχλού και επικυρώνει τη ρύθμιση.
        private void Awake()
        {
            // Καθορισμός ποιο transform περιστρέφεται
            rotatingPart = leverHandle != null ? leverHandle : transform;

            // Αποθήκευση αρχικής περιστροφής
            unpulledRotation = rotatingPart.localRotation;

            // Υπολογισμός τραβηγμένης περιστροφής
            pulledRotation = unpulledRotation * Quaternion.AngleAxis(pullAngle, rotationAxis);

            // Εύρεση AudioSource αν δεν ανατέθηκε
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Επικύρωση αναφοράς πύλης
            if (connectedGate == null)
            {
                Debug.LogWarning($"[Lever] {gameObject.name}: Δεν ανατέθηκε CastleGate! " +
                                "Ο μοχλός θα κινείται αλλά δεν θα ελέγχει τίποτα.");
            }
        }

        /// Το prompt που εμφανίζεται στον παίκτη.
        /// Αλλάζει ανάλογα με την κατάσταση μοχλού και πύλης.
        public string InteractionPrompt
        {
            get
            {
                // Μην επιτρέπεις αλληλεπίδραση ενώ ο μοχλός ή η πύλη κινείται
                if (isMoving || (connectedGate != null && connectedGate.IsMoving))
                {
                    return "";
                }

                if (toggleMode)
                {
                    return isPulled ? "Push Lever" : "Pull Lever";
                }
                else
                {
                    return "Pull Lever";
                }
            }
        }

        /// Καλείται όταν ο παίκτης αλληλεπιδρά με τον μοχλό.
        /// Τραβάει (ή σπρώχνει) τον μοχλό και ενεργοποιεί τη συνδεδεμένη πύλη.
        /// <param name="interactor">Ο Interactor που ξεκίνησε την αλληλεπίδραση.</param>
        public void OnInteract(Interactor interactor)
        {
            // Μην επιτρέπεις αλληλεπίδραση ενώ κινείται
            if (isMoving)
            {
                return;
            }

            // Μην επιτρέπεις αλληλεπίδραση ενώ η πύλη κινείται
            if (connectedGate != null && connectedGate.IsMoving)
            {
                return;
            }

            // Εναλλαγή ή τράβηγμα ανάλογα με τη λειτουργία
            if (toggleMode)
            {
                if (isPulled)
                {
                    Push();
                }
                else
                {
                    Pull();
                }
            }
            else
            {
                // Λειτουργία χωρίς toggle: τράβηξε και μετά αυτόματη επιστροφή
                Pull();
            }

            // Τερμάτισε την αλληλεπίδραση αμέσως
            interactor.EndInteract(this);
        }

        /// Καλείται όταν τελειώνει η αλληλεπίδραση.
        public void OnEndInteract()
        {
            // Δεν χρειάζεται καθαρισμός
        }

        /// Καλείται όταν ο παίκτης κοιτάξει αλλού από τον μοχλό.
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// Καλείται όταν ο παίκτης αρχίσει να κοιτάζει τον μοχλό.
        public void OnReadyInteract()
        {
            // Μην εμφανίσεις ένδειξη αν ο μοχλός ή η πύλη κινείται
            if (isMoving || (connectedGate != null && connectedGate.IsMoving))
            {
                return;
            }

            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        /// Τραβάει τον μοχλό στην ενεργοποιημένη θέση.
        public void Pull()
        {
            if (isPulled || isMoving)
            {
                return;
            }

            PlaySound(pullSound);
            animationCoroutine = StartCoroutine(AnimateLever(pulledRotation, true));
        }

        /// Σπρώχνει τον μοχλό πίσω στην απενεργοποιημένη θέση.
        public void Push()
        {
            if (!isPulled || isMoving)
            {
                return;
            }

            PlaySound(pullSound);
            animationCoroutine = StartCoroutine(AnimateLever(unpulledRotation, false));
        }

        /// Κινεί τον μοχλό και ενεργοποιεί την πύλη.
        /// <param name="targetRotation">Περιστροφή-στόχος για τον μοχλό.</param>
        /// <param name="pulling">True αν τραβάει, false αν σπρώχνει.</param>
        /// <returns>IEnumerator για coroutine.</returns>
        private IEnumerator AnimateLever(Quaternion targetRotation, bool pulling)
        {
            isMoving = true;

            // Animation περιστροφής μοχλού
            while (Quaternion.Angle(rotatingPart.localRotation, targetRotation) > 0.1f)
            {
                rotatingPart.localRotation = Quaternion.RotateTowards(
                    rotatingPart.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Κούμπωμα στην ακριβή περιστροφή
            rotatingPart.localRotation = targetRotation;

            // Ενημέρωση κατάστασης
            isPulled = pulling;
            isMoving = false;

            // Αναμονή πριν ενεργοποιηθεί η πύλη (μηχανική καθυστέρηση)
            if (gateActivationDelay > 0)
            {
                yield return new WaitForSeconds(gateActivationDelay);
            }

            // Ενεργοποίηση της συνδεδεμένης πύλης
            ActivateGate(pulling);

            // Για λειτουργία χωρίς toggle, επέστρεψε τον μοχλό στη μη τραβηγμένη θέση μετά το τέλος της πύλης
            if (!toggleMode && pulling)
            {
                // Περίμενε να τελειώσει η κίνηση της πύλης
                if (connectedGate != null)
                {
                    while (connectedGate.IsMoving)
                    {
                        yield return null;
                    }
                }

                // Επιστροφή μοχλού στη μη τραβηγμένη θέση
                yield return new WaitForSeconds(0.5f);
                Push();
            }
        }

        /// Ενεργοποιεί τη συνδεδεμένη πύλη ανάλογα με την κατάσταση του μοχλού.
        /// <param name="pulled">True αν ο μοχλός τραβήχτηκε, false αν σπρώχτηκε.</param>
        private void ActivateGate(bool pulled)
        {
            if (connectedGate == null)
            {
                return;
            }

            if (pulled)
            {
                // Ο μοχλός τραβήχτηκε - ανέβασε την πύλη
                connectedGate.Raise();
            }
            else
            {
                // Ο μοχλός σπρώχτηκε - κατέβασε την πύλη
                connectedGate.Lower();
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

        /// Επιστρέφει αν ο μοχλός είναι τώρα τραβηγμένος.
        public bool IsPulled => isPulled;

        /// Επιστρέφει αν ο μοχλός κινείται τώρα.
        public bool IsMoving => isMoving;
    }
}
