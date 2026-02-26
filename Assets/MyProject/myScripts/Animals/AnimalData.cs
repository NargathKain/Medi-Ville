using UnityEngine;

namespace MyProject.Animals
{
    /// ScriptableObject που ορίζει τη συμπεριφορά των ζώων (π.χ., ταχύτητα, ακτίνα περιπλάνησης, χρόνοι αδράνειας).
    /// με αυτό μπορούμε να φτιάξουμε διαφορετικά είδη ζώων απλά δημιουργώντας νέα instances του AnimalData στο Unity Editor,
    /// χωρίς να χρειάζεται να τροποποιήσουμε τον κώδικα του AI. Αυτό επιτρέπει εύκολη παραμετροποίηση και επεκτασιμότητα για μελλοντικά είδη ζώων.
    /// δεξί κλικ στο Project window > Create > MyProject > Animals > Animal Data
    [CreateAssetMenu(fileName = "NewAnimalData", menuName = "MyProject/Animals/Animal Data", order = 1)]
    public class AnimalData : ScriptableObject
    {

        [Header("Movement Settings")]

        /// ταχύτητα με την οποία το ζώο κινείται προς τον προορισμό του. 
        /// Αυτή η τιμή εφαρμόζεται στην ιδιότητα speed του NavMeshAgent.
        /// τυπικές τιμές για ζώα που περιπλανιούνται είναι μεταξύ 1 και 3, 
        /// αλλά μπορεί να προσαρμοστεί ανάλογα με το είδος του ζώου (π.χ., ταχύτερα ζώα μπορεί να έχουν υψηλότερη ταχύτητα).
        [Tooltip("Walking speed in units per second. Applied to NavMeshAgent.speed.")]
        [Range(0.5f, 10f)]
        public float moveSpeed = 2f;

        /// Μέγιστη απόσταση από την τρέχουσα θέση για να επιλέξει ένα νέο σημείο περιπλάνησης.
        /// Μεγαλύτερες τιμές επιτρέπουν στα ζώα να περιπλανιούνται σε ευρύτερη περιοχή, ενώ μικρότερες τιμές τα κρατούν πιο κοντά στο σημείο εκκίνησης.
        [Tooltip("Maximum distance from current position to pick a new wander point.")]
        [Range(1f, 50f)]
        public float wanderRadius = 10f;

        /// Γωνιακή ταχύτητα με την οποία το ζώο στρίβει για να αντιμετωπίσει την κατεύθυνση κίνησης.
        /// Εφαρμόζεται στην ιδιότητα angularSpeed του NavMeshAgent. 
        /// Τυπικές τιμές για φυσική κίνηση ζώων είναι μεταξύ 120 και 360.
        [Tooltip("How fast the animal rotates to face movement direction (degrees/second).")]
        [Range(60f, 720f)]
        public float turnSpeed = 120f;

        /// Επιτάχυνση του NavMeshAgent που καθορίζει πόσο γρήγορα το ζώο στο target
        [Tooltip("How quickly the animal accelerates to move speed.")]
        [Range(1f, 20f)]
        public float acceleration = 8f;

        /// Η απόσταση ακινητοποίησης για το NavMeshAgent. 
        /// Όταν το ζώο πλησιάζει τον προορισμό του, θα σταματήσει να κινείται όταν είναι εντός αυτής της απόστασης.
        [Tooltip("Distance from destination at which the animal stops moving.")]
        [Range(0.1f, 2f)]
        public float stoppingDistance = 0.5f;

        [Header("Idle Timing")]

        /// Ελάχιστος χρόνος σε δευτερόλεπτα που το ζώο παραμένει σε κατάσταση αδράνειας πριν επιλέξει νέο προορισμό.
        /// Ο πραγματικός χρόνος αδράνειας θα είναι τυχαίος μεταξύ minIdleTime και maxIdleTime 
        /// για να δημιουργήσει πιο φυσική συμπεριφορά.
        [Tooltip("Minimum seconds to wait in idle state before wandering again.")]
        [Range(0f, 30f)]
        public float minIdleTime = 2f;

        /// Μέγιστος χρόνος σε δευτερόλεπτα που το ζώο παραμένει σε κατάσταση αδράνειας πριν επιλέξει νέο προορισμό.
        /// Υψηλότερες τιμές επιτρέπουν μεγαλύτερα διαστήματα αδράνειας, ενώ χαμηλότερες τιμές κάνουν τα ζώα να περιπλανιούνται πιο συχνά.
        [Tooltip("Maximum seconds to wait in idle state before wandering again.")]
        [Range(0f, 60f)]
        public float maxIdleTime = 8f;

        [Header("Animation Parameters")]

        /// Το όνομα του animation clip που θα παίξει όταν το ζώο κινείται.
        /// για το Ursa Cubic Farm Animals pack: "walk_forward" ή έλεγχος του animator για το ακριβές όνομα του animation clip που αντιστοιχεί στο περπάτημα.
        [Tooltip("Animation clip name for walking. Ursa pack: 'walk_forward'")]
        public string walkTrigger = "walk_forward";

        /// Το όνομα του animation clip που θα παίξει όταν το ζώο είναι σε κατάσταση αδράνειας.
        /// για το Ursa Cubic Farm Animals pack: "idle" ή έλεγχος του animator για το ακριβές 
        /// όνομα του animation clip που αντιστοιχεί στην αδράνεια.
        [Tooltip("Animation clip name for idle. Check your animator for exact name.")]
        public string idleTrigger = "idle";

        [Header("NavMesh Settings")]

        /// Μέγιστη απόσταση για αναζήτηση ενός έγκυρου σημείου NavMesh κατά τη δειγματοληψία.
        /// Εάν το ζώο επιλέξει ένα τυχαίο σημείο που δεν βρίσκεται πάνω στο NavMesh, θα προσπαθήσει
        /// να βρει ένα κοντινό σημείο στο NavMesh εντός αυτής της απόστασης.
        /// αύξηση αν δεν βρουν προορισμούς
        [Tooltip("Max distance to search for valid NavMesh point when sampling random positions.")]
        [Range(1f, 20f)]
        public float navMeshSampleDistance = 5f;

        /// Μέγιστος αριθμός προσπαθειών για να βρει ένα έγκυρο προορισμό NavMesh πριν εγκαταλείψει.
        /// αν δεν βρει θα παραμείνει αδρανές και θα πρσπαθήσει ξανά μετά από κάποιο χρόνο αδράνειας.
        [Tooltip("Maximum attempts to find a valid NavMesh destination before giving up.")]
        [Range(1, 20)]
        public int maxDestinationAttempts = 10;

        /// Καλείται από το Unity Editor όταν τροποποιούνται οι τιμές στο Inspector.
        /// Διασφαλίζει ότι ο ελάχιστος χρόνος αδράνειας δεν υπερβαίνει τον μέγιστο χρόνο αδράνειας,
        private void OnValidate()
        {
            // Ensure min idle time doesn't exceed max idle time
            if (minIdleTime > maxIdleTime)
            {
                minIdleTime = maxIdleTime;
            }
        }
    }
}
