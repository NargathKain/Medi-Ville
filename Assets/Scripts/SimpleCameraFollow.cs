using UnityEngine; // Περιέχει τις βασικές κλάσεις της Unity

public class SimpleCameraFollow : MonoBehaviour // Δηλώνουμε μία κλάση SimpleCameraFollow που κληρονομεί από το MonoBehaviour
{
    public Transform target; // Αυτός είναι ο στόχος που θα ακολουθεί η κάμερα (ο παίκτης)
    public Vector3 offset = new Vector3(0f, 1.6f, -3f); // Μετατόπιση της κάμερας σε σχέση με τον στόχο (λίγο πάνω και πίσω του)

    public float followSpeed = 10f; // Πόσο γρήγορα θα “κολλάει” η κάμερα στην επιθυμητή θέση

    void LateUpdate() // Η LateUpdate καλείται κάθε frame, αλλά μετά από τα Update των άλλων αντικειμένων (ιδανικό για κάμερα)
    {
        if (target == null) // Αν δεν έχουμε ορίσει target στο Inspector
        {
            return; // Δεν κάνουμε τίποτα
        }

        // Υπολογίζουμε την επιθυμητή θέση της κάμερας:
        // Παίρνουμε τη θέση του στόχου (target.position) και προσθέτουμε το offset (λίγο πάνω και πίσω)
        Vector3 desiredPosition = target.position + offset;

        // Μετακινούμε ΟΜΑΛΑ την κάμερα από την τωρινή θέση προς την επιθυμητή θέση
        // Χρησιμοποιούμε Lerp για ομαλή κίνηση
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

        // Κάνουμε την κάμερα να κοιτάει πάντα τον στόχο (τον παίκτη)
        transform.LookAt(target.position);
    }
} // Τέλος της κλάσης SimpleCameraFollow
