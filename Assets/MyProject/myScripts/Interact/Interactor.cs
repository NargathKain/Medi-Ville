using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace MyProject.Interact
{
    /// <summary>
    /// Handles player interaction with IInteractable objects in the game world.
    /// Uses raycasting from the camera to detect interactable objects and manages
    /// the interaction state machine (ready, interacting, end, abort).
    ///
    /// Setup:
    /// 1. Attach to the player's camera (or a child of it)
    /// 2. Assign the InteractorUI reference for displaying messages
    /// 3. Assign the hint GameObject and prompt text for "Press E to..." prompts
    /// 4. Ensure interactable objects have the "Interactable" tag and IInteractable component
    /// 5. Ensure PlayerInput component exists on player with "Interact" action
    ///
    /// Version 2.0 - Updated to use New Input System
    /// </summary>
    public class Interactor : MonoBehaviour
    {
        //=============================================================================
        // OUTPUT - Current interaction state (readable by other scripts)
        //=============================================================================

        [Header("Output")]

        /// <summary>
        /// The interactable object the player is currently looking at.
        /// Null if no valid interactable is in range/view.
        /// </summary>
        public IInteractable currentInteractable;

        /// <summary>
        /// Saved reference to the last valid interactable.
        /// Used for state transitions and calling OnEndInteract/OnAbortInteract.
        /// </summary>
        private IInteractable lastInteractable;

        //=============================================================================
        // SETUP - Configuration in Inspector
        //=============================================================================

        [Header("Raycast Setup")]

        /// <summary>
        /// The tag that interactable objects must have to be detected.
        /// All interactable objects should be tagged with this value.
        /// </summary>
        [Tooltip("Tag that interactable objects must have.")]
        [SerializeField]
        private string targetTag = "Interactable";

        /// <summary>
        /// Maximum distance for the interaction raycast.
        /// Objects beyond this distance cannot be interacted with.
        /// </summary>
        [Tooltip("Maximum interaction distance in units.")]
        [SerializeField]
        private float rayMaxDistance = 5f;

        /// <summary>
        /// Layer mask for the raycast. Determines which layers to check.
        /// Should exclude player layer and any layers that shouldn't block interaction.
        /// </summary>
        [Tooltip("Layers to include in raycast. Exclude player layer.")]
        [SerializeField]
        private LayerMask layerMask = ~0; // Default: all layers (bitwise NOT of 0)

        /// <summary>
        /// Reference to the main camera. If not assigned, uses Camera.main.
        /// The raycast originates from this camera's position and direction.
        /// </summary>
        [Tooltip("Camera to raycast from. Uses Camera.main if not assigned.")]
        [SerializeField]
        private Camera playerCamera;

        [Header("UI References")]

        /// <summary>
        /// Reference to the InteractorUI script that handles message display.
        /// Used to show text returned from interactable objects.
        /// </summary>
        [Tooltip("InteractorUI component for displaying interaction messages.")]
        [SerializeField]
        private InteractorUI interactorUI;

        /// <summary>
        /// The hint panel that shows the interaction prompt.
        /// This GameObject is shown/hidden based on interaction state.
        /// </summary>
        [Tooltip("Panel or GameObject containing the interaction hint UI.")]
        [SerializeField]
        private GameObject hint;

        /// <summary>
        /// Text element showing what action will be performed.
        /// Displays the InteractionPrompt from the current IInteractable.
        /// </summary>
        [Tooltip("TextMeshPro element showing 'Press E to [action]'.")]
        [SerializeField]
        private TextMeshProUGUI promptText;

        /// <summary>
        /// Format string for the interaction prompt.
        /// {0} is replaced with the interactable's InteractionPrompt property.
        /// </summary>
        [Tooltip("Format for prompt text. {0} = action name.")]
        [SerializeField]
        private string promptFormat = "Press E to {0}";

        //=============================================================================
        // INPUT - New Input System references
        //=============================================================================

        [Header("Input")]

        /// <summary>
        /// Reference to the PlayerInput component.
        /// If not assigned, searches in parent GameObjects.
        /// </summary>
        [Tooltip("PlayerInput component. Auto-found if not assigned.")]
        [SerializeField]
        private PlayerInput playerInput;

        /// <summary>
        /// Reference to the Interact input action.
        /// Cached on Start for performance.
        /// </summary>
        private InputAction interactAction;

        /// <summary>
        /// Reference to the Move input action.
        /// Used to cancel interactions when player moves.
        /// </summary>
        private InputAction moveAction;

        //=============================================================================
        // STATE - Internal state tracking
        //=============================================================================

        /// <summary>
        /// True when player is looking at an interactable and can press E.
        /// </summary>
        private bool readyInteract;

        /// <summary>
        /// True when player is actively interacting with an object.
        /// </summary>
        private bool interacting;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        /// <summary>
        /// Initializes references and validates setup.
        /// </summary>
        private void Start()
        {
            // Reset state variables
            currentInteractable = null;
            interacting = false;

            // Get camera reference if not assigned
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
                if (playerCamera == null)
                {
                    Debug.LogError("[Interactor] No camera assigned and Camera.main is null!");
                    enabled = false;
                    return;
                }
            }

            // Get PlayerInput component
            if (playerInput == null)
            {
                playerInput = GetComponentInParent<PlayerInput>();
            }

            if (playerInput == null)
            {
                Debug.LogError("[Interactor] No PlayerInput component found!");
                enabled = false;
                return;
            }

            // Cache input actions
            interactAction = playerInput.actions["Interact"];
            moveAction = playerInput.actions["Move"];

            if (interactAction == null)
            {
                Debug.LogError("[Interactor] 'Interact' action not found in Input Actions!");
                enabled = false;
                return;
            }

            // Validate UI references (warnings only - system works without them)
            if (interactorUI == null)
                Debug.LogWarning("[Interactor] InteractorUI not assigned. Messages won't display.");
            if (hint == null)
                Debug.LogWarning("[Interactor] Hint GameObject not assigned. Prompts won't show.");
            if (promptText == null)
                Debug.LogWarning("[Interactor] PromptText not assigned. Action text won't show.");

            // Initialize to clean state
            AbortInteract();
        }

        /// <summary>
        /// Performs raycast detection and handles input each frame.
        /// </summary>
        private void Update()
        {
            // Reset current interactable each frame
            currentInteractable = null;

            // Perform raycast from camera center
            PerformRaycast();

            // Handle state transitions based on what we're looking at
            HandleStateTransitions();

            // Handle input based on current state
            HandleInput();

            // Update last interactable reference for next frame comparisons
            if (currentInteractable != null)
            {
                lastInteractable = currentInteractable;
            }
        }

        //=============================================================================
        // RAYCAST DETECTION
        //=============================================================================

        /// <summary>
        /// Casts a ray from the camera center to detect interactable objects.
        /// Updates currentInteractable if a valid object is found.
        /// </summary>
        private void PerformRaycast()
        {
            RaycastHit hit;

            // Raycast from camera position in camera's forward direction
            // This ensures we're detecting what the player is actually looking at
            if (Physics.Raycast(playerCamera.transform.position,
                               playerCamera.transform.forward,
                               out hit,
                               rayMaxDistance,
                               layerMask))
            {
                // Check if hit object has the correct tag
                if (hit.collider.CompareTag(targetTag))
                {
                    // Try to get IInteractable component
                    IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                    if (interactable != null)
                    {
                        currentInteractable = interactable;
                    }
                }
            }
        }

        //=============================================================================
        // STATE MACHINE
        //=============================================================================

        /// <summary>
        /// Handles transitions between ready/not ready states based on raycast results.
        /// </summary>
        private void HandleStateTransitions()
        {
            // Transition: Not Ready → Ready (started looking at interactable)
            if (currentInteractable != null && !readyInteract)
            {
                ReadyInteract();
            }
            // Transition: Ready → Not Ready (stopped looking at interactable)
            else if (currentInteractable == null && readyInteract)
            {
                AbortInteract();
            }
            // Update prompt text if looking at a different interactable
            else if (currentInteractable != null && currentInteractable != lastInteractable)
            {
                UpdatePromptText();
            }
        }

        /// <summary>
        /// Handles player input for interaction and cancellation.
        /// </summary>
        private void HandleInput()
        {
            if (!interacting)
            {
                // Not interacting - check for interact input
                if (readyInteract && interactAction.WasPressedThisFrame())
                {
                    Interact();
                }
            }
            else
            {
                // Currently interacting - check for cancellation
                if (ShouldCancelInteraction())
                {
                    EndInteract();
                }
            }
        }

        /// <summary>
        /// Determines if the current interaction should be cancelled.
        /// Cancellation occurs when: player moves, or looks at different/no object.
        /// </summary>
        /// <returns>True if interaction should be cancelled.</returns>
        private bool ShouldCancelInteraction()
        {
            // Cancel if player starts moving
            if (moveAction != null)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                if (moveInput.magnitude > 0.1f)
                {
                    return true;
                }
            }

            // Cancel if no longer looking at an interactable
            if (currentInteractable == null)
            {
                return true;
            }

            // Cancel if looking at a different interactable
            if (currentInteractable != lastInteractable)
            {
                return true;
            }

            return false;
        }

        //=============================================================================
        // STATE METHODS
        //=============================================================================

        /// <summary>
        /// Called when player starts looking at an interactable object.
        /// Shows the interaction hint and notifies the object.
        /// </summary>
        private void ReadyInteract()
        {
            readyInteract = true;

            // Show interaction hint UI
            if (hint != null)
            {
                hint.SetActive(true);
            }

            // Update the prompt text with this object's action
            UpdatePromptText();

            // Notify the interactable object
            currentInteractable.OnReadyInteract();
        }

        /// <summary>
        /// Called when player stops looking at an interactable without interacting.
        /// Hides the interaction hint and notifies the object.
        /// </summary>
        private void AbortInteract()
        {
            readyInteract = false;

            // Hide interaction hint UI
            if (hint != null)
            {
                hint.SetActive(false);
            }

            // Notify the previously looked-at object
            if (lastInteractable != null)
            {
                lastInteractable.OnAbortInteract();
            }
        }

        /// <summary>
        /// Called when player presses the interact button.
        /// Initiates the interaction with the current object.
        /// </summary>
        private void Interact()
        {
            interacting = true;
            readyInteract = true;

            // Hide the hint during interaction
            if (hint != null)
            {
                hint.SetActive(false);
            }

            // Call the object's interact method, passing ourselves for callbacks
            currentInteractable.OnInteract(this);
        }

        /// <summary>
        /// Called when an active interaction ends (cancelled or completed).
        /// Cleans up state and notifies the object.
        /// </summary>
        private void EndInteract()
        {
            interacting = false;
            readyInteract = false;

            // Notify the object that interaction ended
            if (lastInteractable != null)
            {
                lastInteractable.OnEndInteract();
            }

            // Hide any displayed message
            if (interactorUI != null)
            {
                interactorUI.HideTextMessage();
            }
        }

        //=============================================================================
        // UI HELPERS
        //=============================================================================

        /// <summary>
        /// Updates the prompt text to show the current interactable's action.
        /// </summary>
        private void UpdatePromptText()
        {
            if (promptText != null && currentInteractable != null)
            {
                string actionName = currentInteractable.InteractionPrompt;
                promptText.text = string.Format(promptFormat, actionName);
            }
        }

        //=============================================================================
        // PUBLIC METHODS - Called by IInteractable objects
        //=============================================================================

        /// <summary>
        /// Called by interactable objects to end their own interaction.
        /// Validates that the requester is the current interactable.
        /// </summary>
        /// <param name="requester">The IInteractable requesting to end interaction.</param>
        public void EndInteract(IInteractable requester)
        {
            if (requester == lastInteractable)
            {
                EndInteract();
            }
        }

        /// <summary>
        /// Receives a text message from an interactable object and displays it.
        /// Used by objects like signs that want to show text to the player.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void ReceiveInteract(string message)
        {
            if (interactorUI != null)
            {
                interactorUI.ShowTextMessage(message);
            }
        }

        /// <summary>
        /// Receives an interactable object reference for advanced interactions.
        /// Override this in derived classes for custom behavior.
        /// </summary>
        /// <param name="interactable">The interactable object.</param>
        public void ReceiveInteract(IInteractable interactable)
        {
            // Reserved for advanced use cases
            Debug.Log($"[Interactor] Received interactable: {interactable}");
        }

        //=============================================================================
        // DEBUG VISUALIZATION
        //=============================================================================

        /// <summary>
        /// Draws the interaction raycast in the Scene view for debugging.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Camera cam = playerCamera != null ? playerCamera : Camera.main;
            if (cam == null) return;

            // Draw the raycast line
            Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
            Gizmos.DrawRay(cam.transform.position, cam.transform.forward * rayMaxDistance);

            // Draw a sphere at max range
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(cam.transform.position + cam.transform.forward * rayMaxDistance, 0.1f);
        }
    }
}
