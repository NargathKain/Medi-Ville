using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace MyProject.Animals
{
    /// Έλεγχος συμπεριφοράς περιπλάνησης για ζώα με μηχανή 2 καταστάσεωψν (Idle, Walking).
    /// Ζώο -> εναλάσσεται μεταξύ αδράνειας (Idle) και περιπλάνησης (Walking) για τυχαιο χρονικό διάστημα
    /// με επιλογή ενός τυχαίου προορισμού εντός καθορισμένης ακτίνας. Χρησιμοποιεί NavMeshAgent για κίνηση 
    /// και Animator για ομαλή μετάβαση μεταξύ idle/walk animations.
    ///
    /// Requirements:
    /// - NavMeshAgent component στο GameObject
    /// - Animator component στο GameObject (ή στο child)
    /// - GameObject πρέπει να τοποθετηθεί πάνω σε baked NavMesh
    /// - AnimalData ScriptableObject 
    /// State Machine Flow: [Idle] ---(after random time)---> [Walking] ---(on arrival)---> [Idle]
    [RequireComponent(typeof(NavMeshAgent))]
    public class AnimalController : MonoBehaviour
    {

        [Header("Data Configuration")]

        /// Αναφορά στο ScriptableObject AnimalData που περιέχει τις ρυθμίσεις συμπεριφοράς
        /// πρέπει να ανατεθεί στο Inspector με ένα instance του AnimalData
        [Tooltip("Reference to the AnimalData ScriptableObject containing behavior settings.")]
        [SerializeField]
        private AnimalData animalData;

        [Header("Component References")]

        /// Αναφορά στο Animator component για την αναπαραγωγή των animations.
        [Tooltip("Animator component for playing animations. Auto-found if not assigned.")]
        [SerializeField]
        private Animator animator;

        [Header("Debug Settings")]

        /// Όταν ενεργοποιημένο, σχεδιάζει gizmos στο Scene view για να δείξει την ακτίνα περιπλάνησης και τον τρέχοντα προορισμό.
        [Tooltip("Enable to visualize wander radius and destination in Scene view.")]
        [SerializeField]
        private bool showDebugGizmos = true;

        /// Αναφορά στο NavMeshAgent component για τον έλεγχο της κίνησης του ζώου.
        private NavMeshAgent navMeshAgent;

        /// τρέχον σημείο προορισμού που το ζώο προσπαθεί να φτάσει. 
        /// Χρησιμοποιείται για debug drawing και λογική άφιξης.
        private Vector3 currentDestination;

        /// δείχνει αν το coroutine της μηχανής κατάστασης τρέχει αυτή τη στιγμή.
        /// χρήσιμο για να μην τρέχουνε πολλαπλά coroutines
        private bool isStateMachineRunning;

        /// αναφορά στο τρέχον coroutine της μηχανής κατάστασης
        private Coroutine stateMachineCoroutine;

        /// Ορίζει τις πιθανές καταστάσεις για το state machine
        private enum AnimalState
        {
            /// Animal is standing still, waiting before selecting a new destination.
            Idle,

            /// Animal is actively moving towards a destination point.
            Walking
        }

        /// Παρακολουθεί την τρέχουσα κατάσταση του ζώου για εντοπισμό σφαλμάτων και λογική κατάστασης.
        private AnimalState currentState = AnimalState.Idle;

        /// Καλείται κατά τη φόρτωση του script 
        /// αποθηκεύει προσωρινά τις απαιτούμενες αναφορές στοιχείων και επικυρώνει την εγκατάσταση
        private void Awake()
        {
            // Cache the NavMeshAgent component - required for movement
            navMeshAgent = GetComponent<NavMeshAgent>();

            // Attempt to find Animator if not manually assigned
            // First checks this GameObject, then searches children
            if (animator == null)
            {
                animator = GetComponent<Animator>();

                // If still not found, search in children (common setup for character models)
                if (animator == null)
                {
                    animator = GetComponentInChildren<Animator>();
                }
            }

            // Validate that required components and data are present
            ValidateSetup();
        }

        /// Καλείται όταν γίνει enabled και active
        private void OnEnable()
        {
            // Start the state machine when the component is enabled
            StartStateMachine();
        }

        /// καλείται όταν γίνει disabled ή καταστραφεί
        private void OnDisable()
        {
            // Stop the state machine when the component is disabled
            StopStateMachine();
        }

        /// καλείται κάθε καρέ για να ενημερώσει τον animator με βάση την τρέχουσα ταχύτητα κίνησης.
        private void Update()
        {
            // Sync animator with current movement speed
            UpdateAnimator();
        }

        /// Καλείται όταν φορτώνεται το script ή αλλάζει μια τιμή στον Inspector.
        /// Χρησιμοποιείται για την εφαρμογή ρυθμίσεων NavMeshAgent από το AnimalData σε edit mode.
        private void OnValidate()
        {
            // Apply settings when values change in Inspector (editor only)
            if (animalData != null && navMeshAgent != null)
            {
                ApplyNavMeshAgentSettings();
            }
        }

        /// Αρχικοποιεί και ξεκινά coroutine του state machine. 
        /// Εφαρμόζει τις ρυθμίσεις του NavMeshAgent από το στοιχείο AnimalData.
        private void StartStateMachine()
        {
            // Prevent starting if already running or if data is missing
            if (isStateMachineRunning || animalData == null)
            {
                return;
            }

            // Configure the NavMeshAgent with settings from our data asset
            ApplyNavMeshAgentSettings();

            // Store the starting position as initial destination (for debug drawing)
            currentDestination = transform.position;

            // Start the main state machine coroutine
            stateMachineCoroutine = StartCoroutine(StateMachineCoroutine());
            isStateMachineRunning = true;
        }

        /// Σταματά το coroutineκαι διακόπτει κάθε κίνηση.
        /// Καλείται όταν το στοιχείο απενεργοποιηθεί ή καταστραφεί.
        private void StopStateMachine()
        {
            // Stop the coroutine if it's running
            if (stateMachineCoroutine != null)
            {
                StopCoroutine(stateMachineCoroutine);
                stateMachineCoroutine = null;
            }

            isStateMachineRunning = false;

            // Stop the NavMeshAgent to prevent continued movement
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;
            }
        }

        /// Εφαρμόζει όλες τις ρυθμίσεις που σχετίζονται με την κίνηση από το AnimalData στο NavMeshAgent.
        /// Καλείται κατά την εκκίνηση και όταν αλλάζουν οι ρυθμίσεις στον Inspector.
        private void ApplyNavMeshAgentSettings()
        {
            navMeshAgent.speed = animalData.moveSpeed;
            navMeshAgent.angularSpeed = animalData.turnSpeed;
            navMeshAgent.acceleration = animalData.acceleration;
            navMeshAgent.stoppingDistance = animalData.stoppingDistance;
        }

        /// Main state machine coroutine που εκτελείται συνεχώς ενώ το στοιχείο είναι ενεργοποιημένο.
        /// Εναλλάσσεται μεταξύ καταστάσεων αδράνειας και βάδισης με βάση τον χρόνο και τις συνθήκες άφιξης.
        /// Flow:
        /// 1. Enter Idle state, wait for random duration
        /// 2. Pick random destination, transition to Walking state
        /// 3. Wait until arrival at destination
        /// 4. Return to step 1
        /// <returns>IEnumerator for coroutine execution</returns>
        private IEnumerator StateMachineCoroutine()
        {
            // Ensure we start in idle state
            EnterIdleState();

            // Main state machine loop - runs indefinitely while component is enabled
            while (true)
            {
                switch (currentState)
                {
                    case AnimalState.Idle:
                        // Execute idle state behavior and wait for it to complete
                        yield return StartCoroutine(IdleStateCoroutine());
                        break;

                    case AnimalState.Walking:
                        // Execute walking state behavior and wait for it to complete
                        yield return StartCoroutine(WalkingStateCoroutine());
                        break;
                }

                // Small yield to prevent infinite loop issues (safety measure)
                yield return null;
            }
        }

        /// Χειρίζεται τη συμπεριφορά της κατάστασης αδράνειας.
        /// Το ζώο παραμένει ακίνητο για ένα τυχαίο χρονικό διάστημα μεταξύ minIdleTime και maxIdleTime,
        /// στη συνέχεια μεταβαίνει στην κατάσταση Walking.
        /// <returns>IEnumerator για εκτέλεση corutine</returns>
        private IEnumerator IdleStateCoroutine()
        {
            // Calculate a random idle duration within the configured range
            float idleDuration = Random.Range(animalData.minIdleTime, animalData.maxIdleTime);

            // Log for debugging purposes
            if (showDebugGizmos)
            {
                Debug.Log($"[{gameObject.name}] Entering Idle state for {idleDuration:F1} seconds");
            }

            // Wait for the calculated idle duration
            yield return new WaitForSeconds(idleDuration);

            // After waiting, attempt to find a destination and transition to walking
            if (TrySetRandomDestination())
            {
                // Successfully found a valid destination, transition to walking
                EnterWalkingState();
            }
            else
            {
                // Failed to find valid destination, remain in idle and try again
                // (The loop will restart the idle coroutine)
                if (showDebugGizmos)
                {
                    Debug.LogWarning($"[{gameObject.name}] Failed to find valid destination, remaining idle");
                }
            }
        }

        /// Μεταφέρει το ζώο σε κατάσταση αδράνειας. 
        /// Διακόπτει την κίνηση - ο animator παίζει αυτόματα 
        /// σε κατάσταση αδράνειας όταν η ταχύτητα είναι 0.
        private void EnterIdleState()
        {
            currentState = AnimalState.Idle;

            // Stop any current movement
            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                navMeshAgent.isStopped = true;
            }

            // Animator will automatically play idle when Speed = 0
        }

        /// Χειρίζεται τη συμπεριφορά της κατάστασης βάδισης.
        /// Το ζώο κινείται προς τον τρέχοντα προορισμό και παρακολουθεί την άφιξή του.
        /// Μόλις φτάσει (ή εάν η διαδρομή γίνει άκυρη), επιστρέφει στην κατάσταση αδράνειας.
        /// <returns>IEnumerator για εκτέλεση συνρουτίνας</returns>
        private IEnumerator WalkingStateCoroutine()
        {
            // Log for debugging purposes
            if (showDebugGizmos)
            {
                Debug.Log($"[{gameObject.name}] Walking to destination: {currentDestination}");
            }

            // Continue walking until we've arrived at the destination
            while (!HasArrivedAtDestination())
            {
                // Check if the path has become invalid (e.g., destination became unreachable)
                if (!navMeshAgent.hasPath && !navMeshAgent.pathPending)
                {
                    // Path was lost, return to idle
                    if (showDebugGizmos)
                    {
                        Debug.LogWarning($"[{gameObject.name}] Path lost, returning to idle");
                    }
                    break;
                }

                // Wait until next frame before checking again
                yield return null;
            }

            // Arrived at destination (or path was lost), return to idle
            EnterIdleState();
        }

        /// Μεταβαίνει το ζώο στην κατάσταση Βάδισης.
        /// Ενεργοποιεί την κίνηση - ο animator αναπαράγει αυτόματα το 
        /// περπάτημα όταν η Ταχύτητα > 0.
        private void EnterWalkingState()
        {
            currentState = AnimalState.Walking;

            // Enable movement on the NavMeshAgent
            navMeshAgent.isStopped = false;

            // Animator will automatically play walk when Speed > 0
        }

        /// Ελέγχει εάν το ζώο έχει φτάσει στον τρέχοντα προορισμό του.
        /// Χρησιμοποιεί την υπολειπόμενη απόσταση και την κατάσταση της διαδρομής του NavMeshAgent για να προσδιορίσει την άφιξη.
        /// <returns>True εάν το ζώο έχει φτάσει στον προορισμό, false διαφορετικά</returns>
        private bool HasArrivedAtDestination()
        {
            // Must have a path and not be calculating one
            if (navMeshAgent.pathPending)
            {
                return false;
            }

            // Check if remaining distance is within stopping distance
            // Also verify the agent has a valid path
            return navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance
                   && navMeshAgent.hasPath;
        }

        /// Προσπαθεί να βρει έναν έγκυρο τυχαίο προορισμό εντός της ακτίνας περιπλάνησης.
        /// Χρησιμοποιεί το NavMesh.SamplePosition για να διασφαλίσει ότι ο προορισμός βρίσκεται σε μια επιφάνεια που μπορεί να περπατηθεί.
        /// Κάνει πολλαπλές προσπάθειες εάν το πρώτο τυχαίο σημείο δεν είναι έγκυρο.
        /// <returns>True εάν βρέθηκε και ορίστηκε ένας έγκυρος προορισμός, false διαφορετικά</returns>
        private bool TrySetRandomDestination()
        {
            // Make multiple attempts to find a valid destination
            for (int attempt = 0; attempt < animalData.maxDestinationAttempts; attempt++)
            {
                // Generate a random point within a sphere around the current position
                Vector3 randomDirection = Random.insideUnitSphere * animalData.wanderRadius;

                // Add to current position to get world-space target
                randomDirection += transform.position;

                // Try to find the nearest valid point on the NavMesh
                NavMeshHit navMeshHit;
                bool foundValidPoint = NavMesh.SamplePosition(
                    randomDirection,
                    out navMeshHit,
                    animalData.navMeshSampleDistance,
                    NavMesh.AllAreas
                );

                if (foundValidPoint)
                {
                    // Found a valid NavMesh point, set it as our destination
                    currentDestination = navMeshHit.position;

                    // Tell the NavMeshAgent to move to this destination
                    navMeshAgent.SetDestination(currentDestination);

                    return true;
                }
            }

            // Failed to find a valid destination after all attempts
            return false;
        }

        /// Ενημερώνει το animator με βάση την τρέχουσα κίνηση. 
        /// Χρησιμοποιεί την παράμετρο Speed ​​για αυτόματη ανάμειξη ρελαντί/βάδισμα.
        private void UpdateAnimator()
        {
            if (animator == null)
            {
                return;
            }

            // Get current speed from NavMeshAgent velocity
            float speed = navMeshAgent.velocity.magnitude;

            // Set Speed parameter - most Animator Controllers use this
            animator.SetFloat("Speed", speed);
        }

        /// Παλαιότερη μέθοδος για άμεσο έλεγχο κίνησης.
        /// Διατηρείται για συμβατότητα, αλλά προτιμάται η UpdateAnimator().
        private void TriggerAnimation(string animationName)
        {
            if (animator == null || string.IsNullOrEmpty(animationName))
            {
                return;
            }

            animator.Play(animationName);
        }

        /// Επιβεβαιώνει ότι όλα τα απαιτούμενα στοιχεία και οι αναφορές έχουν ρυθμιστεί σωστά.
        /// Καταγράφει προειδοποιήσεις για τυχόν ελλείπουσες ή μη έγκυρες ρυθμίσεις.
        private void ValidateSetup()
        {
            // Check for required AnimalData reference
            if (animalData == null)
            {
                Debug.LogError($"[{gameObject.name}] AnimalData is not assigned! " +
                              "Please assign an AnimalData ScriptableObject in the Inspector.");
            }

            // Check for NavMeshAgent (should always be present due to RequireComponent)
            if (navMeshAgent == null)
            {
                Debug.LogError($"[{gameObject.name}] NavMeshAgent component is missing!");
            }

            // Warn if no Animator is found (animations won't play, but movement will work)
            if (animator == null)
            {
                Debug.LogWarning($"[{gameObject.name}] No Animator component found. " +
                                "Animations will not play, but wandering behavior will still work.");
            }
        }

        /// Σχεδιάζει εργαλεία εντοπισμού σφαλμάτων στην προβολή Σκηνής για να απεικονίσει την ακτίνα περιπλάνησης και το τρέχον σημείο προορισμού. 
        /// Σχεδιάζεται μόνο όταν είναι ενεργοποιημένο το showDebugGizmos.
        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos)
            {
                return;
            }

            // Draw the wander radius as a wire sphere
            if (animalData != null)
            {
                Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // Semi-transparent green
                Gizmos.DrawWireSphere(transform.position, animalData.wanderRadius);
            }

            // Draw the current destination point
            if (Application.isPlaying && currentState == AnimalState.Walking)
            {
                // Draw a sphere at the destination
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(currentDestination, 0.3f);

                // Draw a line from the animal to the destination
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, currentDestination);
            }
        }

        /// Λαμβάνει την τρέχουσα κατάσταση της μηχανής καταστάσεων του ζώου.
        /// Χρήσιμο για εξωτερικά σενάρια που πρέπει να ελέγξουν τη συμπεριφορά του ζώου.
        public bool IsWalking => currentState == AnimalState.Walking;
        public bool IsIdle => currentState == AnimalState.Idle;

        /// Λαμβάνει το αντιστοιχισμένο AnimalData ScriptableObject. 
        /// Επιτρέπει σε εξωτερικά σενάρια να διαβάσουν τη διαμόρφωση του ζώου.
        public AnimalData Data => animalData;
    }
}
