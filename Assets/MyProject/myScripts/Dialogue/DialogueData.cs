using UnityEngine;

namespace MyProject.Dialogue
{
    /// ScriptableObject που περιέχει δεδομένα διαλόγου για NPC.
    /// Δημιουργία με Assets > Create > MyProject > Dialogue Data.
    /// 1. Right-click Project window
    /// 2. Create > MyProject > Dialogue Data
    /// 3. Fill in NPC name and dialogue lines
    /// 4. Assign to an NPC's dialogue component
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "MyProject/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        /// Όνομα του NPC που μιλάει - Εμφανίζεται πάνω από το κείμενο διαλόγου
        [Tooltip("Name displayed for this NPC.")]
        public string npcName = "NPC";

        /// Array με διάλογο που θα πει το NPC
        /// Προχωράει με το E από τον παίκτη
        [Tooltip("Dialogue lines in order. Player presses E to advance.")]
        [TextArea(2, 5)]
        public string[] dialogueLines;

        /// προαιρετικό: αυτόματη μετάβαση στην επόμενη γραμμή μετά από αυτό το χρόνο (σε δευτερόλεπτα)
        /// Χρήσιμο για proximity NPC που μιλάνε αυτόματα 
        [Tooltip("Auto-advance time in seconds. 0 = manual advance only.")]
        public float autoAdvanceTime = 0f;
    }
}
