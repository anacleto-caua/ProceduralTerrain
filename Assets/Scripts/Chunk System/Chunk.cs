using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public GameObject debugSpherePrefab;

    private const float UNITY_DEFAULT_PLANE_SCALE = 10f;
    private Color defaultDebugColor = Color.cyan;
    
    private int chunkSize = 0;
    private const float PROCEDURAL_AMPLITUTE = 10f;
    private int gridResolution = 10;
    private int seed = 0;

    private Vector2Int gridPos;
    private Vector3 basePos;
    private int spaceBetweenGridDots = 0;
    private MeshFilter meshFilter;

    public void CreateChunk(Vector2Int pos, int chunkSize, int gridResolution, int seed)
    {
        this.gridPos = pos;
        this.chunkSize = chunkSize;
        this.gridResolution = gridResolution;
        this.seed = seed;

        this.spaceBetweenGridDots = this.chunkSize / (gridResolution - 1);

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= this.chunkSize / 2;
        this.basePos.z -= this.chunkSize / 2;
        CreateDebugSphere(basePos);

        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";
        
        this.meshFilter = this.GetComponent<MeshFilter>();
    }

    public void GenerateChunk()
    {
        AnaLogger.Log("Generating chunk : " + gridPos);
        
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetSeed(this.seed);

        int index = 0;
        Vector3 vertexPos = Vector3.zero;
        Vector3 rawVertexPos = Vector3.zero;

        // Vertices ordered for mesh creation
        Vector3[] vertices = new Vector3[this.gridResolution * this.gridResolution];
        // Triangles for mesh creation, it consists of 3 index positions on the rawVertices array, in clockwise order
        List<int> triangles =  new List<int>();
        // Uv information for mesh creation
        Vector2[] uvs = new Vector2[this.gridResolution * this.gridResolution];

        for (int i = 0; i < this.gridResolution; i++) {
            vertexPos.x = this.basePos.x + this.spaceBetweenGridDots * i;
            rawVertexPos.x = this.spaceBetweenGridDots * i;

            for (int j = 0; j < this.gridResolution; j++)
            {

                // Filling in triangles for mesh creation
                if (
                    (j % 2 == 0)
                    && (j < this.gridResolution - 1) // There will be another position to the right
                    && (i < this.gridResolution - 1) // There will be another position to the south
                    )
                {
                    triangles.Add(index);
                    triangles.Add(index + 1);
                    triangles.Add(index + this.gridResolution);
                }
                else if (
                    (j % 2 != 0)
                    && (i < this.gridResolution - 1) // There will be another position to the south
                    )
                {
                    triangles.Add(index + this.gridResolution -1);
                    triangles.Add(index);
                    triangles.Add(index + this.gridResolution);
                }

                // Filling in uv data
                uvs[index] = new Vector2(i, j);

                // TODO: DEFINE I'M USING vertexPos or rawVertexPos
                
                // Creating the edge position on the mesh
                vertexPos.z = this.basePos.z + this.spaceBetweenGridDots * j;
                vertexPos.y =
                    (noise.GetNoise(
                        (this.gridPos.x * this.gridResolution) + i,
                        (this.gridPos.y * this.gridResolution) + j
                    )
                    + 1) // Noise goes from 1 to -1, so sum one and it wont go "underground"
                    * PROCEDURAL_AMPLITUTE;

                rawVertexPos.z = this.spaceBetweenGridDots * j;
                rawVertexPos.y =
                   (noise.GetNoise(
                       (this.gridPos.x * this.gridResolution) + i,
                       (this.gridPos.y * this.gridResolution) + j
                   )
                   + 1) // Noise goes from 1 to -1, so sum one and it wont go "underground"
                   * PROCEDURAL_AMPLITUTE;

                // Using the edge position
                vertices[index] = vertexPos;
                index++;
                CreateDebugSphere(rawVertexPos, Color.blue, .5f, "_debug_sphere_" +  i + "_" + j);
            }
        }

        AnaLogger.Log(triangles);
        AnaLogger.Log(vertices);
        //CreateMesh(vertices, triangles.ToArray(), uvs);
    }

    public void CreateMesh(Vector3[] vertices, int[] triangles, Vector2[] uvs, string name = "new mesh")
    {
        Mesh mesh = new Mesh();
        mesh.name = name;
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;

        mesh.RecalculateNormals();

        this.meshFilter.mesh = mesh;

        this.meshFilter.transform.position = new Vector3(0, 0, 0);
    }

    public void CreateDebugSphere(Vector3 pos)
    {
        CreateDebugSphere(pos, Color.cyan);
    }

    public void CreateDebugSphere(Vector3 pos, Color color, float scale = 1.0f, string name = "debug_sphere")
    {
        GameObject sphere = Instantiate(debugSpherePrefab);

        sphere.transform.parent = this.gameObject.transform;
        sphere.name = name;
        sphere.transform.position = pos;
        sphere.transform.localScale = Vector3.one * scale;

        Renderer sphereRenderer = sphere.GetComponent<Renderer>();
        sphereRenderer.sharedMaterial.color = color;
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
