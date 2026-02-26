using UnityEngine;

namespace MyProject.Ambient
{
    /// Κάνει τα σύννεφα να κινούνται αργά στον ουρανό.
    /// Μπορεί να τα κάνει linear είτε rotate pivot
    /// Setup:
    /// 1. Βάζουμε το script σε cloud object 
    /// 2. Επιλογή movement mode: Linear ή Rotate
    /// 3. Επιλογή speed (τα σύννεφα κινούνται πολύ αργά)
    public class CloudDrift : MonoBehaviour
    {
        /// κίνηση σε ευθεία ή περιστροφή γύρω από pivot
        public enum MovementMode
        {
            /// <summary>Move in a straight line along an axis.</summary>
            Linear,
            /// <summary>Rotate around a pivot point (Y axis).</summary>
            Rotate
        }

        [Header("Movement Settings")]

        /// Πως κινούνται τα σύννεφα: Linear = ευθεία, Rotate = περιστροφή γύρω από pivot.
        [Tooltip("Linear = move in a direction, Rotate = spin around pivot.")]
        [SerializeField]
        private MovementMode mode = MovementMode.Rotate;

        /// Ταχύτητα κίνησης.
        /// Linear: units per second.
        /// Rotate: degrees per second.
        [Tooltip("Movement speed. Keep very low for realistic clouds (e.g., 1-5).")]
        [SerializeField]
        private float speed = 2f;

        [Header("Linear Mode Settings")]

        /// Κατεύθυνσης κίνησης για Linear mode.
        [Tooltip("Direction cloud moves in Linear mode.")]
        [SerializeField]
        private Vector3 moveDirection = Vector3.right;

        /// Αν αληθές, τότε τα σύννεφα θα τυλίγονται όταν φτάνουν σε ένα όριο.
        [Tooltip("Wrap cloud position when it goes too far.")]
        [SerializeField]
        private bool wrapAround = true;

        /// Απόσταση από αρχική θέση πριν τυλιχτεί στην αντίθετη πλευρά.
        [Tooltip("Distance before cloud wraps to opposite side.")]
        [SerializeField]
        private float wrapDistance = 200f;

        [Header("Rotate Mode Settings")]

        /// Το pivot point για τη περιστροφή.
        /// Αν δεν έχει ρυθμιστεί, περιστρέφεται γύρω από το world (0,0,0).
        [Tooltip("Point to rotate around. Leave empty for world origin.")]
        [SerializeField]
        private Transform pivotPoint;

        /// Axis to rotate around. Default is Y (horizontal rotation).
        [Tooltip("Axis of rotation. Usually Y for horizontal movement.")]
        [SerializeField]
        private Vector3 rotationAxis = Vector3.up;

        /// Starting position for wrap calculations.
        private Vector3 startPosition;

        private void Start()
        {
            // Store start position for wrapping
            startPosition = transform.position;

            // Normalize move direction
            if (moveDirection != Vector3.zero)
            {
                moveDirection = moveDirection.normalized;
            }
        }

        private void Update()
        {
            switch (mode)
            {
                case MovementMode.Linear:
                    MoveLinear();
                    break;

                case MovementMode.Rotate:
                    MoveRotate();
                    break;
            }
        }


        /// Μετακίνηση συννέφων σε ευθεία γραμμή
        private void MoveLinear()
        {
            // Move in direction
            transform.position += moveDirection * speed * Time.deltaTime;

            // Check for wrap around
            if (wrapAround)
            {
                float distanceFromStart = Vector3.Distance(startPosition, transform.position);
                if (distanceFromStart >= wrapDistance)
                {
                    // Teleport to opposite side
                    transform.position = startPosition - (moveDirection * wrapDistance * 0.9f);
                }
            }
        }

        /// Περιστροφή συννέφων γύρω από ένα pivot point
        private void MoveRotate()
        {
            // Get pivot position
            Vector3 pivot = pivotPoint != null ? pivotPoint.position : Vector3.zero;

            // Rotate around pivot
            transform.RotateAround(pivot, rotationAxis, speed * Time.deltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (mode == MovementMode.Linear)
            {
                // Draw movement direction
                Gizmos.color = Color.cyan;
                Gizmos.DrawRay(transform.position, moveDirection * 10f);

                // Draw wrap bounds
                if (wrapAround)
                {
                    Gizmos.color = new Color(0, 1, 1, 0.3f);
                    Vector3 start = Application.isPlaying ? startPosition : transform.position;
                    Gizmos.DrawWireSphere(start, wrapDistance);
                }
            }
            else if (mode == MovementMode.Rotate)
            {
                // Draw pivot point
                Vector3 pivot = pivotPoint != null ? pivotPoint.position : Vector3.zero;
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pivot, 1f);

                // Draw line to pivot
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(transform.position, pivot);

                // Draw rotation circle
                float radius = Vector3.Distance(transform.position, pivot);
                DrawGizmoCircle(pivot, radius, 32);
            }
        }


        private void DrawGizmoCircle(Vector3 center, float radius, int segments)
        {
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(radius, 0, 0);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
        }
    }
}
