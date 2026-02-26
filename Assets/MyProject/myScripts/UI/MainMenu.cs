using UnityEngine;
using UnityEngine.SceneManagement;

namespace MyProject.UI
{
    /// main menu controller με start gamne και exit game.
    public class MainMenu : MonoBehaviour
    {
        /// φόρτωση σκηνής στο onclick του start button.
        public void StartGame()
        {
            SceneManager.LoadScene("GameScene1");
        }

        /// έξοδος εφαρμογής στο onclick του exit button.
        public void ExitGame()
        {
            Debug.Log("[MainMenu] Exiting game...");

            // Quit the application
            Application.Quit();

            // Stop play mode in editor (for testing)
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
