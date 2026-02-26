using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace MyProject.Dialogue
{
    /// Singleton διαχειριστής διαλόγου για το UI 
    /// Ελέγχει εμφάνιση/hide του πάνελ διαλόγου, ειδοποίηση κατά το τέλος.
    /// Setup:
    /// 1. Δημνιουργία Canvas με Panel για dialogue
    /// 2. Προσθήκη TextMeshProUGUI elements για NPC name και dialogue text
    /// 3. Προσθήκη αυτού του script σε gameObject 
    /// 4. Αντιστοίχιση UI στον Inspector
    public class DialogueManager : MonoBehaviour
    {
        // SINGLETON - > πρόσβαση από οπουδήποτε μέσω DialogueManager.Instance
        public static DialogueManager Instance { get; private set; }

        // SERIALIZED FIELDS       

        [Header("UI References")]
        /// dialogue panel GameObject (shown/hidden).

        [Tooltip("The panel containing all dialogue UI elements.")]
        [SerializeField]
        private GameObject dialoguePanel;

        /// Text element για NPC name.
        [Tooltip("TextMeshPro element for NPC name.")]
        [SerializeField]
        private TextMeshProUGUI npcNameText;

        /// Text element για dialogue line.
        [Tooltip("TextMeshPro element for dialogue text.")]
        [SerializeField]
        private TextMeshProUGUI dialogueText;

        /// Optional prompt text (e.g., "Press E to continue").
        [Tooltip("Optional prompt text shown during dialogue.")]
        [SerializeField]
        private TextMeshProUGUI promptText;

        [Header("Input")]

        /// Αναφορά σε PlayerInput για το Interact action.
        [Tooltip("PlayerInput component. Auto-found if not assigned.")]
        [SerializeField]
        private PlayerInput playerInput;

        [Header("Settings")]

        /// Κείμενο που εμφανίζεται κατά την διάρκεια του διαλόγου. 
        [Tooltip("Prompt text for interactive dialogue.")]
        [SerializeField]
        private string interactivePrompt = "Press E to continue...";

        /// Κείμενο που εμφανίζεται κατά τη διάρκεια του διαλόγου αυτόματης προώθησης.
        [Tooltip("Prompt text for auto-advancing dialogue.")]
        [SerializeField]
        private string autoPrompt = "";
                
        // PRIVATE FIELDS
        
        /// Παρών διάλογος
        private DialogueData currentDialogue;

        /// current γραμμή στο διάλογο
        private int currentLineIndex;

        /// αληθής αν ο διάλογος είναι 
        private bool isDialogueActive;

        /// αληθής αν ο διάλογος προχωράει αυτόματα μετά από χρόνο
        private bool isAutoAdvance;

        /// αλληλεπίδραση αποθηκευμένη στη μνήμη
        private InputAction interactAction;

        /// Coroutine για αυτόματη προώθηση
        private Coroutine autoAdvanceCoroutine;

        /// Callback όταν τελειώνει ο διάλογος (προαιρετικό).
        private Action onDialogueEndCallback;

        // EVENTS

        /// Event όταν ξεκινάει ο διάλογος.
        public event Action OnDialogueStarted;

        /// Event όταν τελειώνει ο διάλογος. 
        public event Action OnDialogueEnded;

        // PROPERTIES

        /// επιστρέφει true αν ο διάλογος είναι ενεργός
        public bool IsDialogueActive => isDialogueActive;

        
        private void Awake()
        {
            // Singleton setup
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[DialogueManager] Duplicate instance destroyed.");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Hide panel at start
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }
        }

        private void Start()
        {
            // Find PlayerInput 
            if (playerInput == null)
            {
                playerInput = FindAnyObjectByType<PlayerInput>();
            }

            if (playerInput != null)
            {
                interactAction = playerInput.actions["Interact"];
            }
            else
            {
                Debug.LogWarning("[DialogueManager] No PlayerInput found. Manual advance won't work.");
            }
        }

        private void Update()
        {
            // Handle manual advance input
            if (isDialogueActive && !isAutoAdvance)
            {
                if (interactAction != null && interactAction.WasPressedThisFrame())
                {
                    AdvanceLine();
                }
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // PUBLIC METHODS
        /// έναρξη διαλόγου
        /// <param name="dialogue">The DialogueData to display.</param>
        /// <param name="forceAutoAdvance">If true, uses auto-advance regardless of DialogueData setting.</param>
        /// <param name="onEnd">Optional callback when dialogue ends.</param>
        public void StartDialogue(DialogueData dialogue, bool forceAutoAdvance = false, Action onEnd = null)
        {
            if (dialogue == null || dialogue.dialogueLines == null || dialogue.dialogueLines.Length == 0)
            {
                Debug.LogWarning("[DialogueManager] Cannot start dialogue: null or empty.");
                return;
            }

            // Store data
            currentDialogue = dialogue;
            currentLineIndex = 0;
            isDialogueActive = true;
            isAutoAdvance = forceAutoAdvance || dialogue.autoAdvanceTime > 0;
            onDialogueEndCallback = onEnd;

            // Show panel
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(true);
            }

            // Set NPC name
            if (npcNameText != null)
            {
                npcNameText.text = dialogue.npcName;
            }

            // Set prompt text
            if (promptText != null)
            {
                promptText.text = isAutoAdvance ? autoPrompt : interactivePrompt;
            }

            // Show first line
            DisplayCurrentLine();

            // Fire event
            OnDialogueStarted?.Invoke();

            Debug.Log($"[DialogueManager] Started dialogue with {dialogue.npcName}");
        }

        /// Προχωρά στην επόμενη γραμμή διαλόγου ή τερματίζει εάν δεν υπάρχουν άλλες γραμμές.
        public void AdvanceLine()
        {
            if (!isDialogueActive)
            {
                return;
            }

            // Stop any auto-advance coroutine
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            currentLineIndex++;

            if (currentLineIndex >= currentDialogue.dialogueLines.Length)
            {
                // End of dialogue
                EndDialogue();
            }
            else
            {
                // Show next line
                DisplayCurrentLine();
            }
        }

        /// ’μεσως τερματισμός διαλόγου 
        public void EndDialogue()
        {
            if (!isDialogueActive)
            {
                return;
            }

            // Stop auto-advance
            if (autoAdvanceCoroutine != null)
            {
                StopCoroutine(autoAdvanceCoroutine);
                autoAdvanceCoroutine = null;
            }

            // Hide panel
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
            }

            // Clear state
            isDialogueActive = false;
            currentDialogue = null;

            // Fire event
            OnDialogueEnded?.Invoke();

            // Invoke callback
            onDialogueEndCallback?.Invoke();
            onDialogueEndCallback = null;

            Debug.Log("[DialogueManager] Dialogue ended.");
        }

        // PRIVATE METHODS

        /// Δείξε την παρούσα γραμμή διαλόγου στο UI.
        private void DisplayCurrentLine()
        {
            if (dialogueText != null && currentDialogue != null)
            {
                dialogueText.text = currentDialogue.dialogueLines[currentLineIndex];
            }

            // Start auto-advance if enabled
            if (isAutoAdvance && currentDialogue.autoAdvanceTime > 0)
            {
                autoAdvanceCoroutine = StartCoroutine(AutoAdvanceCoroutine());
            }
        }

        /// Coroutine που προχωρά αυτόματα στην επόμενη γραμμή μετά από καθυστέρηση
        private IEnumerator AutoAdvanceCoroutine()
        {
            yield return new WaitForSeconds(currentDialogue.autoAdvanceTime);
            AdvanceLine();
        }
    }
}
