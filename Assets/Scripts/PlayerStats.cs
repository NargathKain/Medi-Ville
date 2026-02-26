using UnityEngine; // Περιέχει τις βασικές κλάσεις της Unity
using UnityEngine.UI; // Χρειαζόμαστε αυτό για να χρησιμοποιήσουμε UI Slider (μπάρες ζωής / ενέργειας)

public class PlayerStats : MonoBehaviour // Δηλώνουμε μια δημόσια κλάση PlayerStats που κληρονομεί από MonoBehaviour
{
    [Header("Ζωή (Health)")] // Αυτό το Header είναι απλά για οργάνωση στο Inspector
    public int maxHealth = 100; // Δημόσια μεταβλητή: η μέγιστη ζωή (HP) του παίκτη
    public int currentHealth; // Δημόσια μεταβλητή: η τρέχουσα ζωή του παίκτη

    public Slider healthSlider; // Δημόσια μεταβλητή: αναφορά στο UI Slider που θα δείχνει τη ζωή

    [Header("Ενέργεια (Energy)")]
    public int maxEnergy = 100; // Μέγιστη ενέργεια (π.χ. stamina για combo attacks)
    public int currentEnergy; // Τρέχουσα ενέργεια

    public Slider energySlider; // Αναφορά στο UI Slider που θα δείχνει την ενέργεια

    [Header("Level / XP (προαιρετικό για αργότερα)")]
    public int level = 1; // Τρέχον level του παίκτη
    public int currentXP = 0; // Τρέχουσα εμπειρία (XP)
    public int xpToNextLevel = 100; // Πόση XP χρειάζεται για το επόμενο level

    void Start() // Η Start καλείται αυτόματα όταν ξεκινάει η σκηνή
    {
        // Στην αρχή του παιχνιδιού ο παίκτης έχει full ζωή
        currentHealth = maxHealth; // Η τρέχουσα ζωή γίνεται ίση με τη μέγιστη ζωή

        // Στην αρχή του παιχνιδιού ο παίκτης έχει full ενέργεια
        currentEnergy = maxEnergy; // Η τρέχουσα ενέργεια γίνεται ίση με τη μέγιστη ενέργεια

        // Ενημερώνουμε τις μπάρες UI για να δείχνουν τις αρχικές τιμές
        UpdateHealthUI(); // Καλούμε τη συνάρτηση που ενημερώνει τη μπάρα ζωής
        UpdateEnergyUI(); // Καλούμε τη συνάρτηση που ενημερώνει τη μπάρα ενέργειας
    }

    // ------------------ ΖΩΗ (HEALTH) ------------------

    public void TakeDamage(int amount) // Δημόσια συνάρτηση: μειώνει τη ζωή του παίκτη κατά ένα ποσό
    {
        currentHealth -= amount; // Αφαιρούμε ποσό "amount" από τη ζωή

        if (currentHealth < 0) // Αν η ζωή πέσει κάτω από το 0
        {
            currentHealth = 0; // Την κρατάμε στο 0 (δεν πάμε σε αρνητικά)
        }

        UpdateHealthUI(); // Ενημερώνουμε τη μπάρα ζωής

        if (currentHealth == 0) // Αν η ζωή είναι 0
        {
            Debug.Log("Ο παίκτης πέθανε (απλή εκτύπωση – αργότερα θα βάλουμε Game Over)."); // Προς το παρόν μόνο μήνυμα
            // Εδώ αργότερα θα καλέσουμε animation θανάτου, Game Over UI κτλ.
        }
    }

    private void UpdateHealthUI() // Ιδιωτική συνάρτηση: ενημερώνει το Health Slider
    {
        if (healthSlider != null) // Αν έχουμε ορίσει Slider στο Inspector
        {
            healthSlider.maxValue = maxHealth; // Θέτουμε την μέγιστη τιμή του Slider ίση με τη μέγιστη ζωή
            healthSlider.value = currentHealth; // Θέτουμε την τρέχουσα τιμή του Slider ίση με την τρέχουσα ζωή
        }
    }

    // ------------------ ΕΝΕΡΓΕΙΑ (ENERGY) ------------------

    public bool SpendEnergy(int amount) // Δημόσια συνάρτηση: προσπαθεί να αφαιρέσει ενέργεια. Επιστρέφει true/false.
    {
        if (currentEnergy < amount) // Αν η τρέχουσα ενέργεια είναι μικρότερη από το ποσό που θέλουμε να ξοδέψουμε
        {
            Debug.Log("Δεν υπάρχει αρκετή ενέργεια!"); // Εμφανίζουμε μήνυμα στο Console
            return false; // Επιστρέφουμε false για να ξέρει ο κώδικας ότι δεν πέτυχε
        }

        currentEnergy -= amount; // Αφαιρούμε την ενέργεια
        UpdateEnergyUI(); // Ενημερώνουμε τη μπάρα ενέργειας

        return true; // Επιστρέφουμε true = πετυχημένη αφαίρεση ενέργειας
    }

    public void RestoreEnergy(int amount) // Δημόσια συνάρτηση: προσθέτει ενέργεια (π.χ. από φίλτρο)
    {
        currentEnergy += amount; // Προσθέτουμε την ενέργεια

        if (currentEnergy > maxEnergy) // Αν ξεπεράσουμε το μέγιστο
        {
            currentEnergy = maxEnergy; // Το κόβουμε στο μέγιστο
        }

        UpdateEnergyUI(); // Ενημερώνουμε τη μπάρα ενέργειας
    }

    public void RestoreEnergyPercent(float percent) // Προσθέτει ενέργεια ποσοστιαία (π.χ. 0.1f = 10% της maxEnergy)
    {
        int amount = Mathf.RoundToInt(maxEnergy * percent); // Υπολογίζουμε πόση ενέργεια αντιστοιχεί στο ποσοστό
        RestoreEnergy(amount); // Χρησιμοποιούμε την RestoreEnergy για να την προσθέσουμε
    }

    private void UpdateEnergyUI() // Ιδιωτική συνάρτηση: ενημερώνει το Energy Slider
    {
        if (energySlider != null) // Αν έχουμε ορίσει Slider στο Inspector
        {
            energySlider.maxValue = maxEnergy; // Όριο του Slider = μέγιστη ενέργεια
            energySlider.value = currentEnergy; // Τρέχουσα τιμή του Slider = τρέχουσα ενέργεια
        }
    }

    // ------------------ LEVEL / XP (για αργότερα) ------------------

    public void AddXP(int amount) // Δημόσια συνάρτηση: προσθέτει XP στον παίκτη
    {
        currentXP += amount; // Προσθέτουμε την XP

        if (currentXP >= xpToNextLevel) // Αν η XP φτάσει ή ξεπεράσει το όριο
        {
            LevelUp(); // Καλούμε τη συνάρτηση LevelUp
        }
    }

    private void LevelUp() // Ιδιωτική συνάρτηση: ανεβάζει level τον παίκτη
    {
        level++; // Αυξάνουμε το level κατά 1
        currentXP = 0; // Μηδενίζουμε την τρέχουσα XP
        xpToNextLevel += 50; // Αυξάνουμε το όριο για το επόμενο level (γίνεται πιο δύσκολο)

        maxHealth += 20; // Αυξάνουμε τη μέγιστη ζωή του παίκτη
        currentHealth = maxHealth; // Γεμίζουμε τη ζωή στο full
        UpdateHealthUI(); // Ενημερώνουμε τη μπάρα ζωής

        maxEnergy += 10; // Αυξάνουμε τη μέγιστη ενέργεια
        currentEnergy = maxEnergy; // Γεμίζουμε την ενέργεια στο full
        UpdateEnergyUI(); // Ενημερώνουμε τη μπάρα ενέργειας

        Debug.Log("Level Up! Νέο level: " + level); // Μήνυμα στο Console για ενημέρωση
    }
} // Τέλος της κλάσης PlayerStats
