using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class Chunk : MonoBehaviour
{
    private const float UNITY_DEFAULT_PLANE_SCALE = 10f;

    private Color defaultDebugColor = Color.cyan;

    private int CHUNK_SIZE = 0;
    
    private const float PROCEDURAL_AMPLITUTE = 10f;

    public GameObject plane;
    
    private Vector2Int gridPos;

    private Vector3 basePos;

    private int gridDepth = 10;

    private int spaceBetweenGridDots = 0;

    public void CreateChunk(Vector2Int pos, int chunkSize)
    {
        this.gridPos = pos;
        this.CHUNK_SIZE = chunkSize;
        this.spaceBetweenGridDots = CHUNK_SIZE / (gridDepth - 1);

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= CHUNK_SIZE / 2;
        this.basePos.z -= CHUNK_SIZE / 2;
        CreateDebugSphere(basePos);

        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";
        this.plane.transform.localScale = Vector3.one * CHUNK_SIZE / UNITY_DEFAULT_PLANE_SCALE;
    }

    public void GenerateChunk()
    {
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);

        Vector3 debugSpherePos = Vector3.zero;
        for (int i = 0; i < this.gridDepth; i++) {
            debugSpherePos.x = this.basePos.x + this.spaceBetweenGridDots * i;

            for (int j = 0; j < this.gridDepth; j++)
            {
                debugSpherePos.z = this.basePos.z + this.spaceBetweenGridDots * j;
                debugSpherePos.y = 
                    (noise.GetNoise(
                        (this.gridPos.x * this.gridDepth) + i, 
                        (this.gridPos.y * this.gridDepth) + j
                    ) 
                    + 1) // Noise goes from 1 to -1, so sum one and it wont go "underground"
                    * PROCEDURAL_AMPLITUTE;

                CreateDebugSphere(debugSpherePos, Color.blue, .5f, "_debug_sphere_" +  i + "_" + j);
            }
        }
    }
    public void CreateDebugSphere(Vector3 pos)
    {
        CreateDebugSphere(pos, Color.cyan);
    }

    public void CreateDebugSphere(Vector3 pos, Color color, float scale = 1.0f, string name = "debug_sphere")
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.parent = this.gameObject.transform;
        sphere.name = name;
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * scale;

        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        sphereRenderer.material.color = color;
    }

    void Log()
    {
        Debug.Log("Chunk-" + "x: " + gridPos.x + " y: " + gridPos.y);
    }

    public void Load()
    {
        this.gameObject.SetActive(true);
    }
    public void Unload()
     {
        this.gameObject.SetActive(false);
    }
}
