using UnityEngine;
using MyProject.Interact;

namespace MyProject.Dialogue
{
    /// NPC που μπορεί να αλληλεπιδράσει με τον παίκτη
    /// πατώντας το Ε
    /// Setup:
    /// 1. script σε NPC GameObject
    /// 2. προσθήκη Collider component
    /// 3. Tag NPC GameObject ως "Interactable"
    /// 4. Δημιουργία DialogueData asset και αντιστοίχιση
    /// 5. προαιρετική προσθήκη indicator για οπτικό feedback
    public class InteractableNPC : MonoBehaviour, IInteractable
    {
        // SERIALIZED FIELDS

        [Header("Dialogue")]

        /// dialogue data για αυτό το NPC
        [Tooltip("DialogueData asset containing this NPC's dialogue.")]
        [SerializeField]
        private DialogueData dialogueData;

        [Header("Visual Feedback")]

        /// προαιρετικός indicator
        [Tooltip("Visual indicator shown when player can interact.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Settings")]

        /// Custom prompt text (px "Talk to Blacksmith").
        /// Αν άδειο, τότε "Talk to {NPC Name}".
        [Tooltip("Custom prompt text. Leave empty for auto-generated.")]
        [SerializeField]
        private string customPrompt = "";

        // PRIVATE FIELDS

        /// Κοιτάει αν το npc είναι ήδη σε διάλογο
        private bool isInDialogue;

        // IINTERACTABLE IMPLEMENTATION

        /// The prompt shown to the player.
        public string InteractionPrompt
        {
            get
            {
                if (isInDialogue)
                {
                    return ""; // Don't show prompt during dialogue
                }

                if (!string.IsNullOrEmpty(customPrompt))
                {
                    return customPrompt;
                }

                if (dialogueData != null)
                {
                    return $"Talk to {dialogueData.npcName}";
                }

                return "Talk";
            }
        }

        /// Καλείται όταν ο παίκτης πατάει το πλήκτρο αλληλεπίδρασης (E) ενώ κοιτάει αυτό το NPC.
        /// <param name="interactor">The Interactor that initiated interaction.</param>
        public void OnInteract(Interactor interactor)
        {
            if (isInDialogue)
            {
                return;
            }

            if (dialogueData == null)
            {
                Debug.LogWarning($"[InteractableNPC] {gameObject.name}: No DialogueData assigned!");
                interactor.EndInteract(this);
                return;
            }

            if (DialogueManager.Instance == null)
            {
                Debug.LogError("[InteractableNPC] DialogueManager not found in scene!");
                interactor.EndInteract(this);
                return;
            }

            // Start dialogue
            isInDialogue = true;

            // Hide indicator during dialogue
            if (indicator != null)
            {
                indicator.SetActive(false);
            }

            // Start the dialogue, with callback when it ends
            DialogueManager.Instance.StartDialogue(dialogueData, false, OnDialogueEnded);

            // End the interaction (dialogue system takes over)
            interactor.EndInteract(this);
        }

        /// Καλείται όταν ο διάλογος τελειώνει ή ακυρώνεται.
        public void OnEndInteract()
        {
            // Nothing to do here - dialogue system handles everything
        }

        /// Καλείται όταν ο παίκτης ακυρώνει την αλληλεπίδραση (π.χ. αφήνει το πλήκτρο ή κοιτάζει αλλού).
        public void OnAbortInteract()
        {
            if (indicator != null && !isInDialogue)
            {
                indicator.SetActive(false);
            }
        }

        /// Καλείται όταν ο παίκτης κοιτάζει αυτό το NPC (έτοιμος για αλληλεπίδραση).
        public void OnReadyInteract()
        {
            if (indicator != null && !isInDialogue)
            {
                indicator.SetActive(true);
            }
        }

        // PRIVATE METHODS

        /// Καλείται όταν ο διάλογος τελειώνει, για να επαναφέρει την κατάσταση του NPC.
        private void OnDialogueEnded()
        {
            isInDialogue = false;
        }
    }
}
