using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public GameObject debugSpherePrefab;
    public Terrain terrain;

    private const float UNITY_DEFAULT_TERRAIN_SCALE = 100f;
    private Color defaultDebugColor = Color.cyan;
    
    private int chunkSize = 0;
    private const float PROCEDURAL_AMPLITUTE = 10f;
    private int heightmapResolution = 10;
    private int seed = 0;

    private Vector2Int gridPos;
    private Vector3 basePos;
    private float spaceBetweenGridDots = 0;
    

    public void CreateChunk(Vector2Int pos, int chunkSize, int heightmapResolution, int seed)
    {
        this.gridPos = pos;
        this.chunkSize = chunkSize;
        this.heightmapResolution = heightmapResolution;
        this.seed = seed;

        this.spaceBetweenGridDots = (float)(
            (float)(this.chunkSize) / ((float)(heightmapResolution))
            );

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= this.chunkSize / 2;
        this.basePos.z -= this.chunkSize / 2;
        CreateDebugSphere(basePos);

        // Setting terrain data
        this.terrain.gameObject.transform.position = this.basePos;
        this.terrain.terrainData.size = Vector3.one * this.chunkSize;
        this.terrain.terrainData.heightmapResolution = this.heightmapResolution;

        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";
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
        Vector3[] vertices = new Vector3[this.heightmapResolution * this.heightmapResolution];
        float[,] heights = new float[this.heightmapResolution,this.heightmapResolution];
        for (int i = 0; i < this.heightmapResolution; i++) {
            rawVertexPos.x = this.spaceBetweenGridDots * i;

            for (int j = 0; j < this.heightmapResolution; j++)
            {
                heights[i,j] = (noise.GetNoise(
                       (this.gridPos.x * this.heightmapResolution) + i,
                       (this.gridPos.y * this.heightmapResolution) + j
                   )
                   + 1) // Noise goes from 1 to -1, so sum one and it wont go "underground"
                   //* PROCEDURAL_AMPLITUTE
                   ;

                continue;
                rawVertexPos.z = this.spaceBetweenGridDots * j;
                rawVertexPos.y =
                   (noise.GetNoise(
                       (this.gridPos.x * this.heightmapResolution) + i,
                       (this.gridPos.y * this.heightmapResolution) + j
                   )
                   + 1) // Noise goes from 1 to -1, so sum one and it wont go "underground"
                   * PROCEDURAL_AMPLITUTE;



                // Using the edge position
                vertices[index] = vertexPos;
                index++;

                Vector3 debugSpherePos = rawVertexPos;
                debugSpherePos.x += this.basePos.x;
                debugSpherePos.z += this.basePos.z;
                //CreateDebugSphere(debugSpherePos, Color.blue, .5f, "_debug_sphere_" +  i + "_" + j);
            }
        }

        //AnaLogger.Log(vertices);
        SetHeights(heights);
    } 

    public void SetHeights(float[,] heights)
    {
        for (int i = 0; i < heights.Length; i++)
        {
            terrain.terrainData.SetHeights(0, 0, heights);
        }
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
    public void Load()
    {
        this.gameObject.SetActive(true);
    }
    public void Unload()
     {
        this.gameObject.SetActive(false);
    }
}
