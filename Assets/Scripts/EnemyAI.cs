using UnityEngine; // Περιέχει τις βασικές κλάσεις της Unity

public class EnemyAI : MonoBehaviour // Δηλώνουμε μια κλάση EnemyAI που κληρονομεί από MonoBehaviour
{
    public float moveSpeed = 2f; // Πόσο γρήγορα θα κινείται ο εχθρός προς τον παίκτη
    public float stopDistance = 2f; // Από ποια απόσταση και πιο κοντά ο εχθρός σταματάει να προχωράει (στάση επίθεσης)

    private Transform playerTransform; // Εσωτερική μεταβλητή: θα κρατάει τη θέση (Transform) του παίκτη

    void Start() // Καλείται μία φορά όταν ξεκινήσει η σκηνή
    {
        // Βρίσκουμε τον παίκτη στη σκηνή με βάση το Tag "Player"
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player"); // Βρίσκουμε GameObject με tag "Player"

        if (playerObject != null) // Αν βρήκαμε GameObject με tag Player
        {
            playerTransform = playerObject.transform; // Αποθηκεύουμε το Transform του παίκτη
        }
        else // Αν δεν βρήκαμε παίκτη
        {
            Debug.LogWarning("EnemyAI: Δεν βρέθηκε αντικείμενο με Tag 'Player' στη σκηνή!"); // Προειδοποιητικό μήνυμα
        }
    }

    void Update() // Καλείται κάθε frame
    {
        if (playerTransform == null) // Αν δεν έχουμε Transform παίκτη (δεν βρέθηκε στην Start)
        {
            return; // Δεν κάνουμε τίποτα
        }

        // Υπολογίζουμε την απόσταση ανάμεσα στον εχθρό και στον παίκτη
        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position); // Απόσταση δύο σημείων στο χώρο

        if (distanceToPlayer > stopDistance) // Αν η απόσταση είναι ΜΕΓΑΛΥΤΕΡΗ από το stopDistance
        {
            // Θέλουμε να κουνηθούμε προς τον παίκτη

            // Πρώτα, στρίβουμε τον εχθρό να κοιτάει προς τον παίκτη
            Vector3 direction = (playerTransform.position - transform.position).normalized; // Κατεύθυνση προς τον παίκτη (μονάδα)
            direction.y = 0f; // Μηδενίζουμε το Y για να μην κοιτάει προς τα πάνω/κάτω, μόνο στο οριζόντιο επίπεδο

            if (direction != Vector3.zero) // Αν η κατεύθυνση δεν είναι μηδενικό διάνυσμα
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction); // Υπολογίζουμε την περιστροφή που κοιτάει προς την κατεύθυνση
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f); // Ομαλή περιστροφή προς τον στόχο
            }

            // Μετά κουνάμε τον εχθρό προς τον παίκτη
            Vector3 move = direction * moveSpeed * Time.deltaTime; // Υπολογίζουμε το βήμα κίνησης
            transform.position += move; // Προσθέτουμε το βήμα στη θέση του εχθρού
        }
        else
        {
            // Αν είμαστε αρκετά κοντά (distanceToPlayer <= stopDistance), ο εχθρός σταματάει κίνηση
            // Εδώ αργότερα θα βάλουμε λογική "επίθεσης" στον παίκτη
        }
    }
} // Τέλος κλάσης EnemyAI
