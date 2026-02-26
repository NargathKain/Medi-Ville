using UnityEngine; // Βασικές κλάσεις Unity
using TMPro; // Για TextMeshPro UI (QuestText, QuestCompleteText)
using UnityEngine.UI; // Αν χρησιμοποιείς και άλλα UI στοιχεία (π.χ. Panel)
using System.Collections; // Για να μπορούμε να χρησιμοποιήσουμε IEnumerator / Coroutines


// Ο QuestManager διαχειρίζεται 2 Quest:
// Quest 1: Σκότωσε Χ απλούς εχθρούς
// Quest 2: Σκότωσε τον Αρχηγό (Boss)
public class QuestManager : MonoBehaviour // Δηλώνουμε μια δημόσια κλάση QuestManager που κληρονομεί από MonoBehaviour
{
    public static QuestManager instance; // Singleton: θα κρατάει τη μοναδική instance του QuestManager

    [Header("Quest 1 - Σκότωσε εχθρούς")]
    public int quest1TotalEnemies = 3; // Πόσους απλούς εχθρούς πρέπει να σκοτώσει ο παίκτης στο πρώτο Quest
    private int quest1CurrentKills = 0; // Πόσους απλούς εχθρούς έχει σκοτώσει μέχρι τώρα
    private bool quest1Completed = false; // Αν το Quest 1 έχει ολοκληρωθεί

    [Header("Quest 2 - Σκότωσε τον Αρχηγό (Boss)")]
    public GameObject bossObject; // Αναφορά στο GameObject του Boss στη σκηνή
    private bool quest2Completed = false; // Αν το Quest 2 έχει ολοκληρωθεί

    // Τρέχον Quest (1 ή 2)
    private int currentQuestIndex = 1; // Ξεκινάμε από το Quest 1

    [Header("UI (μέσα στο Canvas)")]
    public TextMeshProUGUI questText; // Κείμενο στην οθόνη που δείχνει την περιγραφή / πρόοδο του Quest
    public GameObject questCompletePanel; // Panel που εμφανίζεται όταν ολοκληρωθεί ένα Quest
    public TextMeshProUGUI questCompleteText; // Κείμενο μέσα στο questCompletePanel

    void Awake() // Καλείται πολύ νωρίς, όταν "ξυπνάει" το GameObject
    {
        // Singleton pattern: επιτρέπουμε μόνο ένα QuestManager στη σκηνή
        if (instance == null) // Αν δεν υπάρχει ήδη κάποιο instance
        {
            instance = this; // Αυτή η instance γίνεται η global QuestManager.instance
        }
        else if (instance != this) // Αν υπάρχει ήδη κάποιο άλλο instance
        {
            Destroy(gameObject); // Καταστρέφουμε αυτό το GameObject για να μην υπάρχουν διπλά
        }
    }

    void Start() // Καλείται όταν ξεκινήσει η σκηνή
    {
        // Ρύθμιση αρχικών τιμών για τα Quest
        currentQuestIndex = 1; // Ξεκινάμε με το Quest 1
        quest1CurrentKills = 0; // Κανένας εχθρός δεν είναι σκοτωμένος στην αρχή
        quest1Completed = false; // Το Quest 1 δεν έχει ολοκληρωθεί
        quest2Completed = false; // Το Quest 2 επίσης δεν έχει ολοκληρωθεί

        // Αν δεν θέλεις να φαίνεται το panel στην αρχή, το κρύβουμε
        if (questCompletePanel != null)
        {
            questCompletePanel.SetActive(false); // Κρύβουμε το panel ολοκλήρωσης quest
        }

        // Αν θες ο Boss να είναι κρυφός μέχρι το Quest 2, μπορείς να το ξεκλειδώσεις αργότερα
        // Προς το παρόν, τον αφήνουμε όπως είναι στο Scene (active ή inactive)
        // Αν θες να τον κρύβεις:
        // if (bossObject != null)
        // {
        //     bossObject.SetActive(false);
        // }

        ShowQuest1Text(); // Εμφανίζουμε στην οθόνη το κείμενο για το Quest 1
    }

    // ------------------------- QUEST 1 -------------------------

    private void ShowQuest1Text() // Γράφει το αρχικό κείμενο του Quest 1
    {
        if (questText != null) // Αν έχουμε αναφορά στο UI Text
        {
            // Π.χ. "Quest 1: Σκότωσε 0 / 3 εχθρούς"
            questText.text = "Quest 1: Σκότωσε " + quest1CurrentKills + " / " + quest1TotalEnemies + " εχθρούς";
        }
    }

    private void UpdateQuest1Text() // Ενημερώνει την πρόοδο του Quest 1
    {
        if (questText != null)
        {
            questText.text = "Quest 1: Σκότωσε " + quest1CurrentKills + " / " + quest1TotalEnemies + " εχθρούς";
        }
    }

    // Αυτή η συνάρτηση καλείται από τους απλούς εχθρούς όταν πεθαίνουν (EnemyStats → Die)
    public void EnemyKilled()
    {
        // Αν ΔΕΝ είμαστε στο Quest 1, αγνοούμε τα kills απλών εχθρών
        if (currentQuestIndex != 1)
        {
            return;
        }

        // Αν το Quest 1 έχει ήδη ολοκληρωθεί, δεν συνεχίζουμε να μετράμε
        if (quest1Completed)
        {
            return;
        }

        // Αυξάνουμε τον αριθμό των σκοτωμένων εχθρών
        quest1CurrentKills++;

        // Αν για κάποιο λόγο ξεπεράσαμε το όριο, το "κόβουμε" στο μέγιστο
        if (quest1CurrentKills > quest1TotalEnemies)
        {
            quest1CurrentKills = quest1TotalEnemies;
        }

        // Ενημερώνουμε το UI με την πρόοδο
        UpdateQuest1Text();

        // Αν φτάσαμε στο στόχο, ολοκληρώνουμε το Quest 1
        if (quest1CurrentKills >= quest1TotalEnemies)
        {
            CompleteQuest1();
        }
    }

    private void CompleteQuest1() // Λογική όταν ολοκληρωθεί το Quest 1
    {
        quest1Completed = true; // Σημειώνουμε ότι το Quest 1 τελείωσε

        // Εμφανίζουμε ένα μήνυμα ότι το Quest 1 ολοκληρώθηκε
        if (questCompletePanel != null)
        {
            questCompletePanel.SetActive(true); // Δείχνουμε το Panel
        }

        if (questCompleteText != null)
        {
            questCompleteText.text = "Quest 1 Completed!\nΣκότωσες όλους τους εχθρούς."; // Μήνυμα μέσα στο Panel
        }

        // Ξεκινάμε μια Coroutine που θα περιμένει 2 δευτερόλεπτα
        // και μετά θα ξεκινήσει το Quest 2
        StartCoroutine(StartQuest2WithDelay(4f)); // 4f = 4 δευτερόλεπτα
    }

    private IEnumerator StartQuest2WithDelay(float delaySeconds) // Coroutine που περιμένει κάποια δευτερόλεπτα πριν ξεκινήσει το Quest 2
    {
        // Περιμένουμε delaySeconds δευτερόλεπτα (π.χ. 4 δευτερόλεπτα)
        yield return new WaitForSeconds(delaySeconds);

        // Κρύβουμε το panel "Quest 1 Completed"
        if (questCompletePanel != null)
        {
            questCompletePanel.SetActive(false);
        }

        // Και ξεκινάμε το Quest 2
        StartQuest2();
    }



    // ------------------------- QUEST 2 -------------------------

    private void StartQuest2() // Ενεργοποιεί το Quest 2
    {
        currentQuestIndex = 2; // Τώρα το ενεργό quest είναι το Quest 2

        

        // Αλλάζουμε το κείμενο του Quest στην οθόνη
        if (questText != null)
        {
            questText.text = "Quest 2: Σκότωσε τον Αρχηγό (Boss)!";
        }

        // Αν θέλουμε ο Boss να εμφανίζεται ΜΟΝΟ τώρα, τον ενεργοποιούμε εδώ:
        // if (bossObject != null)
        // {
        //     bossObject.SetActive(true);
        // }
    }

    // Αυτή η συνάρτηση καλείται από τον Boss (EnemyStats → Die) όταν ο Boss πεθάνει
    public void BossKilled()
    {
        // Αν δεν είμαστε στο Quest 2, αγνοούμε τον θάνατο του Boss
        if (currentQuestIndex != 2)
        {
            return;
        }

        // Αν το Quest 2 έχει ήδη ολοκληρωθεί, δεν κάνουμε τίποτα
        if (quest2Completed)
        {
            return;
        }

        quest2Completed = true; // Σημειώνουμε ότι το Quest 2 τελείωσε

        // Αλλάζουμε κείμενο Quest
        if (questText != null)
        {
            questText.text = "Quest 2: Ο Αρχηγός νικήθηκε!";
        }

        // Εμφανίζουμε τελικό μήνυμα για όλα τα Quest
        if (questCompletePanel != null)
        {
            questCompletePanel.SetActive(true);
        }

        if (questCompleteText != null)
        {
            questCompleteText.text = "Όλα τα Quest ολοκληρώθηκαν!\nΜπράβο!";
        }

        // Εδώ αργότερα μπορείς να βάλεις:
        // - Victory Screen
        // - Κουμπί "Return to Main Menu"
        // - Κουμπί "Restart" κτλ.
    }
} // Τέλος κλάσης QuestManager
