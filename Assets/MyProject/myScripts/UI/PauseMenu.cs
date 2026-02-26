using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace MyProject.UI
{
    /// pause menu controller.
    /// pause - Escape key, freeze game time, show/hide UI.
    
    public class PauseMenu : MonoBehaviour
    {
        
        [Header("UI Reference")]

        /// pause menu panel show/hide.
        [Tooltip("The pause menu panel GameObject.")]
        [SerializeField]
        private GameObject pauseMenuPanel;

        ///έλεγχος αν έχει ήδη παύση 
        private bool isPaused = false;

        /// απενεργοποίηση εισόδου παίκτη για να σταματήσει η κίνηση της κάμερας
        private PlayerInput playerInput;

        private void Start()
        {
            // Find PlayerInput on the player
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerInput = player.GetComponent<PlayerInput>();
            }

            // Ensure game starts unpaused
            ResumeGame();
        }

        private void Update()
        {
            // Toggle pause with Escape key (New Input System)
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (isPaused)
                {
                    ResumeGame();
                }
                else
                {
                    PauseGame();
                }
            }
        }

        /// Παύση παιχνιδιού - εμφάνιση μενού 
        public void PauseGame()
        {
            isPaused = true;

            // Freeze game time
            Time.timeScale = 0f;

            // Disable player input (stops camera movement)
            if (playerInput != null)
            {
                playerInput.enabled = false;
            }

            // Show pause menu
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }

            // Unlock cursor for menu navigation
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        /// συνέχεια παιχνιδιού
        public void ResumeGame()
        {
            isPaused = false;

            // Restore game time
            Time.timeScale = 1f;

            // Re-enable player input
            if (playerInput != null)
            {
                playerInput.enabled = true;
            }

            // Hide pause menu
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }

            // Lock cursor for gameplay
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        /// Επιστροφή στο κύριο μενού - φόρτωση σκηνής
        public void GoToMainMenu()
        {
            // Restore time before loading (important!)
            Time.timeScale = 1f;

            // Load main menu scene
            SceneManager.LoadScene("MainMenu");
        }

        /// Έξοδος από το παιχνίδι - τερματισμός εφαρμογής
        public void ExitGame()
        {
            Debug.Log("[PauseMenu] Exiting game...");

            // Restore time before quitting
            Time.timeScale = 1f;

            Application.Quit();

            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }

        /// αλήθεια αν είναι σε παύση
        public bool IsPaused => isPaused;
    }
}
