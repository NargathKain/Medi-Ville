using UnityEngine;

namespace MyProject.Interact
{
    /// <summary>
    /// An interactable sign that displays a text message when the player interacts with it.
    /// Use this for information boards, notices, lore items, or any readable object.
    ///
    /// Setup:
    /// 1. Attach this script to a sign/board GameObject
    /// 2. Add a Collider component (for raycast detection)
    /// 3. Tag the GameObject as "Interactable"
    /// 4. Assign an indicator GameObject (optional highlight/outline)
    /// 5. Enter the text to display in the text field
    ///
    /// Version 2.0 - Added InteractionPrompt property
    /// </summary>
    public class TextSign : MonoBehaviour, IInteractable
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("Visual Feedback")]

        /// <summary>
        /// Optional indicator GameObject that shows when the player is looking at the sign.
        /// This could be a highlight effect, outline, or floating icon.
        /// </summary>
        [Tooltip("Visual indicator shown when player can interact (e.g., highlight mesh).")]
        [SerializeField]
        private GameObject indicator;

        [Header("Sign Content")]

        /// <summary>
        /// The text message displayed when the player reads the sign.
        /// Use TextArea attribute to allow multi-line editing in Inspector.
        /// </summary>
        [Tooltip("Text displayed when player interacts with the sign.")]
        [TextArea(3, 10)]
        [SerializeField]
        private string text = "Sign text goes here...";

        /// <summary>
        /// Custom prompt text shown to the player (e.g., "Read Sign", "Examine Notice").
        /// </summary>
        [Tooltip("Text shown in the interaction prompt (e.g., 'Read Sign').")]
        [SerializeField]
        private string promptText = "Read";

        //=============================================================================
        // IINTERACTABLE IMPLEMENTATION
        //=============================================================================

        /// <summary>
        /// The text shown in the "Press E to..." prompt.
        /// Returns the custom prompt text set in the Inspector.
        /// </summary>
        public string InteractionPrompt => promptText;

        /// <summary>
        /// Called when the player presses the interact button while looking at the sign.
        /// Sends the sign's text to the Interactor for display on screen.
        /// </summary>
        /// <param name="interactor">The Interactor that initiated the interaction.</param>
        public void OnInteract(Interactor interactor)
        {
            // Hide the indicator during reading
            if (indicator != null)
            {
                indicator.SetActive(false);
            }

            // Send the text to the Interactor, which will display it via InteractorUI
            interactor.ReceiveInteract(text);
        }

        /// <summary>
        /// Called when the interaction ends (player moves away or cancels).
        /// Signs don't need special cleanup, but the method must be implemented.
        /// </summary>
        public void OnEndInteract()
        {
            // No special cleanup needed for signs
        }

        /// <summary>
        /// Called when the player stops looking at the sign before interacting.
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
        /// Called when the player starts looking at the sign.
        /// Shows the visual indicator to signal that interaction is possible.
        /// </summary>
        public void OnReadyInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }
    }
}
