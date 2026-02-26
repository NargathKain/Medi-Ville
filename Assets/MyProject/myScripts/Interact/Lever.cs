using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// <summary>
    /// An interactable lever that controls a CastleGate (or other mechanism).
    /// When pulled, the lever rotates and triggers the connected gate to raise/lower.
    ///
    /// Setup:
    /// 1. Attach this script to the lever GameObject
    /// 2. Add a Collider for raycast detection
    /// 3. Tag as "Interactable"
    /// 4. Assign the lever handle Transform (the part that rotates)
    /// 5. Assign the CastleGate reference to control
    /// 6. Configure rotation angle and speed
    ///
    /// The lever can control any object with a CastleGate component,
    /// or you can extend it to control other mechanisms via UnityEvents.
    /// </summary>
    public class Lever : MonoBehaviour, IInteractable
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Visual Feedback")]

        /// <summary>
        /// Optional indicator shown when player can interact with the lever.
        /// </summary>
        [Tooltip("Visual indicator shown when player can interact.")]
        [SerializeField]
        private GameObject indicator;

        [Header("Lever Handle")]

        /// <summary>
        /// The lever handle Transform that rotates when pulled.
        /// If not assigned, the script will rotate this GameObject.
        /// </summary>
        [Tooltip("The lever handle that rotates. Uses this object if not assigned.")]
        [SerializeField]
        private Transform leverHandle;

        /// <summary>
        /// The rotation angle when the lever is pulled (in degrees).
        /// Positive or negative depending on desired direction.
        /// </summary>
        [Tooltip("Rotation angle when lever is pulled (degrees).")]
        [SerializeField]
        private float pullAngle = 45f;

        /// <summary>
        /// How fast the lever rotates in degrees per second.
        /// </summary>
        [Tooltip("Lever rotation speed (degrees per second).")]
        [SerializeField]
        private float rotationSpeed = 90f;

        /// <summary>
        /// The axis around which the lever rotates.
        /// Default is X-axis (forward/backward pull).
        /// </summary>
        [Tooltip("Local rotation axis for the lever.")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.right;

        [Header("Connected Gate")]

        /// <summary>
        /// Reference to the CastleGate this lever controls.
        /// When the lever is pulled, the gate raises/lowers.
        /// </summary>
        [Tooltip("The CastleGate this lever controls.")]
        [SerializeField]
        private CastleGate connectedGate;

        [Header("Behavior")]

        /// <summary>
        /// If true, the lever toggles between pulled/unpulled states.
        /// If false, the lever returns to unpulled position automatically.
        /// </summary>
        [Tooltip("If true, lever stays in pulled position until pulled again.")]
        [SerializeField]
        private bool toggleMode = true;

        /// <summary>
        /// Delay before the gate starts moving after the lever is pulled.
        /// Creates a more realistic mechanical feel.
        /// </summary>
        [Tooltip("Delay before gate moves after lever is pulled (seconds).")]
        [SerializeField]
        private float gateActivationDelay = 0.2f;

        [Header("Audio (Optional)")]

        /// <summary>
        /// Sound played when the lever is pulled.
        /// </summary>
        [Tooltip("Sound when lever is pulled.")]
        [SerializeField]
        private AudioClip pullSound;

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
        /// Tracks whether the lever is in the pulled position.
        /// </summary>
        private bool isPulled = false;

        /// <summary>
        /// Tracks whether the lever is currently animating.
        /// </summary>
        private bool isMoving = false;

        /// <summary>
        /// The lever's initial (unpulled) rotation.
        /// </summary>
        private Quaternion unpulledRotation;

        /// <summary>
        /// The lever's pulled rotation.
        /// </summary>
        private Quaternion pulledRotation;

        /// <summary>
        /// Reference to the current animation coroutine.
        /// </summary>
        private Coroutine animationCoroutine;

        /// <summary>
        /// The Transform that actually rotates (handle or self).
        /// </summary>
        private Transform rotatingPart;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        /// <summary>
        /// Initializes lever rotations and validates setup.
        /// </summary>
        private void Awake()
        {
            // Determine which transform rotates
            rotatingPart = leverHandle != null ? leverHandle : transform;

            // Store initial rotation
            unpulledRotation = rotatingPart.localRotation;

            // Calculate pulled rotation
            pulledRotation = unpulledRotation * Quaternion.AngleAxis(pullAngle, rotationAxis);

            // Find AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            // Validate gate reference
            if (connectedGate == null)
            {
                Debug.LogWarning($"[Lever] {gameObject.name}: No CastleGate assigned! " +
                                "Lever will animate but won't control anything.");
            }
        }

        //=============================================================================
        // IINTERACTABLE IMPLEMENTATION
        //=============================================================================

        /// <summary>
        /// The prompt shown to the player.
        /// Changes based on lever state and gate state.
        /// </summary>
        public string InteractionPrompt
        {
            get
            {
                // Don't allow interaction while lever or gate is moving
                if (isMoving || (connectedGate != null && connectedGate.IsMoving))
                {
                    return "";
                }

                if (toggleMode)
                {
                    return isPulled ? "Push Lever" : "Pull Lever";
                }
                else
                {
                    return "Pull Lever";
                }
            }
        }

        /// <summary>
        /// Called when player interacts with the lever.
        /// Pulls (or pushes) the lever and activates the connected gate.
        /// </summary>
        /// <param name="interactor">The Interactor that initiated interaction.</param>
        public void OnInteract(Interactor interactor)
        {
            // Don't allow interaction while moving
            if (isMoving)
            {
                return;
            }

            // Don't allow interaction while gate is moving
            if (connectedGate != null && connectedGate.IsMoving)
            {
                return;
            }

            // Toggle or pull based on mode
            if (toggleMode)
            {
                if (isPulled)
                {
                    Push();
                }
                else
                {
                    Pull();
                }
            }
            else
            {
                // Non-toggle mode: pull then auto-return
                Pull();
            }

            // End interaction immediately
            interactor.EndInteract(this);
        }

        /// <summary>
        /// Called when interaction ends.
        /// </summary>
        public void OnEndInteract()
        {
            // No cleanup needed
        }

        /// <summary>
        /// Called when player looks away from the lever.
        /// </summary>
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// <summary>
        /// Called when player starts looking at the lever.
        /// </summary>
        public void OnReadyInteract()
        {
            // Don't show indicator if lever or gate is moving
            if (isMoving || (connectedGate != null && connectedGate.IsMoving))
            {
                return;
            }

            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }

        //=============================================================================
        // LEVER OPERATIONS
        //=============================================================================

        /// <summary>
        /// Pulls the lever to the activated position.
        /// </summary>
        public void Pull()
        {
            if (isPulled || isMoving)
            {
                return;
            }

            PlaySound(pullSound);
            animationCoroutine = StartCoroutine(AnimateLever(pulledRotation, true));
        }

        /// <summary>
        /// Pushes the lever back to the deactivated position.
        /// </summary>
        public void Push()
        {
            if (!isPulled || isMoving)
            {
                return;
            }

            PlaySound(pullSound);
            animationCoroutine = StartCoroutine(AnimateLever(unpulledRotation, false));
        }

        //=============================================================================
        // COROUTINES
        //=============================================================================

        /// <summary>
        /// Animates the lever rotation and triggers the gate.
        /// </summary>
        /// <param name="targetRotation">Target rotation for the lever.</param>
        /// <param name="pulling">True if pulling, false if pushing.</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator AnimateLever(Quaternion targetRotation, bool pulling)
        {
            isMoving = true;

            // Animate lever rotation
            while (Quaternion.Angle(rotatingPart.localRotation, targetRotation) > 0.1f)
            {
                rotatingPart.localRotation = Quaternion.RotateTowards(
                    rotatingPart.localRotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
                yield return null;
            }

            // Snap to exact rotation
            rotatingPart.localRotation = targetRotation;

            // Update state
            isPulled = pulling;
            isMoving = false;

            // Wait before activating gate (mechanical delay)
            if (gateActivationDelay > 0)
            {
                yield return new WaitForSeconds(gateActivationDelay);
            }

            // Activate the connected gate
            ActivateGate(pulling);

            // For non-toggle mode, return lever to unpulled position after gate finishes
            if (!toggleMode && pulling)
            {
                // Wait for gate to finish moving
                if (connectedGate != null)
                {
                    while (connectedGate.IsMoving)
                    {
                        yield return null;
                    }
                }

                // Return lever to unpulled position
                yield return new WaitForSeconds(0.5f);
                Push();
            }
        }

        /// <summary>
        /// Activates the connected gate based on lever state.
        /// </summary>
        /// <param name="pulled">True if lever was pulled, false if pushed.</param>
        private void ActivateGate(bool pulled)
        {
            if (connectedGate == null)
            {
                return;
            }

            if (pulled)
            {
                // Lever pulled - raise the gate
                connectedGate.Raise();
            }
            else
            {
                // Lever pushed - lower the gate
                connectedGate.Lower();
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
        /// Returns whether the lever is currently pulled.
        /// </summary>
        public bool IsPulled => isPulled;

        /// <summary>
        /// Returns whether the lever is currently animating.
        /// </summary>
        public bool IsMoving => isMoving;
    }
}
