using UnityEngine; // Βασικές κλάσεις Unity

public class PlayerAttack : MonoBehaviour // Δηλώνουμε μία δημόσια κλάση PlayerAttack που κληρονομεί από MonoBehaviour
{
    [Header("Ρυθμίσεις Επίθεσης")]
    public int baseDamage = 25; // Πόση ζημιά κάνει μία βασική επίθεση
    public float attackRange = 2f; // Απόσταση εμβέλειας της επίθεσης (σε μονάδες Unity)
    public float attackCooldown = 0.5f; // Χρόνος (σε δευτερόλεπτα) που πρέπει να περάσει ανάμεσα σε δύο επιθέσεις

    private float attackTimer = 0f; // Εσωτερικός μετρητής χρόνου, για να ελέγχουμε το cooldown
    private PlayerStats playerStats; // Αναφορά στο PlayerStats, για να μπορούμε αργότερα να χρησιμοποιούμε ενέργεια αν θέλουμε

    void Start() // Καλείται μία φορά όταν ξεκινάει η σκηνή
    {
        playerStats = GetComponent<PlayerStats>(); // Παίρνουμε το component PlayerStats από τον ίδιο GameObject (Player)
    }

    void Update() // Καλείται κάθε frame
    {
        attackTimer -= Time.deltaTime; // Μειώνουμε τον attackTimer με βάση τον χρόνο που πέρασε από το προηγούμενο frame

        // Αν ο παίκτης πατήσει αριστερό κλικ (mouse button 0) ΚΑΙ έχει περάσει το cooldown
        if (Input.GetMouseButtonDown(0) && attackTimer <= 0f)
        {
            PerformAttack(); // Καλούμε τη συνάρτηση PerformAttack για να εκτελέσουμε την επίθεση
        }
    }

    private void PerformAttack() // Ιδιωτική συνάρτηση που εκτελεί την λογική της επίθεσης
    {
        Debug.Log("Ο παίκτης έκανε επίθεση!"); // Γράφουμε ένα μήνυμα στο Console για έλεγχο

        // Εδώ αργότερα θα βάλουμε:
        // - animation επίθεσης
        // - έλεγχο ενέργειας (SpendEnergy)
        // Προς το παρόν, απλά ελέγχουμε για εχθρούς μπροστά από τον παίκτη

        // Υπολογίζουμε το κέντρο μιας νοητής σφαίρας μπροστά από τον παίκτη
        Vector3 sphereCenter = transform.position + transform.forward * (attackRange / 2f); // Μπροστά από τον παίκτη, στη μέση της εμβέλειας
        float sphereRadius = attackRange; // Η ακτίνα της σφαίρας = η εμβέλεια επίθεσης

        // Βρίσκουμε όλα τα Collider μέσα σε αυτή τη σφαίρα
        Collider[] hitColliders = Physics.OverlapSphere(sphereCenter, sphereRadius); // Η OverlapSphere επιστρέφει πίνακα από colliders

        // Διατρέχουμε όλα τα colliders που βρήκαμε
        foreach (Collider hit in hitColliders)
        {
            // Ελέγχουμε αν το αντικείμενο έχει tag "Enemy"
            if (hit.CompareTag("Enemy")) // Αν το αντικείμενο έχει Tag "Enemy"
            {
                EnemyStats enemyStats = hit.GetComponent<EnemyStats>(); // Προσπαθούμε να πάρουμε το component EnemyStats από αυτό το αντικείμενο

                if (enemyStats != null) // Αν βρήκαμε EnemyStats
                {
                    enemyStats.TakeDamage(baseDamage); // Καλούμε τη συνάρτηση TakeDamage του εχθρού με ζημιά = baseDamage
                    Debug.Log("Ο παίκτης έκανε " + baseDamage + " ζημιά σε εχθρό."); // Μήνυμα για έλεγχο
                }
            }


        }

        // Ξαναβάζουμε τον μετρητή cooldown στην αρχική τιμή
        attackTimer = attackCooldown; // Πρέπει να περάσουν attackCooldown δευτερόλεπτα πριν την επόμενη επίθεση
    }

    void OnDrawGizmosSelected() // Αυτή η συνάρτηση σχεδιάζει βοηθητικά “Gizmos” στο Scene view, όταν έχουμε επιλεγμένο τον παίκτη
    {
        // Βάζουμε χρώμα κόκκινο για την σφαίρα επίθεσης
        Gizmos.color = Color.red;

        // Υπολογίζουμε το σημείο κέντρου της σφαίρας μας μπροστά από τον παίκτη
        Vector3 sphereCenter = transform.position + transform.forward * (attackRange / 2f);

        // Ζωγραφίζουμε μία “wireframe” σφαίρα (μόνο περίγραμμα)
        Gizmos.DrawWireSphere(sphereCenter, attackRange);
    }
} // Τέλος της κλάσης PlayerAttack
