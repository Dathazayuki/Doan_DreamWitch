using UnityEngine;

namespace DreamKnight.Systems.Interaction
{
    /// <summary>
    /// Interactive 2D Water with Spring-based Physics.
    /// Automatically converts a SpriteRenderer (long water sprite) into a dynamic mesh
    /// and simulates physical waves when a Rigidbody2D (Player/Enemy/Projectiles) interacts with it.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    [DisallowMultipleComponent]
    public class InteractiveWater2D : MonoBehaviour
    {
        [System.Serializable]
        public struct WaterNode
        {
            public float yPosition; // Local Y position of the node
            public float velocity;  // Vertical velocity of the node
            public float acceleration;
        }

        [Header("Spring Physics Settings")]
        [Tooltip("Number of vertical columns/nodes on the water surface.")]
        [SerializeField] private int nodeCount = 40;
        
        [Tooltip("Stiffness of the springs. Higher = faster and tighter waves.")]
        [SerializeField] private float springConstant = 0.025f;
        
        [Tooltip("Damping of the wave movement. Higher = waves die down faster.")]
        [SerializeField] private float damping = 0.05f;
        
        [Tooltip("Propagation speed of the wave to adjacent nodes.")]
        [SerializeField] private float spread = 0.06f;

        [Header("Collision settings")]
        [Tooltip("Force multiplier applied to water when a Rigidbody2D enters the trigger.")]
        [SerializeField] private float enterForceMultiplier = 0.15f;
        
        [Tooltip("Force multiplier applied continuously when a Rigidbody2D stays/moves inside the water.")]
        [SerializeField] private float stayForceMultiplier = 0.02f;

        // Physics node state
        private WaterNode[] nodes;
        
        // Mesh components (generated dynamically at runtime)
        private MeshFilter meshFilter;
        private MeshRenderer meshRenderer;
        private Mesh waterMesh;
        
        // Boundary dimensions (obtained from Collider / SpriteRenderer)
        private float waterLocalTopY;
        private float waterLocalBottomY;
        private float waterLocalLeftX;
        private float waterLocalWidth;

        // Vertices cache for updating mesh at runtime
        private Vector3[] vertices;
        private Vector2[] uvs;
        private int[] triangles;

        private void Start()
        {
            InitializeWaterMesh();
        }

        private void InitializeWaterMesh()
        {
            // 1. Get original SpriteRenderer data
            SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
            Sprite sprite = originalRenderer.sprite;

            if (sprite == null)
            {
                Debug.LogError($"[InteractiveWater2D] No sprite found on SpriteRenderer of {gameObject.name}!");
                return;
            }

            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            collider.isTrigger = true; // Ensure it is a trigger

            // Calculate boundaries using Collider offsets & sizes (local space)
            Vector2 colOffset = collider.offset;
            Vector2 colSize = collider.size;

            waterLocalTopY = colOffset.y + colSize.y * 0.5f;
            waterLocalBottomY = colOffset.y - colSize.y * 0.5f;
            waterLocalLeftX = colOffset.x - colSize.x * 0.5f;
            waterLocalWidth = colSize.x;

            // 2. Setup Spring Nodes
            nodes = new WaterNode[nodeCount];
            for (int i = 0; i < nodeCount; i++)
            {
                nodes[i].yPosition = waterLocalTopY;
                nodes[i].velocity = 0f;
                nodes[i].acceleration = 0f;
            }

            // 3. Setup Mesh Components dynamically on a child GameObject to avoid Renderer conflicts
            GameObject meshHolder = new GameObject("WaterMesh_Dynamic");
            meshHolder.transform.SetParent(transform, false);
            meshHolder.layer = gameObject.layer;

            meshFilter = meshHolder.AddComponent<MeshFilter>();
            meshRenderer = meshHolder.AddComponent<MeshRenderer>();

            // Create material using the sprite's texture and Sprites/Default shader
            Material waterMaterial = new Material(Shader.Find("Sprites/Default"));
            waterMaterial.mainTexture = sprite.texture;
            waterMaterial.color = originalRenderer.color;
            meshRenderer.sharedMaterial = waterMaterial;

            // Disable original renderer so it doesn't draw the static sprite
            originalRenderer.enabled = false;

            // 4. Generate Mesh structures
            waterMesh = new Mesh();
            waterMesh.name = "DynamicWaterMesh";

            int vertexCount = nodeCount * 2;
            vertices = new Vector3[vertexCount];
            uvs = new Vector2[vertexCount];
            triangles = new int[(nodeCount - 1) * 6];

            // Map UV coordinates from Sprite texture rect
            Rect uvRect = sprite.rect;
            float texWidth = sprite.texture.width;
            float texHeight = sprite.texture.height;

            float uMin = uvRect.xMin / texWidth;
            float uMax = uvRect.xMax / texWidth;
            float vMin = uvRect.yMin / texHeight;
            float vMax = uvRect.yMax / texHeight;

            for (int i = 0; i < nodeCount; i++)
            {
                float progress = (float)i / (nodeCount - 1);
                float u = Mathf.Lerp(uMin, uMax, progress);

                // Top UV (vMax)
                uvs[i] = new Vector2(u, vMax);
                // Bottom UV (vMin)
                uvs[i + nodeCount] = new Vector2(u, vMin);
            }

            // Generate Triangles
            int triIndex = 0;
            for (int i = 0; i < nodeCount - 1; i++)
            {
                int topLeft = i;
                int topRight = i + 1;
                int bottomLeft = i + nodeCount;
                int bottomRight = i + 1 + nodeCount;

                // Triangle 1
                triangles[triIndex++] = topLeft;
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomLeft;

                // Triangle 2
                triangles[triIndex++] = topRight;
                triangles[triIndex++] = bottomRight;
                triangles[triIndex++] = bottomLeft;
            }

            waterMesh.vertices = vertices;
            waterMesh.uv = uvs;
            waterMesh.triangles = triangles;

            meshFilter.mesh = waterMesh;

            UpdateMeshVertices();
        }

        private void Update()
        {
            if (nodes == null || nodes.Length == 0) return;

            // 1. Spring physics simulation (Verlet integration)
            for (int i = 0; i < nodeCount; i++)
            {
                float force = springConstant * (waterLocalTopY - nodes[i].yPosition);
                nodes[i].acceleration = force - nodes[i].velocity * damping;
                nodes[i].yPosition += nodes[i].velocity;
                nodes[i].velocity += nodes[i].acceleration;
            }

            // 2. Wave propagation to adjacent nodes
            float[] leftDeltas = new float[nodeCount];
            float[] rightDeltas = new float[nodeCount];

            // Perform propagation iterations for smooth fluid look
            for (int iteration = 0; iteration < 8; iteration++)
            {
                for (int i = 0; i < nodeCount; i++)
                {
                    if (i > 0)
                    {
                        leftDeltas[i] = spread * (nodes[i].yPosition - nodes[i - 1].yPosition);
                        nodes[i - 1].velocity += leftDeltas[i];
                    }
                    if (i < nodeCount - 1)
                    {
                        rightDeltas[i] = spread * (nodes[i].yPosition - nodes[i + 1].yPosition);
                        nodes[i + 1].velocity += rightDeltas[i];
                    }
                }
            }

            // 3. Update the mesh vertices based on computed node heights
            UpdateMeshVertices();
        }

        private void UpdateMeshVertices()
        {
            if (waterMesh == null) return;

            for (int i = 0; i < nodeCount; i++)
            {
                float progress = (float)i / (nodeCount - 1);
                float x = waterLocalLeftX + (waterLocalWidth * progress);

                // Top vertex (with wave height)
                vertices[i] = new Vector3(x, nodes[i].yPosition, 0f);
                // Bottom vertex (fixed)
                vertices[i + nodeCount] = new Vector3(x, waterLocalBottomY, 0f);
            }

            waterMesh.vertices = vertices;
            waterMesh.RecalculateBounds();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();
            if (rb == null) return;

            // Calculate impact velocity and location
            float entryVelocityY = rb.linearVelocity.y;
            if (Mathf.Abs(entryVelocityY) < 0.1f) return;

            int nearestNode = GetNearestNodeIndex(other.transform.position.x);

            // Apply splash force
            float force = entryVelocityY * enterForceMultiplier;
            nodes[nearestNode].velocity = force;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            Rigidbody2D rb = other.GetComponentInParent<Rigidbody2D>();
            if (rb == null) return;

            // Continuous ripples when moving inside water
            float horizontalVelocity = Mathf.Abs(rb.linearVelocity.x);
            if (horizontalVelocity < 0.1f) return;

            int nearestNode = GetNearestNodeIndex(other.transform.position.x);

            // Create gentle ripples relative to movement speed
            float rippleForce = -Mathf.Sign(Random.value - 0.5f) * horizontalVelocity * stayForceMultiplier;
            nodes[nearestNode].velocity += rippleForce;
        }

        private int GetNearestNodeIndex(float worldX)
        {
            // Convert world X to local X
            float localX = transform.InverseTransformPoint(new Vector3(worldX, 0f, 0f)).x;
            
            // Calculate progress percentage along the water width
            float relativeX = localX - waterLocalLeftX;
            float percentage = Mathf.Clamp01(relativeX / waterLocalWidth);
            
            return Mathf.RoundToInt(percentage * (nodeCount - 1));
        }

        private void OnDestroy()
        {
            if (waterMesh != null)
            {
                Destroy(waterMesh);
            }
        }
    }
}
