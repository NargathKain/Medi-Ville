using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;

namespace MyProject.Player
{
    /// <summary>
    /// Handles camera zoom functionality for the third-person controller.
    /// When the player holds the Zoom button (RMB or Gamepad RB), the camera smoothly
    /// zooms in by reducing the Cinemachine camera distance, creating an over-the-shoulder view.
    ///
    /// Requirements:
    /// - Cinemachine Virtual Camera with Cinemachine3rdPersonFollow component
    /// - PlayerInput component on the same GameObject (or parent)
    /// - "Zoom" action defined in the Input Actions asset
    ///
    /// Setup:
    /// 1. Attach this script to the PlayerArmature (same object with PlayerInput)
    /// 2. Assign the Cinemachine Virtual Camera reference in the Inspector
    /// 3. Ensure "Zoom" action exists in StarterAssets.inputactions (RMB / RB)
    /// 4. Adjust zoom distances and speed as desired
    /// </summary>
    public class CameraZoom : MonoBehaviour
    {
        //=============================================================================
        // SERIALIZED FIELDS - Configurable in Inspector
        //=============================================================================

        [Header("Camera Reference")]

        /// <summary>
        /// Reference to the Cinemachine Virtual Camera that follows the player.
        /// This camera must have a Cinemachine3rdPersonFollow component attached.
        /// </summary>
        [Tooltip("The Cinemachine Virtual Camera following the player.")]
        [SerializeField]
        private CinemachineVirtualCamera virtualCamera;

        [Header("Zoom Settings")]

        /// <summary>
        /// The default camera distance when not zooming.
        /// This is the normal third-person view distance.
        /// </summary>
        [Tooltip("Default camera distance when not zoomed in.")]
        [SerializeField]
        private float defaultDistance = 4f;

        /// <summary>
        /// The camera distance when zoomed in (holding right mouse button).
        /// Lower values bring the camera closer for an over-the-shoulder view.
        /// </summary>
        [Tooltip("Camera distance when zoomed in (holding RMB).")]
        [SerializeField]
        private float zoomedDistance = 1.5f;

        /// <summary>
        /// How fast the camera transitions between default and zoomed distances.
        /// Higher values create snappier zoom, lower values create smoother transitions.
        /// </summary>
        [Tooltip("Speed of zoom transition. Higher = faster.")]
        [SerializeField]
        private float zoomSpeed = 8f;

        //=============================================================================
        // PRIVATE FIELDS
        //=============================================================================

        /// <summary>
        /// Reference to the Cinemachine3rdPersonFollow component that controls camera positioning.
        /// Cached on Start for performance.
        /// </summary>
        private Cinemachine3rdPersonFollow thirdPersonFollow;

        /// <summary>
        /// Reference to the PlayerInput component for reading input actions.
        /// </summary>
        private PlayerInput playerInput;

        /// <summary>
        /// Reference to the Zoom input action from the StarterAssets input actions.
        /// </summary>
        private InputAction zoomAction;

        /// <summary>
        /// The target distance we're lerping towards (either defaultDistance or zoomedDistance).
        /// </summary>
        private float targetDistance;

        /// <summary>
        /// Tracks whether the zoom button is currently being held.
        /// </summary>
        private bool isZooming;

        //=============================================================================
        // UNITY LIFECYCLE METHODS
        //=============================================================================

        /// <summary>
        /// Called when the script instance is being loaded.
        /// Caches the Cinemachine3rdPersonFollow component, PlayerInput, and Zoom action.
        /// </summary>
        private void Start()
        {
            // Validate that we have a virtual camera assigned
            if (virtualCamera == null)
            {
                Debug.LogError($"[{gameObject.name}] CameraZoom: No Virtual Camera assigned!");
                enabled = false; // Disable the script to prevent errors
                return;
            }

            // Get the 3rdPersonFollow component from the virtual camera's Body
            // In Cinemachine 2.x, body components are accessed via GetCinemachineComponent
            thirdPersonFollow = virtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>();

            // Validate that the camera has the required component
            if (thirdPersonFollow == null)
            {
                Debug.LogError($"[{gameObject.name}] CameraZoom: Virtual Camera does not have " +
                              "Cinemachine3rdPersonFollow component! Make sure Body is set to '3rd Person Follow'.");
                enabled = false;
                return;
            }

            // Get the PlayerInput component from this GameObject or its parent
            // PlayerInput is typically on the PlayerArmature root object
            playerInput = GetComponentInParent<PlayerInput>();

            if (playerInput == null)
            {
                Debug.LogError($"[{gameObject.name}] CameraZoom: No PlayerInput component found! " +
                              "Make sure this script is on the PlayerArmature or a child of it.");
                enabled = false;
                return;
            }

            // Get the Zoom action from the PlayerInput's action map
            // This action should be defined in StarterAssets.inputactions
            zoomAction = playerInput.actions["Zoom"];

            if (zoomAction == null)
            {
                Debug.LogError($"[{gameObject.name}] CameraZoom: 'Zoom' action not found in Input Actions! " +
                              "Make sure 'Zoom' action exists in StarterAssets.inputactions.");
                enabled = false;
                return;
            }

            // Initialize target distance to default (not zoomed)
            targetDistance = defaultDistance;

            // Ensure the camera starts at the default distance
            thirdPersonFollow.CameraDistance = defaultDistance;
        }

        /// <summary>
        /// Called every frame. Checks for zoom input and smoothly interpolates camera distance.
        /// </summary>
        private void Update()
        {
            // Check for right mouse button input using the new Input System
            HandleZoomInput();

            // Smoothly interpolate the camera distance towards the target
            SmoothZoom();
        }

        //=============================================================================
        // INPUT HANDLING
        //=============================================================================

        /// <summary>
        /// Reads the Zoom action state from the Input System.
        /// Sets isZooming to true while the button is held, false when released.
        /// Supports both keyboard/mouse (RMB) and gamepad (Right Shoulder) input.
        /// </summary>
        private void HandleZoomInput()
        {
            // Check if the Zoom action is currently being pressed
            // ReadValue<float>() returns 1.0 when pressed, 0.0 when released
            // This works for both button presses (RMB) and gamepad triggers (RB)
            if (zoomAction != null)
            {
                isZooming = zoomAction.ReadValue<float>() > 0.5f;
            }

            // Set target distance based on zoom state
            // When zooming, we want the camera closer (zoomedDistance)
            // When not zooming, we want the default distance
            targetDistance = isZooming ? zoomedDistance : defaultDistance;
        }

        //=============================================================================
        // ZOOM LOGIC
        //=============================================================================

        /// <summary>
        /// Smoothly interpolates the camera distance from current value to target value.
        /// Uses Lerp for smooth transitions that feel natural and polished.
        /// </summary>
        private void SmoothZoom()
        {
            // Only process if we have a valid reference
            if (thirdPersonFollow == null)
            {
                return;
            }

            // Get the current camera distance
            float currentDistance = thirdPersonFollow.CameraDistance;

            // Check if we're already at the target (with small tolerance to avoid floating point issues)
            if (Mathf.Abs(currentDistance - targetDistance) < 0.01f)
            {
                // Snap to exact target to prevent tiny oscillations
                thirdPersonFollow.CameraDistance = targetDistance;
                return;
            }

            // Smoothly interpolate towards the target distance
            // Lerp moves from current to target by a fraction (zoomSpeed * deltaTime) each frame
            // This creates a smooth ease-out effect as the camera approaches the target
            float newDistance = Mathf.Lerp(currentDistance, targetDistance, zoomSpeed * Time.deltaTime);

            // Apply the new distance to the camera
            thirdPersonFollow.CameraDistance = newDistance;
        }

        //=============================================================================
        // PUBLIC METHODS (for external access if needed)
        //=============================================================================

        /// <summary>
        /// Returns whether the camera is currently zoomed in.
        /// Useful for other scripts that need to know the zoom state
        /// (e.g., to show/hide crosshair, change sensitivity, etc.)
        /// </summary>
        public bool IsZoomed => isZooming;

        /// <summary>
        /// Allows external scripts to change the default zoom distance at runtime.
        /// </summary>
        /// <param name="distance">The new default camera distance.</param>
        public void SetDefaultDistance(float distance)
        {
            defaultDistance = distance;

            // Update target if not currently zooming
            if (!isZooming)
            {
                targetDistance = defaultDistance;
            }
        }

        /// <summary>
        /// Allows external scripts to change the zoomed distance at runtime.
        /// </summary>
        /// <param name="distance">The new zoomed camera distance.</param>
        public void SetZoomedDistance(float distance)
        {
            zoomedDistance = distance;

            // Update target if currently zooming
            if (isZooming)
            {
                targetDistance = zoomedDistance;
            }
        }
    }
}
