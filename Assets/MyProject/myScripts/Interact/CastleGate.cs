using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// Μια πύλη κάστρου (portcullis) που ανεβαίνει και κατεβαίνει κάθετα.
    /// Ελέγχεται εξωτερικά από έναν Μοχλό ή άλλο μηχανισμό.
    /// Δεν υλοποιεί απευθείας το IInteractable - χρησιμοποιήστε τον Μοχλό για έλεγχο.
    ///
    /// Ρύθμιση:
    /// 1. Προσθέστε αυτό το script στο πύλη GameObject
    /// 2. Ρυθμίστε το ύψος ανύψωσης και την ταχύτητα κίνησης
    /// 3. Κάντε reference αυτό το component από ένα Lever script
    ///
    /// Η πύλη κινείται κατά μήκος του τοπικού Y-άξονα (πάνω/κάτω).
    public class CastleGate : MonoBehaviour
    {
        [Header("Gate Movement")]

        /// Πόσο ψηλά ανεβαίνει η πύλη όταν ανοίγει (σε τοπικές μονάδες).
        /// Μετρήστε από την κλειστή θέση στην πλήρως ανοιχτή θέση.
        [Tooltip("Ύψος που ανεβαίνει η πύλη όταν ανοίγει (τοπικές μονάδες Y).")]
        [SerializeField]
        private float raiseHeight = 5f;

        /// Ταχύτητα με την οποία κινείται η πύλη (μονάδες ανά δευτερόλεπτο).
        /// Υψηλότερες τιμές δημιουργούν ταχύτερη κίνηση πύλης.
        [Tooltip("Ταχύτητα κίνησης πύλης (μονάδες ανά δευτερόλεπτο).")]
        [SerializeField]
        private float moveSpeed = 2f;

        [Header("Audio (Optional)")]

        /// Ήχος που παίζει όταν η πύλη αρχίζει να κινείται.
        /// Μπορεί να είναι ήχος αλυσίδας/γραναζιού για portcullis.
        [Tooltip("Ήχος που παίζει όταν η πύλη αρχίζει να κινείται.")]
        [SerializeField]
        private AudioClip moveSound;

        /// Ήχος που παίζει όταν η πύλη φτάνει στον προορισμό της.
        /// Μπορεί να είναι ένα βαρύ γδούπο ή κλανκ.
        [Tooltip("Ήχος που παίζει όταν η πύλη σταματά.")]
        [SerializeField]
        private AudioClip stopSound;

        /// Ήχος loop που παίζει ενώ η πύλη κινείται.
        /// Μπορεί να είναι αλυσίδες που κροταλίζουν ή γρανάζια που γυρίζουν.
        [Tooltip("Ήχος loop ενώ η πύλη κινείται.")]
        [SerializeField]
        private AudioClip movingLoopSound;

        /// AudioSource για αναπαραγωγή ήχων.
        [Tooltip("AudioSource για ήχους. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private AudioSource audioSource;


        /// Η αρχική (κλειστή/κατεβασμένη) θέση της πύλης.
        private Vector3 closedPosition;

        /// Η ανυψωμένη (ανοιχτή) θέση της πύλης.
        /// Υπολογίζεται ως closedPosition + (up * raiseHeight).
        private Vector3 raisedPosition;

        /// Παρακολουθεί αν η πύλη είναι τώρα ανυψωμένη (ανοιχτή).
        private bool isRaised = false;

        /// Παρακολουθεί αν η πύλη κινείται τώρα.
        private bool isMoving = false;

        /// Αναφορά στο τρέχον coroutine κίνησης.
        private Coroutine moveCoroutine;


        /// Αρχικοποιεί θέσεις πύλης.
        private void Awake()
        {
            // Αποθήκευση αρχικής (κλειστής) θέσης
            closedPosition = transform.localPosition;

            // Υπολογισμός ανυψωμένης θέσης
            raisedPosition = closedPosition + Vector3.up * raiseHeight;

            // Εύρεση AudioSource αν δεν ανατέθηκε
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }


        /// Ανυψώνει την πύλη στην ανοιχτή θέση.
        /// Καλέστε αυτό από έναν Μοχλό ή διακόπτη.
        public void Raise()
        {
            if (isRaised || isMoving)
            {
                return;
            }

            // Σταμάτησε οποιαδήποτε υπάρχουσα κίνηση
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(MoveGate(raisedPosition, true));
        }

        /// Κατεβάζει την πύλη στην κλειστή θέση.
        /// Καλέστε αυτό από έναν Μοχλό ή διακόπτη.
        public void Lower()
        {
            if (!isRaised || isMoving)
            {
                return;
            }

            // Σταμάτησε οποιαδήποτε υπάρχουσα κίνηση
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(MoveGate(closedPosition, false));
        }

        /// Εναλλάσσει την πύλη μεταξύ ανυψωμένης και κατεβασμένης κατάστασης.
        public void Toggle()
        {
            if (isRaised)
            {
                Lower();
            }
            else
            {
                Raise();
            }
        }

        /// Ορίζει αμέσως την πύλη σε συγκεκριμένη κατάσταση χωρίς animation.
        /// Χρήσιμο για αρχικοποίηση ή cutscenes.
        /// <param name="raised">True για ορισμό ως ανυψωμένη, false για κατεβασμένη.</param>
        public void SetStateImmediate(bool raised)
        {
            // Σταμάτησε οποιαδήποτε τρέχουσα κίνηση
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                isMoving = false;
            }

            // Ορισμός θέσης αμέσως
            transform.localPosition = raised ? raisedPosition : closedPosition;
            isRaised = raised;
        }

        /// Κινεί ομαλά την πύλη στη θέση-στόχο.
        /// <param name="targetPosition">Η θέση προς την οποία κινείται.</param>
        /// <param name="raising">True αν ανυψώνεται, false αν κατεβαίνει.</param>
        /// <returns>IEnumerator για coroutine.</returns>
        private IEnumerator MoveGate(Vector3 targetPosition, bool raising)
        {
            isMoving = true;

            // Αναπαραγωγή ήχου έναρξης
            PlaySound(moveSound);

            // Έναρξη ήχου loop
            if (movingLoopSound != null && audioSource != null)
            {
                audioSource.clip = movingLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Κίνηση προς τον στόχο
            while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Κούμπωμα στην ακριβή θέση
            transform.localPosition = targetPosition;

            // Διακοπή ήχου loop
            if (audioSource != null && audioSource.loop)
            {
                audioSource.loop = false;
                audioSource.Stop();
            }

            // Αναπαραγωγή ήχου διακοπής
            PlaySound(stopSound);

            // Ενημέρωση κατάστασης
            isRaised = raising;
            isMoving = false;
        }

        /// Αναπαράγει ένα ηχητικό clip μία φορά.
        /// <param name="clip">Το clip προς αναπαραγωγή.</param>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// Επιστρέφει αν η πύλη είναι τώρα ανυψωμένη (ανοιχτή).
        public bool IsRaised => isRaised;

        /// Επιστρέφει αν η πύλη κινείται τώρα.
        public bool IsMoving => isMoving;

        /// Σχεδιάζει τις ανοιχτές και κλειστές θέσεις της πύλης στο Scene view.
        private void OnDrawGizmosSelected()
        {
            // Υπολογισμός θέσεων (χρήση τρέχουσας αν δεν είμαστε σε play mode)
            Vector3 closed = Application.isPlaying ? closedPosition : transform.localPosition;
            Vector3 raised = closed + Vector3.up * raiseHeight;

            // Μετατροπή σε world space για σχεδίαση
            Vector3 closedWorld = transform.parent != null
                ? transform.parent.TransformPoint(closed)
                : closed;
            Vector3 raisedWorld = transform.parent != null
                ? transform.parent.TransformPoint(raised)
                : raised;

            // Σχεδίαση κλειστής θέσης
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(closedWorld, Vector3.one * 0.5f);

            // Σχεδίαση ανυψωμένης θέσης
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(raisedWorld, Vector3.one * 0.5f);

            // Σχεδίαση διαδρομής κίνησης
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(closedWorld, raisedWorld);
        }
    }
}
