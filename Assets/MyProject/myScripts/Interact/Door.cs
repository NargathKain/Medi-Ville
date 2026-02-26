using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// <summary>
    /// An interactable door that rotates open and closed when the player interacts.
    /// Supports configurable rotation angle, speed, and pivot direction.
    ///
    /// Setup:
    /// 1. Attach this script to the door GameObject (or a parent pivot object)
    /// 2. Add a Collider component for raycast detection
    /// 3. Tag the GameObject as "Interactable"
    /// 4. Ensure the door's pivot point is at the hinge location
    /// 5. Configure rotation angle and speed in Inspector
    ///
    /// Note: The door rotates around its local Y-axis (up). Position the pivot
    /// at the hinge edge for realistic door movement.
    /// </summary>
    public class Door : MonoBehaviour, IInteractable
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Visual Feedback")]

        /// <summary>
        /// Optional indicator shown when player can interact with the door.
        /// </summary>
        [Tooltip("Visual indicator shown when player can interact.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Door Settings")]

        /// <summary>
        /// The angle in degrees the door rotates when opened.
        /// Positive values rotate counter-clockwise (when viewed from above).
        /// Typical values: 90 for standard doors, 110 for wide swing.
        /// </summary>
        [Tooltip("Rotation angle in degrees when door opens.")]
        [SerializeField]
        private float openAngle = 90f;

        /// <summary>
        /// How fast the door rotates in degrees per second.
        /// Higher values create faster door movement.
        /// </summary>
        [Tooltip("Rotation speed in degrees per second.")]
        [SerializeField]
        private float rotationSpeed = 180f;

        /// <summary>
        /// The axis around which the door rotates.
        /// Default is Vector3.up (Y-axis) for horizontal hinge doors.
        /// </summary>
        [Tooltip("Local axis of rotation (usually Y-up for doors).")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.up;

        [Header("Audio (Optional)")]

        /// <summary>
        /// Sound played when the door starts opening.
        /// </summary>
        [Tooltip("Sound played when door opens.")]
        [SerializeField]
        private AudioClip openSound;

        /// <summary>
        /// Sound played when the door starts closing.
        /// </summary>
        [Tooltip("Sound played when door closes.")]
        [SerializeField]
        private AudioClip closeSound;

        /// <summary>
        /// AudioSource component for playing door sounds.
        /// If not assigned, attempts to find one on this GameObject.
        /// </summary>
        [Tooltip("AudioSource for playing sounds. Auto-found if not assigned.")]
        [SerializeField]
        private AudioSource audioSource;

        //=============================================================================
        // PRIVATE FIELDS
        //=============================================================================

        /// <summary>
        /// Tracks whether the door is currently open or closed.
        /// </summary>
        private bool isOpen = false;

        /// <summary>
        /// Tracks whether the door is currently animating.
        /// Prevents multiple interactions during movement.
        /// </summary>
        private bool isMoving = false;

        /// <summary>
        /// The door's initial rotation, used as the "closed" state.
        /// </summary>
        private Quaternion closedRotation;

        /// <summary>
        /// The door's target rotation when fully open.
        /// Calculated from closedRotation + openAngle.
        /// </summary>
        private Quaternion openRotation;

        /// <summary>
        /// Reference to the current rotation coroutine.
        /// Allows stopping mid-animation if needed.
        /// </summary>
        private Coroutine rotationCoroutine;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        /// <summary>
        /// Initializes rotation values and caches components.
        /// </summary>
        private void Awake()
        {
            // Store the initial (closed) rotation
            closedRotation = transform.localRotation;

            // Calculate the open rotation by adding the open angle around the rotation axis
            openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis);

            // Try to find AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        //=============================================================================
        // IINTERACTABLE IMPLEMENTATION
        //=============================================================================

        /// <summary>
        /// The text shown in the interaction prompt.
        /// Changes based on whether the door is open or closed.
        /// </summary>
        public string InteractionPrompt => isOpen ? "Close Door" : "Open Door";

        /// <summary>
        /// Called when the player interacts with the door.
        /// Toggles between open and closed states.
        /// </summary>
        /// <param name="interactor">The Interactor that initiated the interaction.</param>
        public void OnInteract(Interactor interactor)
        {
            // Don't allow interaction while door is moving
            if (isMoving)
            {
                return;
            }

            // Toggle the door state
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }

            // End the interaction immediately (door doesn't need sustained interaction)
            interactor.EndInteract(this);
        }

        /// <summary>
        /// Called when interaction ends. Not used for doors.
        /// </summary>
        public void OnEndInteract()
        {
            // Doors don't need sustained interaction, so nothing to clean up
        }

        /// <summary>
        /// Called when player looks away from the door.
        /// Hides the visual indicator.
        /// </summary>
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// <summary>
        /// Called when player starts looking at the door.
        /// Shows the visual indicator.
        /// </summary>
        public void OnReadyInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        //=============================================================================
        // DOOR OPERATIONS
        //=============================================================================

        /// <summary>
        /// Opens the door by rotating to the open position.
        /// </summary>
        public void Open()
        {
            if (isOpen || isMoving)
            {
                return;
            }

            // Play open sound
            PlaySound(openSound);

            // Start rotation coroutine
            rotationCoroutine = StartCoroutine(RotateDoor(openRotation, true));
        }

        /// <summary>
        /// Closes the door by rotating to the closed position.
        /// </summary>
        public void Close()
        {
            if (!isOpen || isMoving)
            {
                return;
            }

            // Play close sound
            PlaySound(closeSound);

            // Start rotation coroutine
            rotationCoroutine = StartCoroutine(RotateDoor(closedRotation, false));
        }

        /// <summary>
        /// Toggles the door between open and closed states.
        /// </summary>
        public void Toggle()
        {
            if (isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        //=============================================================================
        // COROUTINES
        //=============================================================================

        /// <summary>
        /// Smoothly rotates the door to the target rotation over time.
        /// Uses Quaternion.RotateTowards for consistent angular speed.
        /// </summary>
        /// <param name="targetRotation">The rotation to rotate towards.</param>
        /// <param name="opening">True if opening, false if closing.</param>
        /// <returns>IEnumerator for coroutine execution.</returns>
        private IEnumerator RotateDoor(Quaternion targetRotation, bool opening)
        {
            isMoving = true;

            // Rotate until we reach the target
            while (Quaternion.Angle(transform.localRotation, targetRotation) > 0.1f)
            {
                // Rotate towards target at constant speed
                transform.localRotation = Quaternion.RotateTowards(
                    transform.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Snap to exact target rotation
            transform.localRotation = targetRotation;

            // Update state
            isOpen = opening;
            isMoving = false;
        }

        //=============================================================================
        // AUDIO
        //=============================================================================

        /// <summary>
        /// Plays a sound clip if an AudioSource is available.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
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
        /// Returns whether the door is currently open.
        /// </summary>
        public bool IsOpen => isOpen;

        /// <summary>
        /// Returns whether the door is currently moving.
        /// </summary>
        public bool IsMoving => isMoving;
    }
}
