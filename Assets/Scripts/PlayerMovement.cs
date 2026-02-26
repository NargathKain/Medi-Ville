using UnityEngine; // Χρησιμοποιούμε το namespace UnityEngine για πρόσβαση σε όλες τις βασικές κλάσεις της Unity

public class PlayerMovement : MonoBehaviour // Δηλώνουμε μια δημόσια κλάση PlayerMovement που κληρονομεί από το MonoBehaviour
{
    public float moveSpeed = 5f; // Δημόσια μεταβλητή για την ταχύτητα κίνησης του παίκτη

    public float rotationSpeed = 720f; // Δημόσια μεταβλητή για την ταχύτητα περιστροφής του παίκτη (μοίρες ανά δευτερόλεπτο)

    public float gravity = -9.81f; // Δημόσια μεταβλητή για τη βαρύτητα που θα εφαρμόζεται στον παίκτη

    private CharacterController controller; // Ιδιωτική μεταβλητή αναφοράς στον CharacterController του παίκτη

    private Vector3 velocity; // Ιδιωτικό διάνυσμα για να αποθηκεύουμε την κατακόρυφη ταχύτητα (π.χ. πτώση λόγω βαρύτητας)

    void Start() // Συνάρτηση που καλείται μία φορά στην αρχή όταν ξεκινά το παιχνίδι
    {
        controller = GetComponent<CharacterController>(); // Παίρνουμε και αποθηκεύουμε το component CharacterController από το αντικείμενο του παίκτη
    }

    void Update() // Συνάρτηση που καλείται κάθε frame του παιχνιδιού
    {
        float horizontal = Input.GetAxis("Horizontal"); // Παίρνουμε την οριζόντια είσοδο από το πληκτρολόγιο (A/D ή αριστερό/δεξί βελάκι)
        float vertical = Input.GetAxis("Vertical"); // Παίρνουμε την κατακόρυφη είσοδο από το πληκτρολόγιο (W/S ή πάνω/κάτω βελάκι)

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical); // Δημιουργούμε ένα διάνυσμα κατεύθυνσης με βάση τις οριζόντιες και κατακόρυφες εισόδους

        if (inputDirection.magnitude > 0.1f) // Αν το μέτρο του διανύσματος κατεύθυνσης είναι μεγαλύτερο από ένα μικρό κατώφλι (δηλαδή αν πραγματικά κινούμαστε)
        {
            Vector3 moveDirection = inputDirection.normalized; // Κανονικοποιούμε το διάνυσμα κατεύθυνσης για να έχει πάντα μήκος 1

            float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg; // Υπολογίζουμε τη γωνία προς την οποία πρέπει να στραφεί ο παίκτης με βάση τη διεύθυνση κίνησης

            Quaternion targetRotation = Quaternion.Euler(0f, targetAngle, 0f); // Δημιουργούμε ένα quaternion περιστροφής γύρω από τον άξονα Y με την γωνία που υπολογίσαμε

            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime); // Περιστρέφουμε ομαλά τον παίκτη προς την επιθυμητή κατεύθυνση με βάση την ταχύτητα περιστροφής

            Vector3 worldMove = transform.forward * moveSpeed; // Δημιουργούμε ένα διάνυσμα κίνησης προς τα μπροστά του παίκτη με βάση την ταχύτητα κίνησης

            controller.Move(worldMove * Time.deltaTime); // Κινούμε τον παίκτη στο χώρο χρησιμοποιώντας τον CharacterController και το διάνυσμα κίνησης στον χρόνο του frame
        }

        if (controller.isGrounded && velocity.y < 0f) // Αν ο παίκτης πατάει στο έδαφος και η κατακόρυφη ταχύτητα είναι προς τα κάτω
        {
            velocity.y = -2f; // Θέτουμε μια μικρή αρνητική τιμή στην κατακόρυφη ταχύτητα για να κρατήσουμε τον παίκτη "κολλημένο" στο έδαφος
        }

        velocity.y += gravity * Time.deltaTime; // Προσθέτουμε τη βαρύτητα στην κατακόρυφη ταχύτητα πολλαπλασιασμένη με τον χρόνο του frame

        controller.Move(velocity * Time.deltaTime); // Εφαρμόζουμε την κατακόρυφη κίνηση (π.χ. πτώση) μέσω του CharacterController
    }
} // Τέλος της κλάσης PlayerMovement
