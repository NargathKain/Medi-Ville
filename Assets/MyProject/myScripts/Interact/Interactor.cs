using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace MyProject.Interact
{
    /// Κύριο script για το σύστημα αλληλεπίδρασης του παίκτη.
    /// Ανιχνεύει αντικείμενα με IInteractable μέσω raycast και διαχειρίζεται τις αλληλεπιδράσεις.
    /// Προσθέστε αυτό στην κάμερα του παίκτη.
    public class Interactor : MonoBehaviour
    {
        [Header("Output")]

        /// Το αλληλεπιδραστικό αντικείμενο που κοιτάζει τώρα ο παίκτης.
        /// Null αν δεν υπάρχει έγκυρο αλληλεπιδραστικό σε εμβέλεια/όραση.
        public IInteractable currentInteractable;

        /// Αποθηκευμένη αναφορά στο τελευταίο έγκυρο αλληλεπιδραστικό.
        /// Χρησιμοποιείται για μεταβάσεις κατάστασης και κλήση OnEndInteract/OnAbortInteract.
        private IInteractable lastInteractable;

        [Header("Raycast Setup")]

        /// Το tag που πρέπει να έχουν τα αλληλεπιδραστικά αντικείμενα για να ανιχνευθούν.
        /// Όλα τα αλληλεπιδραστικά αντικείμενα πρέπει να έχουν αυτό το tag.
        [Tooltip("Tag που πρέπει να έχουν τα αλληλεπιδραστικά αντικείμενα.")]
        [SerializeField]
        private string targetTag = "Interactable";

        /// Μέγιστη απόσταση για το raycast αλληλεπίδρασης.
        /// Αντικείμενα πέρα από αυτή την απόσταση δεν μπορούν να αλληλεπιδράσουν.
        [Tooltip("Μέγιστη απόσταση αλληλεπίδρασης σε μονάδες.")]
        [SerializeField]
        private float rayMaxDistance = 5f;

        /// Layer mask για το raycast. Καθορίζει ποια layers να ελέγχονται.
        /// Πρέπει να εξαιρεί το layer του παίκτη και τυχόν layers που δεν πρέπει να μπλοκάρουν την αλληλεπίδραση.
        [Tooltip("Layers να συμπεριληφθούν στο raycast. Εξαιρέστε το layer του παίκτη.")]
        [SerializeField]
        private LayerMask layerMask = ~0; // Προεπιλογή: όλα τα layers (bitwise NOT του 0)

        /// Αναφορά στην κύρια κάμερα. Αν δεν ανατεθεί, χρησιμοποιεί Camera.main.
        /// Το raycast ξεκινά από τη θέση και κατεύθυνση αυτής της κάμερας.
        [Tooltip("Κάμερα από την οποία εκπέμπεται το raycast. Χρησιμοποιεί Camera.main αν δεν ανατεθεί.")]
        [SerializeField]
        private Camera playerCamera;

        [Header("UI References")]

        /// Αναφορά στο InteractorUI script που διαχειρίζεται την εμφάνιση μηνυμάτων.
        /// Χρησιμοποιείται για εμφάνιση κειμένου που επιστρέφεται από αλληλεπιδραστικά αντικείμενα.
        [Tooltip("InteractorUI component για εμφάνιση μηνυμάτων αλληλεπίδρασης.")]
        [SerializeField]
        private InteractorUI interactorUI;

        /// Το panel hint που δείχνει το prompt αλληλεπίδρασης.
        /// Αυτό το GameObject εμφανίζεται/κρύβεται ανάλογα με την κατάσταση αλληλεπίδρασης.
        [Tooltip("Panel ή GameObject που περιέχει το hint UI αλληλεπίδρασης.")]
        [SerializeField]
        private GameObject hint;

        /// Text element που δείχνει ποια ενέργεια θα εκτελεστεί.
        /// Εμφανίζει το InteractionPrompt από το τρέχον IInteractable.
        [Tooltip("TextMeshPro element που δείχνει 'Press E to [ενέργεια]'.")]
        [SerializeField]
        private TextMeshProUGUI promptText;

        /// Format string για το prompt αλληλεπίδρασης.
        /// Το {0} αντικαθίσταται με την ιδιότητα InteractionPrompt του αλληλεπιδραστικού.
        [Tooltip("Format για το κείμενο prompt. {0} = όνομα ενέργειας.")]
        [SerializeField]
        private string promptFormat = "Press E to {0}";

        [Header("Input")]

        /// Αναφορά στο PlayerInput component.
        /// Αν δεν ανατεθεί, ψάχνει στα parent GameObjects.
        [Tooltip("PlayerInput component. Βρίσκεται αυτόματα αν δεν ανατεθεί.")]
        [SerializeField]
        private PlayerInput playerInput;

        /// Αναφορά στο Interact input action.
        /// Αποθηκεύεται στο Start για απόδοση.
        private InputAction interactAction;

        /// Αναφορά στο Move input action.
        /// Χρησιμοποιείται για ακύρωση αλληλεπιδράσεων όταν ο παίκτης κινείται.
        private InputAction moveAction;

        /// True όταν ο παίκτης κοιτάζει ένα αλληλεπιδραστικό και μπορεί να πατήσει E.
        private bool readyInteract;

        /// True όταν ο παίκτης αλληλεπιδρά ενεργά με ένα αντικείμενο.
        private bool interacting;

        /// Αρχικοποιεί αναφορές και επικυρώνει τη ρύθμιση.
        private void Start()
        {
            // Επαναφορά μεταβλητών κατάστασης
            currentInteractable = null;
            interacting = false;

            // Λήψη αναφοράς κάμερας αν δεν ανατέθηκε
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    Debug.LogError("[Interactor] Δεν ανατέθηκε κάμερα και το Camera.main είναι null!");
                    enabled = false;
                    return;
                }
            }

            // Λήψη PlayerInput component
            if (playerInput == null)
            {
                playerInput = GetComponentInParent<PlayerInput>();
            }

            if (playerInput == null)
            {
                Debug.LogError("[Interactor] Δεν βρέθηκε PlayerInput component!");
                enabled = false;
                return;
            }

            // Αποθήκευση input actions
            interactAction = playerInput.actions["Interact"];
            moveAction = playerInput.actions["Move"];

            if (interactAction == null)
            {
                Debug.LogError("[Interactor] Δεν βρέθηκε το 'Interact' action στα Input Actions!");
                enabled = false;
                return;
            }

            // Επικύρωση αναφορών UI (μόνο προειδοποιήσεις - το σύστημα λειτουργεί χωρίς αυτά)
            if (interactorUI == null)
                Debug.LogWarning("[Interactor] Δεν ανατέθηκε InteractorUI. Τα μηνύματα δεν θα εμφανίζονται.");
            if (hint == null)
                Debug.LogWarning("[Interactor] Δεν ανατέθηκε Hint GameObject. Τα prompts δεν θα εμφανίζονται.");
            if (promptText == null)
                Debug.LogWarning("[Interactor] Δεν ανατέθηκε PromptText. Το κείμενο ενέργειας δεν θα εμφανίζεται.");

            // Αρχικοποίηση σε καθαρή κατάσταση
            AbortInteract();
        }

        /// Εκτελεί ανίχνευση raycast και διαχειρίζεται input κάθε frame.
        private void Update()
        {
            // Επαναφορά τρέχοντος αλληλεπιδραστικού κάθε frame
            currentInteractable = null;

            // Εκτέλεση raycast από το κέντρο της κάμερας
            PerformRaycast();

            // Διαχείριση μεταβάσεων κατάστασης ανάλογα με το τι κοιτάμε
            HandleStateTransitions();

            // Διαχείριση input ανάλογα με την τρέχουσα κατάσταση
            HandleInput();

            // Ενημέρωση αναφοράς τελευταίου αλληλεπιδραστικού για συγκρίσεις επόμενου frame
            if (currentInteractable != null)
            {
                lastInteractable = currentInteractable;
            }
        }

        /// Εκπέμπει μια ακτίνα από το κέντρο της κάμερας για ανίχνευση αλληλεπιδραστικών αντικειμένων.
        /// Ενημερώνει το currentInteractable αν βρεθεί έγκυρο αντικείμενο.
        private void PerformRaycast()
        {
            RaycastHit hit;

            // Raycast από τη θέση της κάμερας προς την κατεύθυνση forward της κάμερας
            // Αυτό εξασφαλίζει ότι ανιχνεύουμε αυτό που πραγματικά κοιτάζει ο παίκτης
            if (Physics.Raycast(playerCamera.transform.position,
                               playerCamera.transform.forward,
                               out hit,
                               rayMaxDistance,
                               layerMask))
            {
                // Έλεγχος αν το αντικείμενο που χτυπήθηκε έχει το σωστό tag
                if (hit.collider.CompareTag(targetTag))
                {
                    // Προσπάθεια λήψης IInteractable component
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        currentInteractable = interactable;
                    }
                }
            }
        }

        /// Διαχειρίζεται μεταβάσεις μεταξύ καταστάσεων ready/not ready βάσει αποτελεσμάτων raycast.
        private void HandleStateTransitions()
        {
            // Μετάβαση: Not Ready → Ready (άρχισε να κοιτάζει αλληλεπιδραστικό)
            if (currentInteractable != null && !readyInteract)
            {
                ReadyInteract();
            }
            // Μετάβαση: Ready → Not Ready (σταμάτησε να κοιτάζει αλληλεπιδραστικό)
            else if (currentInteractable == null && readyInteract)
            {
                AbortInteract();
            }
            // Ενημέρωση κειμένου prompt αν κοιτάζει διαφορετικό αλληλεπιδραστικό
            else if (currentInteractable != null && currentInteractable != lastInteractable)
            {
                UpdatePromptText();
            }
        }

        /// Διαχειρίζεται input παίκτη για αλληλεπίδραση και ακύρωση.
        private void HandleInput()
        {
            if (!interacting)
            {
                // Δεν αλληλεπιδρά - έλεγχος για input αλληλεπίδρασης
                if (readyInteract && interactAction.WasPressedThisFrame())
                {
                    Interact();
                }
            }
            else
            {
                // Αλληλεπιδρά τώρα - έλεγχος για ακύρωση
                if (ShouldCancelInteraction())
                {
                    EndInteract();
                }
            }
        }

        /// Καθορίζει αν η τρέχουσα αλληλεπίδραση πρέπει να ακυρωθεί.
        /// Ακύρωση συμβαίνει όταν: ο παίκτης κινείται, ή κοιτάζει διαφορετικό/κανένα αντικείμενο.
        /// <returns>True αν η αλληλεπίδραση πρέπει να ακυρωθεί.</returns>
        private bool ShouldCancelInteraction()
        {
            // Ακύρωση αν ο παίκτης αρχίσει να κινείται
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                if (moveInput.magnitude > 0.1f)
                {
                    return true;
                }
            }

            // Ακύρωση αν δεν κοιτάζει πλέον αλληλεπιδραστικό
            if (currentInteractable == null)
            {
                return true;
            }

            // Ακύρωση αν κοιτάζει διαφορετικό αλληλεπιδραστικό
            if (currentInteractable != lastInteractable)
            {
                return true;
            }

            return false;
        }

        /// Καλείται όταν ο παίκτης αρχίσει να κοιτάζει ένα αλληλεπιδραστικό αντικείμενο.
        /// Εμφανίζει το hint αλληλεπίδρασης και ειδοποιεί το αντικείμενο.
        private void ReadyInteract()
        {
            readyInteract = true;

            // Εμφάνιση hint UI αλληλεπίδρασης
            if (hint != null)
            {
                hint.SetActive(true);
            }

            // Ενημέρωση κειμένου prompt με την ενέργεια αυτού του αντικειμένου
            UpdatePromptText();

            // Ειδοποίηση του αλληλεπιδραστικού αντικειμένου
            currentInteractable.OnReadyInteract();
        }

        /// Καλείται όταν ο παίκτης σταματήσει να κοιτάζει ένα αλληλεπιδραστικό χωρίς αλληλεπίδραση.
        /// Κρύβει το hint αλληλεπίδρασης και ειδοποιεί το αντικείμενο.
        private void AbortInteract()
        {
            readyInteract = false;

            // Απόκρυψη hint UI αλληλεπίδρασης
            if (hint != null)
            {
                hint.SetActive(false);
            }

            // Ειδοποίηση του αντικειμένου που κοιτούσε προηγουμένως
            if (lastInteractable != null)
            {
                lastInteractable.OnAbortInteract();
            }
        }

        /// Καλείται όταν ο παίκτης πατήσει το κουμπί αλληλεπίδρασης.
        /// Ξεκινά την αλληλεπίδραση με το τρέχον αντικείμενο.
        private void Interact()
        {
            interacting = true;
            readyInteract = true;

            // Απόκρυψη του hint κατά την αλληλεπίδραση
            if (hint != null)
            {
                hint.SetActive(false);
            }

            // Κλήση της μεθόδου interact του αντικειμένου, περνώντας τον εαυτό μας για callbacks
            currentInteractable.OnInteract(this);
        }

        /// Καλείται όταν τελειώνει μια ενεργή αλληλεπίδραση (ακυρώθηκε ή ολοκληρώθηκε).
        /// Καθαρίζει την κατάσταση και ειδοποιεί το αντικείμενο.
        private void EndInteract()
        {
            interacting = false;
            readyInteract = false;

            // Ειδοποίηση του αντικειμένου ότι τελείωσε η αλληλεπίδραση
            if (lastInteractable != null)
            {
                lastInteractable.OnEndInteract();
            }

            // Απόκρυψη οποιουδήποτε εμφανιζόμενου μηνύματος
            if (interactorUI != null)
            {
                interactorUI.HideTextMessage();
            }
        }

        /// Ενημερώνει το κείμενο prompt για να δείξει την ενέργεια του τρέχοντος αλληλεπιδραστικού.
        private void UpdatePromptText()
        {
            if (promptText != null && currentInteractable != null)
            {
                string actionName = currentInteractable.InteractionPrompt;
                promptText.text = string.Format(promptFormat, actionName);
            }
        }

        /// Καλείται από αλληλεπιδραστικά αντικείμενα για να τερματίσουν τη δική τους αλληλεπίδραση.
        /// Επικυρώνει ότι ο αιτών είναι το τρέχον αλληλεπιδραστικό.
        /// <param name="requester">Το IInteractable που ζητά τερματισμό αλληλεπίδρασης.</param>
        public void EndInteract(IInteractable requester)
        {
            if (requester == lastInteractable)
            {
                EndInteract();
            }
        }

        /// Λαμβάνει μήνυμα κειμένου από αλληλεπιδραστικό αντικείμενο και το εμφανίζει.
        /// Χρησιμοποιείται από αντικείμενα όπως πινακίδες που θέλουν να δείξουν κείμενο στον παίκτη.
        /// <param name="message">Το μήνυμα προς εμφάνιση.</param>
        public void ReceiveInteract(string message)
        {
            if (interactorUI != null)
            {
                interactorUI.ShowTextMessage(message);
            }
        }

        /// Λαμβάνει αναφορά αλληλεπιδραστικού αντικειμένου για προχωρημένες αλληλεπιδράσεις.
        /// Override αυτό σε παράγωγες κλάσεις για προσαρμοσμένη συμπεριφορά.
        /// <param name="interactable">Το αλληλεπιδραστικό αντικείμενο.</param>
        public void ReceiveInteract(IInteractable interactable)
        {
            // Δεσμευμένο για προχωρημένες περιπτώσεις χρήσης
            Debug.Log($"[Interactor] Ελήφθη interactable: {interactable}");
        }

        /// Σχεδιάζει το raycast αλληλεπίδρασης στο Scene view για debugging.
        private void OnDrawGizmosSelected()
        {
            Camera cam = playerCamera != null ? playerCamera : Camera.main;
            if (cam == null) return;

            // Σχεδίαση της γραμμής raycast
            Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
            Gizmos.DrawRay(cam.transform.position, cam.transform.forward * rayMaxDistance);

            // Σχεδίαση σφαίρας στη μέγιστη εμβέλεια
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cam.transform.position + cam.transform.forward * rayMaxDistance, 0.1f);
        }
    }
}
