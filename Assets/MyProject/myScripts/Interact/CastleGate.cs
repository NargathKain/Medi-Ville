using System.Collections;
using UnityEngine;

namespace MyProject.Interact
{
    /// <summary>
    /// A castle gate (portcullis) that raises and lowers vertically.
    /// Controlled externally by a Lever or other mechanism.
    /// Does not implement IInteractable directly - use Lever to control it.
    ///
    /// Setup:
    /// 1. Attach this script to the gate GameObject
    /// 2. Configure the raise height and movement speed
    /// 3. Reference this component from a Lever script
    ///
    /// The gate moves along its local Y-axis (up/down).
    /// </summary>
    public class CastleGate : MonoBehaviour
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Gate Movement")]

        /// <summary>
        /// How high the gate raises when opened (in local units).
        /// Measure from closed position to fully open position.
        /// </summary>
        [Tooltip("Height the gate raises when opened (local Y units).")]
        [SerializeField]
        private float raiseHeight = 5f;

        /// <summary>
        /// Speed at which the gate moves (units per second).
        /// Higher values create faster gate movement.
        /// </summary>
        [Tooltip("Gate movement speed (units per second).")]
        [SerializeField]
        private float moveSpeed = 2f;

        [Header("Audio (Optional)")]

        /// <summary>
        /// Sound played when the gate starts moving.
        /// Could be a chain/gear sound for a portcullis.
        /// </summary>
        [Tooltip("Sound played when gate starts moving.")]
        [SerializeField]
        private AudioClip moveSound;

        /// <summary>
        /// Sound played when the gate reaches its destination.
        /// Could be a heavy thud or clank.
        /// </summary>
        [Tooltip("Sound played when gate stops.")]
        [SerializeField]
        private AudioClip stopSound;

        /// <summary>
        /// Looping sound played while the gate is moving.
        /// Could be chains rattling or gears turning.
        /// </summary>
        [Tooltip("Looping sound while gate moves.")]
        [SerializeField]
        private AudioClip movingLoopSound;

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
        /// The gate's starting (closed/lowered) position.
        /// </summary>
        private Vector3 closedPosition;

        /// <summary>
        /// The gate's raised (open) position.
        /// Calculated as closedPosition + (up * raiseHeight).
        /// </summary>
        private Vector3 raisedPosition;

        /// <summary>
        /// Tracks whether the gate is currently raised (open).
        /// </summary>
        private bool isRaised = false;

        /// <summary>
        /// Tracks whether the gate is currently moving.
        /// </summary>
        private bool isMoving = false;

        /// <summary>
        /// Reference to the current movement coroutine.
        /// </summary>
        private Coroutine moveCoroutine;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        /// <summary>
        /// Initializes gate positions.
        /// </summary>
        private void Awake()
        {
            // Store the initial (closed) position
            closedPosition = transform.localPosition;

            // Calculate the raised position
            raisedPosition = closedPosition + Vector3.up * raiseHeight;

            // Find AudioSource if not assigned
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }
        }

        //=============================================================================
        // PUBLIC METHODS - Called by Lever or other controllers
        //=============================================================================

        /// <summary>
        /// Raises the gate to the open position.
        /// Call this from a Lever or switch.
        /// </summary>
        public void Raise()
        {
            if (isRaised || isMoving)
            {
                return;
            }

            // Stop any existing movement
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(MoveGate(raisedPosition, true));
        }

        /// <summary>
        /// Lowers the gate to the closed position.
        /// Call this from a Lever or switch.
        /// </summary>
        public void Lower()
        {
            if (!isRaised || isMoving)
            {
                return;
            }

            // Stop any existing movement
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
            }

            moveCoroutine = StartCoroutine(MoveGate(closedPosition, false));
        }

        /// <summary>
        /// Toggles the gate between raised and lowered states.
        /// </summary>
        public void Toggle()
        {
            if (isRaised)
            {
                Lower();
            }
            else
            {
                Raise();
            }
        }

        /// <summary>
        /// Immediately sets the gate to a specific state without animation.
        /// Useful for initialization or cutscenes.
        /// </summary>
        /// <param name="raised">True to set raised, false to set lowered.</param>
        public void SetStateImmediate(bool raised)
        {
            // Stop any current movement
            if (moveCoroutine != null)
            {
                StopCoroutine(moveCoroutine);
                isMoving = false;
            }

            // Set position immediately
            transform.localPosition = raised ? raisedPosition : closedPosition;
            isRaised = raised;
        }

        //=============================================================================
        // COROUTINES
        //=============================================================================

        /// <summary>
        /// Smoothly moves the gate to the target position.
        /// </summary>
        /// <param name="targetPosition">The position to move to.</param>
        /// <param name="raising">True if raising, false if lowering.</param>
        /// <returns>IEnumerator for coroutine.</returns>
        private IEnumerator MoveGate(Vector3 targetPosition, bool raising)
        {
            isMoving = true;

            // Play start sound
            PlaySound(moveSound);

            // Start looping sound
            if (movingLoopSound != null && audioSource != null)
            {
                audioSource.clip = movingLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }

            // Move towards target
            while (Vector3.Distance(transform.localPosition, targetPosition) > 0.01f)
            {
                transform.localPosition = Vector3.MoveTowards(
                    transform.localPosition,
                    targetPosition,
                    moveSpeed * Time.deltaTime
                );

                yield return null;
            }

            // Snap to exact position
            transform.localPosition = targetPosition;

            // Stop looping sound
            if (audioSource != null && audioSource.loop)
            {
                audioSource.loop = false;
                audioSource.Stop();
            }

            // Play stop sound
            PlaySound(stopSound);

            // Update state
            isRaised = raising;
            isMoving = false;
        }

        //=============================================================================
        // AUDIO
        //=============================================================================

        /// <summary>
        /// Plays a one-shot sound clip.
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
        /// Returns whether the gate is currently raised (open).
        /// </summary>
        public bool IsRaised => isRaised;

        /// <summary>
        /// Returns whether the gate is currently moving.
        /// </summary>
        public bool IsMoving => isMoving;

        //=============================================================================
        // DEBUG VISUALIZATION
        //=============================================================================

        /// <summary>
        /// Draws the gate's open and closed positions in the Scene view.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // Calculate positions (use current if not in play mode)
            Vector3 closed = Application.isPlaying ? closedPosition : transform.localPosition;
            Vector3 raised = closed + Vector3.up * raiseHeight;

            // Convert to world space for drawing
            Vector3 closedWorld = transform.parent != null
                ? transform.parent.TransformPoint(closed)
                : closed;
            Vector3 raisedWorld = transform.parent != null
                ? transform.parent.TransformPoint(raised)
                : raised;

            // Draw closed position
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(closedWorld, Vector3.one * 0.5f);

            // Draw raised position
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(raisedWorld, Vector3.one * 0.5f);

            // Draw movement path
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(closedWorld, raisedWorld);
        }
    }
}
