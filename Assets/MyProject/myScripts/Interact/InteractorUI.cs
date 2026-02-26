using UnityEngine;
using TMPro;

namespace MyProject.Interact
{
    /// Βοηθητικό UI για το σύστημα Interactor.
    /// Διαχειρίζεται την εμφάνιση μηνυμάτων κειμένου από αλληλεπιδραστικά αντικείμενα στην οθόνη.
    /// Προσθέστε το σε ένα Canvas ή UI manager GameObject.
    ///
    /// Ρύθμιση:
    /// 1. Δημιουργήστε ένα TextMeshProUGUI element στο Canvas σας για εμφάνιση μηνυμάτων
    /// 2. Αναθέστε το στο πεδίο messageText
    /// 3. Κάντε reference αυτό το component στο Interactor script
    public class InteractorUI : MonoBehaviour
    {
        [Header("UI References")]

        /// Το TextMeshProUGUI element που εμφανίζει τα μηνύματα αλληλεπίδρασης.
        /// Εδώ θα εμφανίζεται κείμενο από πινακίδες, βιβλία, κλπ.
        [Tooltip("TextMeshPro element για εμφάνιση μηνυμάτων αλληλεπίδρασης.")]
        [SerializeField]
        private TextMeshProUGUI messageText;

        /// Αρχικοποιεί το UI κρύβοντας το κείμενο μηνύματος.
        private void Start()
        {
            // Ξεκινάμε με το μήνυμα κρυμμένο
            HideTextMessage();

            // Προειδοποίηση αν λείπει η αναφορά
            if (messageText == null)
            {
                Debug.LogWarning("[InteractorUI] Το MessageText δεν έχει ανατεθεί. Τα μηνύματα δεν θα εμφανίζονται.");
            }
        }

        /// Εμφανίζει ένα μήνυμα κειμένου στην οθόνη.
        /// Καλείται από αλληλεπιδραστικά αντικείμενα (μέσω Interactor.ReceiveInteract) για εμφάνιση κειμένου.
        /// <param name="message">Το μήνυμα προς εμφάνιση.</param>
        public void ShowTextMessage(string message)
        {
            if (messageText == null)
            {
                Debug.LogWarning("[InteractorUI] ShowTextMessage: το messageText δεν έχει ανατεθεί.");
                return;
            }

            // Ορισμός του περιεχομένου κειμένου
            messageText.text = message;

            // Εμφάνιση του text element
            messageText.gameObject.SetActive(true);
        }

        /// Κρύβει το UI element του μηνύματος κειμένου.
        /// Καλείται όταν τελειώνει ή ακυρώνεται η αλληλεπίδραση.
        public void HideTextMessage()
        {
            if (messageText == null)
            {
                Debug.LogWarning("[InteractorUI] HideTextMessage: το messageText δεν έχει ανατεθεί.");
                return;
            }

            // Καθαρισμός περιεχομένου κειμένου για ασφάλεια
            messageText.text = "";

            // Απόκρυψη του text element
            messageText.gameObject.SetActive(false);
        }
    }
}
