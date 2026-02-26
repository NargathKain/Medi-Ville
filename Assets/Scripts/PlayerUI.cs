using UnityEngine; // Χρησιμοποιούμε το namespace UnityEngine για τις βασικές λειτουργίες της Unity
using UnityEngine.UI; // Χρησιμοποιούμε το namespace UnityEngine.UI για να έχουμε πρόσβαση σε UI στοιχεία όπως Slider
using TMPro; // Χρησιμοποιούμε το namespace TMPro για να δουλέψουμε με TextMeshPro στοιχεία κειμένου

public class PlayerUI : MonoBehaviour // Δηλώνουμε την δημόσια κλάση PlayerUI που κληρονομεί από το MonoBehaviour
{
    public PlayerStats playerStats; // Δημόσια μεταβλητή αναφοράς προς το script PlayerStats του παίκτη
    public Slider healthSlider; // Δημόσια μεταβλητή αναφοράς προς το UI Slider που δείχνει την ζωή του παίκτη
    public TextMeshProUGUI levelText; // Δημόσια μεταβλητή αναφοράς προς το TextMeshProUGUI που θα δείχνει το level του παίκτη
    public TextMeshProUGUI xpText; // Δημόσια μεταβλητή αναφοράς προς το TextMeshProUGUI που θα δείχνει την εμπειρία (XP) του παίκτη

    void Start() // Συνάρτηση που καλείται μία φορά στην αρχή όταν ξεκινά το παιχνίδι
    {
        healthSlider.maxValue = playerStats.maxHealth; // Ορίζουμε την μέγιστη τιμή του slider ίση με την μέγιστη ζωή του παίκτη
        healthSlider.value = playerStats.currentHealth; // Ορίζουμε την αρχική τιμή του slider ίση με την τρέχουσα ζωή του παίκτη
    }

    void Update() // Συνάρτηση που καλείται κάθε frame του παιχνιδιού
    {
        healthSlider.value = playerStats.currentHealth; // Ενημερώνουμε κάθε frame την τιμή του slider ώστε να ακολουθεί την τρέχουσα ζωή του παίκτη

        levelText.text = "Level: " + playerStats.level; // Ενημερώνουμε το κείμενο levelText για να δείχνει το τρέχον level του παίκτη
        xpText.text = "XP: " + playerStats.currentXP + " / " + playerStats.xpToNextLevel; // Ενημερώνουμε το κείμενο xpText για να δείχνει την τρέχουσα εμπειρία και την απαιτούμενη για το επόμενο level
    }
} // Τέλος της κλάσης PlayerUI
