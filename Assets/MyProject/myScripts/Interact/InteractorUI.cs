using UnityEngine;
using TMPro;

namespace MyProject.Interact
{
    /// <summary>
    /// UI helper for the Interactor system.
    /// Handles displaying text messages from interactable objects on the screen.
    /// Attach this to a Canvas or UI manager GameObject.
    ///
    /// Setup:
    /// 1. Create a TextMeshProUGUI element on your Canvas for displaying messages
    /// 2. Assign it to the messageText field
    /// 3. Reference this component in the Interactor script
    ///
    /// Version 2.0 - Added namespace and improved comments
    /// </summary>
    public class InteractorUI : MonoBehaviour
    {
        //=============================================================================
        // SERIALIZED FIELDS
        //=============================================================================

        [Header("UI References")]

        /// <summary>
        /// The TextMeshProUGUI element that displays interaction messages.
        /// This is where text from signs, books, etc. will be shown.
        /// </summary>
        [Tooltip("TextMeshPro element for displaying interaction messages.")]
        [SerializeField]
        private TextMeshProUGUI messageText;

        //=============================================================================
        // UNITY LIFECYCLE
        //=============================================================================

        /// <summary>
        /// Initializes the UI by hiding the message text.
        /// </summary>
        private void Start()
        {
            // Start with message hidden
            HideTextMessage();

            // Warn if reference is missing
            if (messageText == null)
            {
                Debug.LogWarning("[InteractorUI] MessageText not assigned. Messages won't display.");
            }
        }

        //=============================================================================
        // PUBLIC METHODS
        //=============================================================================

        /// <summary>
        /// Displays a text message on screen.
        /// Called by interactable objects (via Interactor.ReceiveInteract) to show text.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public void ShowTextMessage(string message)
        {
            if (messageText == null)
            {
                Debug.LogWarning("[InteractorUI] ShowTextMessage: messageText not assigned.");
                return;
            }

            // Set the text content
            messageText.text = message;

            // Show the text element
            messageText.gameObject.SetActive(true);
        }

        /// <summary>
        /// Hides the message text UI element.
        /// Called when interaction ends or is cancelled.
        /// </summary>
        public void HideTextMessage()
        {
            if (messageText == null)
            {
                Debug.LogWarning("[InteractorUI] HideTextMessage: messageText not assigned.");
                return;
            }

            // Clear text content for safety
            messageText.text = "";

            // Hide the text element
            messageText.gameObject.SetActive(false);
        }
    }
}
