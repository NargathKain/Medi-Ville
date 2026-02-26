using UnityEngine;

namespace MyProject.Interact
{
    /// <summary>
    /// Interface for all interactable objects in the game.
    /// Any object that the player can interact with must implement this interface.
    /// The Interactor script detects objects implementing IInteractable via raycast
    /// and calls the appropriate methods based on player input and gaze direction.
    ///
    /// Lifecycle:
    /// 1. Player looks at object → OnReadyInteract()
    /// 2. Player presses Interact → OnInteract(interactor)
    /// 3. Player releases/cancels → OnEndInteract()
    /// 4. Player looks away → OnAbortInteract()
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// The text shown to the player when looking at this object.
        /// Example: "Read Sign", "Open Door", "Pull Lever"
        /// This property allows the Interactor to display context-specific prompts.
        /// </summary>
        string InteractionPrompt { get; }

        /// <summary>
        /// Called when the player presses the interact button while looking at this object.
        /// The Interactor reference allows the object to send data back (e.g., text messages).
        /// </summary>
        /// <param name="interactor">Reference to the Interactor that initiated the interaction.</param>
        void OnInteract(Interactor interactor);

        /// <summary>
        /// Called when an active interaction ends, either by player input or forced cancellation.
        /// Use this to clean up any interaction state (e.g., close UI, stop animations).
        /// </summary>
        void OnEndInteract();

        /// <summary>
        /// Called when the player's gaze enters this object (ready to interact).
        /// Use this to show visual feedback indicating the object can be interacted with.
        /// </summary>
        void OnReadyInteract();

        /// <summary>
        /// Called when the player's gaze leaves this object before interacting.
        /// Use this to hide any visual feedback shown in OnReadyInteract().
        /// </summary>
        void OnAbortInteract();
    }
}
