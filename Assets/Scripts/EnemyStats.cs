using UnityEngine; // Βασικές κλάσεις Unity

public class EnemyStats : MonoBehaviour // Δηλώνουμε κλάση EnemyStats που κληρονομεί από MonoBehaviour
{
    [Header("Ζωή Εχθρού")]
    public int maxHealth = 50; // Μέγιστη ζωή του εχθρού
    public int currentHealth; // Τρέχουσα ζωή του εχθρού

    [Header("Επίθεση σε Παίκτη")]
    public int damageToPlayer = 10; // Πόση ζημιά κάνει ο εχθρός στον παίκτη κάθε φορά που επιτίθεται
    public float attackRange = 2f; // Από ποια απόσταση και μέσα θα μπορεί να χτυπήσει τον παίκτη
    public float attackCooldown = 1.5f; // Κάθε πόσα δευτερόλεπτα μπορεί να ξαναχτυπήσει
    private float attackTimer = 0f; // Μετρητής χρόνου για το cooldown της επίθεσης

    [Header("XP / Ενέργεια προς παίκτη όταν πεθάνει")]
    public int xpReward = 20; // Πόση εμπειρία (XP) παίρνει ο παίκτης όταν σκοτώσει αυτόν τον εχθρό
    public float energyPercentOnKill = 0.1f; // Ποσοστό (0.1 = 10%) της max ενέργειας που δίνεται στον παίκτη όταν πεθάνει ο εχθρός

    [Header("Boss Ρυθμίσεις")] // Επικεφαλίδα για το Inspector
    public bool isBoss = false; // Αν αυτός ο εχθρός είναι Boss (Αρχηγός), το κάνουμε true


    private Transform playerTransform; // Θα κρατάει τη θέση του παίκτη
    private PlayerStats playerStats; // Θα κρατάει αναφορά στο PlayerStats του παίκτη

    void Start() // Καλείται όταν ξεκινήσει η σκηνή
    {
        currentHealth = maxHealth; // Στην αρχή ο εχθρός έχει full ζωή

        // Βρίσκουμε τον παίκτη με βάση το Tag "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Βρίσκουμε το GameObject του παίκτη

        if (playerObject != null) // Αν βρέθηκε κάτι
        {
            playerTransform = playerObject.transform; // Παίρνουμε το Transform του παίκτη
            playerStats = playerObject.GetComponent<PlayerStats>(); // Παίρνουμε το PlayerStats από τον παίκτη
        }
        else
        {
            Debug.LogWarning("EnemyStats: Δεν βρέθηκε αντικείμενο με Tag 'Player' στη σκηνή!"); // Προειδοποίηση
        }
    }

    void Update() // Καλείται κάθε frame
    {
        if (playerTransform == null) // Αν δεν έχουμε παίκτη
        {
            return; // Δεν κάνουμε τίποτα
        }

        attackTimer -= Time.deltaTime; // Μειώνουμε τον μετρητή χρόνου επίθεσης

        // Υπολογίζουμε την απόσταση εχθρού - παίκτη
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position); // Απόσταση 3D

        // Αν ο παίκτης είναι μέσα στην απόσταση attackRange ΚΑΙ έχει τελειώσει το cooldown
        if (distanceToPlayer <= attackRange && attackTimer <= 0f)
        {
            AttackPlayer(); // Καλούμε συνάρτηση για να χτυπήσουμε τον παίκτη
        }
    }

    public void TakeDamage(int amount) // Δημόσια συνάρτηση για όταν ο εχθρός δέχεται ζημιά από τον παίκτη
    {
        currentHealth -= amount; // Μειώνουμε τη ζωή του εχθρού

        if (currentHealth <= 0) // Αν έπεσε στο 0 ή κάτω
        {
            Die(); // Καλούμε τη συνάρτηση θανάτου
        }
        else
        {
            Debug.Log("Ο εχθρός δέχτηκε ζημιά, τρέχουσα ζωή: " + currentHealth); // Μήνυμα στο Console
            // Αργότερα μπορούμε να βάλουμε εδώ animation "Hit"
        }
    }

    private void AttackPlayer() // Ιδιωτική συνάρτηση για επίθεση στον παίκτη
    {
        if (playerStats != null) // Αν έχουμε αναφορά στο PlayerStats
        {
            playerStats.TakeDamage(damageToPlayer); // Μειώνουμε τη ζωή του παίκτη
            Debug.Log("Ο εχθρός χτύπησε τον παίκτη για " + damageToPlayer + " ζημιά."); // Μήνυμα για έλεγχο
        }

        attackTimer = attackCooldown; // Ξαναβάζουμε τον μετρητή επίθεσης στο cooldown
    }

    private void Die() // Ιδιωτική συνάρτηση για όταν ο εχθρός πεθάνει
    {
        Debug.Log("Ο εχθρός πέθανε."); // Γράφουμε μήνυμα στο Console

        if (playerStats != null) // Αν έχουμε αναφορά στο PlayerStats
        {
            playerStats.AddXP(xpReward); // Δίνουμε XP στον παίκτη
            playerStats.RestoreEnergyPercent(energyPercentOnKill); // Δίνουμε ποσοστό ενέργειας στον παίκτη
        }

        if (QuestManager.instance != null) // Αν υπάρχει QuestManager στη σκηνή
        {
            if (isBoss) // Αν αυτός ο εχθρός είναι Boss
            {
                QuestManager.instance.BossKilled(); // Ενημερώνουμε τον QuestManager ότι ο Boss πέθανε
            }
            else // Αλλιώς είναι απλός εχθρός
            {
                QuestManager.instance.EnemyKilled(); // Ενημερώνουμε ότι πέθανε ένας κανονικός εχθρός
            }
        }

        Destroy(gameObject); // Καταστρέφουμε το GameObject του εχθρού από τη σκηνή
    }


} // Τέλος κλάσης EnemyStats
