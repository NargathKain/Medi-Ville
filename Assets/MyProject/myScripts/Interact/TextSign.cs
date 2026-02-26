using UnityEngine;

namespace MyProject.Interact
{
    /// Μια αλληλεπιδραστική πινακίδα που εμφανίζει μήνυμα κειμένου όταν ο παίκτης αλληλεπιδρά.
    /// Χρησιμοποιήστε το για πίνακες πληροφοριών, ειδοποιήσεις, αντικείμενα lore, ή οποιοδήποτε αναγνώσιμο αντικείμενο.
    ///
    /// Ρύθμιση:
    /// 1. Προσθέστε αυτό το script σε ένα πινακίδα/πίνακα GameObject
    /// 2. Προσθέστε ένα Collider component (για ανίχνευση raycast)
    /// 3. Βάλτε tag στο GameObject ως "Interactable"
    /// 4. Αναθέστε ένα indicator GameObject (προαιρετικό highlight/outline)
    /// 5. Εισάγετε το κείμενο προς εμφάνιση στο πεδίο text
    public class TextSign : MonoBehaviour, IInteractable
    {

        [Header("Visual Feedback")]

        /// Προαιρετικό indicator GameObject που εμφανίζεται όταν ο παίκτης κοιτάζει την πινακίδα.
        /// Μπορεί να είναι ένα highlight effect, outline, ή floating icon.
        [Tooltip("Οπτική ένδειξη που εμφανίζεται όταν ο παίκτης μπορεί να αλληλεπιδράσει (π.χ. highlight mesh).")]
        [SerializeField]
        private GameObject indicator;

        [Header("Sign Content")]

        /// Το μήνυμα κειμένου που εμφανίζεται όταν ο παίκτης διαβάζει την πινακίδα.
        /// Χρησιμοποιούμε TextArea attribute για επεξεργασία πολλαπλών γραμμών στον Inspector.
        [Tooltip("Κείμενο που εμφανίζεται όταν ο παίκτης αλληλεπιδρά με την πινακίδα.")]
        [TextArea(3, 10)]
        [SerializeField]
        private string text = "Το κείμενο της πινακίδας...";

        /// Προσαρμοσμένο κείμενο prompt που εμφανίζεται στον παίκτη (π.χ. "Διάβασε Πινακίδα", "Εξέτασε Ειδοποίηση").
        [Tooltip("Κείμενο που εμφανίζεται στο interaction prompt (π.χ. 'Διάβασε').")]
        [SerializeField]
        private string promptText = "Read";

        /// Το κείμενο που εμφανίζεται στο "Press E to..." prompt.
        /// Επιστρέφει το προσαρμοσμένο prompt text που ορίστηκε στον Inspector.
        public string InteractionPrompt => promptText;

        /// Καλείται όταν ο παίκτης πατήσει το κουμπί αλληλεπίδρασης ενώ κοιτάζει την πινακίδα.
        /// Στέλνει το κείμενο της πινακίδας στον Interactor για εμφάνιση στην οθόνη.
        /// <param name="interactor">Ο Interactor που ξεκίνησε την αλληλεπίδραση.</param>
        public void OnInteract(Interactor interactor)
        {
            // Κρύψε την ένδειξη κατά την ανάγνωση
            if (indicator != null)
            {
                indicator.SetActive(false);
            }

            // Στείλε το κείμενο στον Interactor, ο οποίος θα το εμφανίσει μέσω InteractorUI
            interactor.ReceiveInteract(text);
        }

        /// Καλείται όταν τελειώνει η αλληλεπίδραση (ο παίκτης απομακρύνεται ή ακυρώνει).
        /// Οι πινακίδες δεν χρειάζονται ειδικό καθαρισμό, αλλά η μέθοδος πρέπει να υλοποιηθεί.
        public void OnEndInteract()
        {
            // Δεν χρειάζεται ειδικός καθαρισμός για πινακίδες
        }

        /// Καλείται όταν ο παίκτης σταματήσει να κοιτάζει την πινακίδα πριν την αλληλεπίδραση.
        /// Κρύβει την οπτική ένδειξη.
        public void OnAbortInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(false);
            }
        }

        /// Καλείται όταν ο παίκτης αρχίσει να κοιτάζει την πινακίδα.
        /// Εμφανίζει την οπτική ένδειξη για να σηματοδοτήσει ότι είναι δυνατή η αλληλεπίδραση.
        public void OnReadyInteract()
        {
            if (indicator != null)
            {
                indicator.SetActive(true);
            }
        }
    }
}
