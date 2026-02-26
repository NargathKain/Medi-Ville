using UnityEngine;

namespace MyProject.Ambient
{
    /// Scroll επάνω σε material ώστε να δημιουργήσει την ψευδαίσθηση ροής νερού.
    /// Setup:
    /// 1. Attach το script σε stream/river mesh
    /// 2. Ρύθμιση scroll speed (X = horizontal, Y = vertical flow direction)
    /// 3. Material πρέπει texture με tiling enabled
    /// Note: Creates a material instance to avoid affecting other objects using the same material.
    public class ScrollingWater : MonoBehaviour
    {
        // SERIALIZED FIELDS

        [Header("Scroll Settings")]

        /// Ταχύτητα texture scrolling.
        /// X = horizontal scroll, Y = vertical scroll.
        /// Ρύθμιση με βάση προσανατολισμό ροής (π.χ. (0, 0.5) για κάθετη ροή).
        [Tooltip("Scroll speed (X = horizontal, Y = vertical). Typical: (0, 0.5) for vertical flow.")]
        [SerializeField]
        private Vector2 scrollSpeed = new Vector2(0f, 0.5f);

        [Header("Optional Settings")]

        /// Όνομα texture στο shader που θα scrollάρει.
        /// "_BaseMap" for URP Lit, "_MainTex" for legacy shaders.
        [Tooltip("Shader texture property name. URP Lit = '_BaseMap', Legacy = '_MainTex'.")]
        [SerializeField]
        private string texturePropertyName = "_BaseMap";

        /// Material index για κύληση (για αντικείμενα με πολλαπλά materials).
        [Tooltip("Material index for objects with multiple materials.")]
        [SerializeField]
        private int materialIndex = 0;

        // PRIVATE FIELDS

        /// Reference to renderer 
        private Renderer meshRenderer;

        /// instance του material 
        private Material material;

        /// Current texture offset.
        private Vector2 currentOffset;

        private void Start()
        {
            // Get renderer
            meshRenderer = GetComponent<Renderer>();
            if (meshRenderer == null)
            {
                Debug.LogError($"[ScrollingWater] {gameObject.name}: No Renderer found!");
                enabled = false;
                return;
            }

            // Validate material index
            if (materialIndex >= meshRenderer.materials.Length)
            {
                Debug.LogError($"[ScrollingWater] {gameObject.name}: Material index {materialIndex} out of range!");
                enabled = false;
                return;
            }

            // Get material instance (this creates a copy so we don't affect shared materials)
            material = meshRenderer.materials[materialIndex];

            // Get current offset
            currentOffset = material.GetTextureOffset(texturePropertyName);
        }

        private void Update()
        {
            if (material == null)
            {
                return;
            }

            // Update offset based on time
            currentOffset += scrollSpeed * Time.deltaTime;

            // Keep values in 0-1 range to prevent floating point issues over time
            currentOffset.x = currentOffset.x % 1f;
            currentOffset.y = currentOffset.y % 1f;

            // Apply offset to material
            material.SetTextureOffset(texturePropertyName, currentOffset);
        }

        private void OnDestroy()
        {
            // Clean up the material instance to prevent memory leaks
            if (material != null)
            {
                Destroy(material);
            }
        }
    }
}
