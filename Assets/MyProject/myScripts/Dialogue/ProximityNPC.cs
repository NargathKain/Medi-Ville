using System.Collections;
using UnityEngine;

namespace MyProject.Dialogue
{
    /// NPC που αυτόματα ξεκινάει διάλογο όταν ο παίκτης μπαίνει σε ένα trigger area.
    /// Η κάμερα στρέφεται προς το NPC κατά τη διάρκεια του διαλόγου.
    /// Setup:
    /// 1. Προσθήκη script στο NPC GameObject
    /// 2. Προσθήκη Collider component, "Is Trigger" = true
    /// 3. Collider size -> detection radius
    /// 4. Assign lookAtTarget κεφάλι του NPC ή create empty child για καλύτερο αποτέλεσμα
    /// 5. Δημιουργία DialogueData asset και αντιστοίχιση στο inspector
    /// 6. παίκτης πρέπει να έχει το tag "Player" (ή προσαρμόστε το playerTag)
    [RequireComponent(typeof(Collider))]
    public class ProximityNPC : MonoBehaviour
    {
        // SERIALIZED FIELDS
        [Header("Dialogue")]

        /// Το dialogue data για αυτό το NPC.
        [Tooltip("DialogueData asset containing this NPC's dialogue.")]
        [SerializeField]
        private DialogueData dialogueData;

        [Header("Camera Look-At")]

        /// Το σημείο που η κάμερα κοιτάει κατά τη διάρκεια του διαλόγου. 
        /// Συνήθως το κεφάλι του NPC ή ένα empty child.
        [Tooltip("Transform the camera looks at. Use NPC's head or create an empty child.")]
        [SerializeField]
        private Transform lookAtTarget;

        /// Πόσο γρήγορα στρέφεται η κάμερα προς το NPC (μοίρες ανά δευτερόλεπτο).
        [Tooltip("Camera rotation speed (degrees per second).")]
        [SerializeField]
        private float cameraRotationSpeed = 90f;

        /// Αν αληθεύει, η κάμερα παραμένει κλειδωμένη στο NPC καθ' όλη τη διάρκεια του διαλόγου.
        /// Αν ψευδές, η κάμερα σταματάει να στρέφεται μόλις φτάσει κοντά στο στόχο.
        [Tooltip("Keep camera locked on NPC during entire dialogue.")]
        [SerializeField]
        private bool lockCameraDuringDialogue = true;

        [Header("Player Detection")]

        /// Tag που χρησιμοποιείται για να εντοπίσει τον παίκτη. 
        [Tooltip("Tag for the player GameObject.")]
        [SerializeField]
        private string playerTag = "Player";

        [Header("Behavior")]

        /// Αληθές -> Npc ενεργοποιείται 1 φορά
        /// Ψευδές -> Npc μπορεί να ενεργοποιηθεί ξανά αν ο παίκτης ξαναμπεί στο trigger
        [Tooltip("Only trigger dialogue once, then never again.")]
        [SerializeField]
        private bool triggerOnce = false;

        /// Καθυστέρηση σε δευτερόλεπτα πριν ξεκινήσει ο διάλογος μετά την είσοδο του παίκτη στο trigger.
        [Tooltip("Delay in seconds before dialogue starts.")]
        [SerializeField]
        private float startDelay = 0.5f;

        /// Αληθές -> ο διάλογος προχωράει αυτόματα μετά από το χρόνο που ορίζεται στο DialogueData.autoAdvanceTime
        [Tooltip("Auto-advance dialogue lines (player doesn't press E).")]
        [SerializeField]
        private bool useAutoAdvance = true;

        // PRIVATE FIELDS
        
        /// Παρακολουθεί αν ο διάλογος έχει ήδη ενεργοποιηθεί.
        private bool hasTriggered;

        /// Βλέπει αν είναι ήδη σε διάλογο 
        private bool isInDialogue;

        /// Αναφορά στο player camera transform
        private Transform playerCamera;

        /// Camera's original rotation πριν διάλογο
        private Quaternion originalCameraRotation;

        /// Αναφορά στο cinemachine 
        private MonoBehaviour cameraController;

        /// Coroutine για camera look-at.
        private Coroutine lookAtCoroutine;


        private void Awake()
        {
            // Validate trigger collider
            Collider col = GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                Debug.LogWarning($"[ProximityNPC] {gameObject.name}: Collider should be set as Trigger!");
            }

            // If no look at target, use this transform
            if (lookAtTarget == null)
            {
                lookAtTarget = transform;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            // Check if it's the player
            if (!other.CompareTag(playerTag))
            {
                return;
            }

            // Check if already triggered (for triggerOnce mode)
            if (triggerOnce && hasTriggered)
            {
                return;
            }

            // Check if already in dialogue
            if (isInDialogue)
            {
                return;
            }

            // Check if DialogueManager is busy
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                return;
            }

            // Find the camera
            playerCamera = Camera.main?.transform;
            if (playerCamera == null)
            {
                Debug.LogWarning("[ProximityNPC] No main camera found!");
                return;
            }

            // Find camera controller to disable during dialogue
            FindCameraController();

            // Start dialogue sequence
            StartCoroutine(StartDialogueSequence());
        }

        // DIALOGUE SEQUENCE

        /// εκκίνηση διαλόγου με delay και camera rotation
        private IEnumerator StartDialogueSequence()
        {
            isInDialogue = true;
            hasTriggered = true;

            // Store original camera rotation
            originalCameraRotation = playerCamera.rotation;

            // Disable camera controller
            SetCameraControllerEnabled(false);

            // Wait for start delay
            if (startDelay > 0)
            {
                yield return new WaitForSeconds(startDelay);
            }

            // Start rotating camera to look at NPC
            lookAtCoroutine = StartCoroutine(RotateCameraToTarget());

            // Wait a moment for camera to start rotating
            yield return new WaitForSeconds(0.3f);

            // Start dialogue
            if (DialogueManager.Instance != null && dialogueData != null)
            {
                DialogueManager.Instance.StartDialogue(dialogueData, useAutoAdvance, OnDialogueEnded);
            }
            else
            {
                Debug.LogWarning("[ProximityNPC] Cannot start dialogue: missing manager or data.");
                OnDialogueEnded();
            }
        }

        /// Smooth rotation της camera για να βλέπει το NPC
        private IEnumerator RotateCameraToTarget()
        {
            while (isInDialogue)
            {
                if (playerCamera != null && lookAtTarget != null)
                {
                    // Calculate target rotation
                    Vector3 direction = lookAtTarget.position - playerCamera.position;
                    Quaternion targetRotation = Quaternion.LookRotation(direction);

                    // Smoothly rotate towards target
                    playerCamera.rotation = Quaternion.RotateTowards(
                        playerCamera.rotation,
                        targetRotation,
                        cameraRotationSpeed * Time.deltaTime
                    );

                    // If not locking camera, stop once we're close enough
                    if (!lockCameraDuringDialogue)
                    {
                        if (Quaternion.Angle(playerCamera.rotation, targetRotation) < 1f)
                        {
                            yield break;
                        }
                    }
                }

                yield return null;
            }
        }

        /// Κλήση όταν τελειώνει ο διάλογος
        private void OnDialogueEnded()
        {
            // Stop look-at coroutine
            if (lookAtCoroutine != null)
            {
                StopCoroutine(lookAtCoroutine);
                lookAtCoroutine = null;
            }

            // Re-enable camera controller
            SetCameraControllerEnabled(true);

            isInDialogue = false;

            Debug.Log($"[ProximityNPC] {gameObject.name}: Dialogue ended.");
        }

        // CAMERA CONTROLLER MANAGEMENT

        /// Εντοπίζει το camera controller component και το απενεργοποιεί κατά τη διάρκεια.
        /// Supports Cinemachine and custom controllers.
        private void FindCameraController()
        {
            if (playerCamera == null)
            {
                return;
            }

            // Try to find Cinemachine Brain on camera
            cameraController = playerCamera.GetComponent("CinemachineBrain") as MonoBehaviour;

            // If not found, try to find a camera controller on the player
            if (cameraController == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag(playerTag);
                if (player != null)
                {
                    // Look for common camera controller scripts
                    cameraController = player.GetComponentInChildren<MonoBehaviour>();
                }
            }
        }

        /// Enables ή disables camera controller       
        /// <param name="enabled">Whether to enable the controller.</param>
        private void SetCameraControllerEnabled(bool enabled)
        {
            if (cameraController != null)
            {
                cameraController.enabled = enabled;
            }
        }

        // DEBUG VISUALIZATION

        private void OnDrawGizmosSelected()
        {
            // Draw look-at target
            if (lookAtTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(lookAtTarget.position, 0.2f);
                Gizmos.DrawLine(transform.position, lookAtTarget.position);
            }

            // Draw trigger area
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                Gizmos.color = new Color(0, 1, 0, 0.3f);
                if (col is SphereCollider sphere)
                {
                    Gizmos.DrawWireSphere(transform.position + sphere.center, sphere.radius);
                }
                else if (col is BoxCollider box)
                {
                    Gizmos.matrix = transform.localToWorldMatrix;
                    Gizmos.DrawWireCube(box.center, box.size);
                }
            }
        }
    }
}
