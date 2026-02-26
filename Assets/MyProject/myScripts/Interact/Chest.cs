using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// <summary>
    /// An interactable chest that opens its lid when the player interacts.
    /// The lid rotates around a pivot point to simulate opening/closing.
    ///
    /// Setup:
    /// 1. Create a chest model with a separate lid object as a child
    /// 2. Position the lid's pivot point at the hinge (back edge of lid)
    /// 3. Attach this script to the chest body (with the collider)
    /// 4. Assign the lid Transform reference
    /// 5. Tag the chest as "Interactable"
    ///
    /// Hierarchy example:
    ///   Chest (this script + collider)
    ///   └── Lid (Transform reference)
    /// </summary>
    public class Chest : MonoBehaviour, IInteractable
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Visual Feedback")]

        /// <summary>
        /// Optional indicator shown when player can interact with the chest.
        /// </summary>
        [Tooltip("Visual indicator shown when player can interact.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Lid Settings")]

        /// <summary>
        /// Reference to the chest lid Transform that will rotate when opened.
        /// This should be a child object with its pivot at the hinge point.
        /// </summary>
        [Tooltip("The lid Transform that rotates when chest opens.")]
        [SerializeField]
        private Transform lid;

        /// <summary>
        /// The angle the lid rotates when opened (in degrees).
        /// Positive values typically rotate the lid backwards.
        /// </summary>
        [Tooltip("Rotation angle when lid opens (degrees).")]
        [SerializeField]
        private float openAngle = -110f;

        /// <summary>
        /// How fast the lid rotates in degrees per second.
        /// </summary>
        [Tooltip("Lid rotation speed (degrees per second).")]
        [SerializeField]
        private float rotationSpeed = 180f;

        /// <summary>
        /// The local axis around which the lid rotates.
        /// Default is X-axis for a lid that opens backward.
        /// </summary>
        [Tooltip("Local axis of lid rotation.")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.right;

        [Header("Chest Content (Optional)")]

        /// <summary>
        /// Text displayed when the chest is opened.
        /// Can describe what's inside the chest.
        /// </summary>
        [Tooltip("Optional message shown when chest is opened.")]
        [TextArea(2, 5)]
        [SerializeField]
        private string contentMessage = "";

        /// <summary>
        /// If true, the chest can only be opened once and stays open.
        /// </summary>
        [Tooltip("If true, chest stays open permanently after first interaction.")]
        [SerializeField]
        private bool oneTimeOnly = false;

        [Header("Audio (Optional)")]

        /// <summary>
        /// Sound played when the chest opens.
        /// </summary>
        [Tooltip("Sound played when chest opens.")]
        [SerializeField]
        private AudioClip openSound;

        /// <summary>
        /// Sound played when the chest closes.
        /// </summary>
        [Tooltip("Sound played when chest closes.")]
        [SerializeField]
        private AudioClip closeSound;

        /// <summary>
        /// AudioSource for playing sounds.
        /// </summary>
        [Tooltip("AudioSource for sounds. Auto-found if not assigned.")]
        [SerializeField]
        private AudioSource audioSource;

        //=============================================================================
        // PRIVATE FIELDS
        //=============================================================================

        /// <summary>
        /// Tracks whether the chest is open or closed.
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// Tracks whether the lid is currently animating.
        /// </summary>
        private bool isMoving = false;

        /// <summary>
        /// Tracks whether the chest has been opened (for one-time chests).
        /// </summary>
        private bool hasBeenOpened = false;

        /// <summary>
        /// The lid's initial (closed) rotation.
        /// </summary>
        private Quaternion closedRotation;

        /// <summary>
        /// The lid's target rotation when open.
        /// </summary>
        private Quaternion openRotation;

        /// <summary>
        /// Reference to the running animation coroutine.
        /// </summary>
        private Coroutine animationCoroutine;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        /// <summary>
        /// Initializes lid rotations and validates setup.
        /// </summary>
        private void Awake()
        {
            // Validate lid reference
            if (lid == null)
            {
                Debug.LogError($"[Chest] {gameObject.name}: Lid Transform not assigned!");
                enabled = false;
                return;
            }

            // Store closed rotation
            closedRotation = lid.localRotation;

            // Calculate open rotation
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            // Find AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        //=============================================================================
        // IINTERACTABLE IMPLEMENTATION
        //=============================================================================

        /// <summary>
        /// The prompt shown to the player.
        /// Shows "Open Chest" or "Close Chest" based on state.
        /// For one-time chests that are open, returns empty to disable further interaction.
        /// </summary>
        public string InteractionPrompt
        {
            get
            {
                if (oneTimeOnly && hasBeenOpened)
                {
                    return ""; // No more interaction possible
                }
                return isOpen ? "Close Chest" : "Open Chest";
            }
        }

        /// <summary>
        /// Called when player interacts with the chest.
        /// Toggles the lid open/closed.
        /// </summary>
        /// <param name="interactor">The Interactor that initiated interaction.</param>
        public void OnInteract(Interactor interactor)
        {
            // Don't allow interaction while animating
            if (isMoving)
            {
                return;
            }

            // Don't allow interaction if one-time chest already opened
            if (oneTimeOnly && hasBeenOpened)
            {
                return;
            }

            // Toggle chest state
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open(interactor);
            }

            // End interaction immediately
            interactor.EndInteract(this);
        }

        /// <summary>
        /// Called when interaction ends.
        /// </summary>
        public void OnEndInteract()
        {
            // No cleanup needed - chest handles its own state
        }

        /// <summary>
        /// Called when player looks away from the chest.
        /// </summary>
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// <summary>
        /// Called when player starts looking at the chest.
        /// </summary>
        public void OnReadyInteract()
        {
            // Don't show indicator for one-time chests that are already open
            if (oneTimeOnly && hasBeenOpened)
            {
                return;
            }

            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        //=============================================================================
        // CHEST OPERATIONS
        //=============================================================================

        /// <summary>
        /// Opens the chest lid.
        /// </summary>
        /// <param name="interactor">Optional interactor to receive content message when animation completes.</param>
        public void Open(Interactor interactor = null)
        {
            if (isOpen || isMoving)
            {
                return;
            }

            PlaySound(openSound);
            animationCoroutine = StartCoroutine(AnimateLid(openRotation, true, interactor));
        }

        /// <summary>
        /// Closes the chest lid.
        /// </summary>
        public void Close()
        {
            if (!isOpen || isMoving)
            {
                return;
            }

            // Don't allow closing one-time chests
            if (oneTimeOnly && hasBeenOpened)
            {
                return;
            }

            PlaySound(closeSound);
            animationCoroutine = StartCoroutine(AnimateLid(closedRotation, false, null));
        }

        //=============================================================================
        // COROUTINES
        //=============================================================================

        /// <summary>
        /// Animates the lid rotation smoothly over time.
        /// </summary>
        /// <param name="targetRotation">Target rotation for the lid.</param>
        /// <param name="opening">True if opening, false if closing.</param>
        /// <param name="interactor">Interactor to receive content message (can be null).</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator AnimateLid(Quaternion targetRotation, bool opening, Interactor interactor)
        {
            isMoving = true;

            // Animate until reaching target
            while (Quaternion.Angle(lid.localRotation, targetRotation) > 0.1f)
            {
                lid.localRotation = Quaternion.RotateTowards(
                    lid.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Snap to exact rotation
            lid.localRotation = targetRotation;

            // Update state
            isOpen = opening;
            isMoving = false;

            // Mark as opened for one-time chests
            if (opening)
            {
                hasBeenOpened = true;

                // Show content message if available
                if (!string.IsNullOrEmpty(contentMessage) && interactor != null)
                {
                    interactor.ReceiveInteract(contentMessage);
                }
            }
        }

        //=============================================================================
        // AUDIO
        //=============================================================================

        /// <summary>
        /// Plays a sound clip.
        /// </summary>
        /// <param name="clip">The clip to play.</param>
        private void PlaySound(AudioClip clip)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        //=============================================================================
        // PUBLIC ACCESSORS
        //=============================================================================

        /// <summary>
        /// Returns whether the chest is currently open.
        /// </summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Returns whether the chest has been opened at least once.
        /// </summary>
        public bool HasBeenOpened => hasBeenOpened;
    }
}
