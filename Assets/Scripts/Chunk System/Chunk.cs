using System.Collections.Generic;
using System.Xml.Serialization;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public GameObject debugSpherePrefab;
    public Terrain terrain;
    public TerrainData terrainTemplate;
    private const float DEFAULT_UNITY_TERRAIN_HEIGHT_SCALE = 10f;

    private int chunkSize = 0;
    private const float PROCEDURAL_AMPLITUTE = 100f;
    private int heightmapResolution = 10;
    private int seed = 0;
    private float spaceBetweenGridVertexes;

    private Vector2Int gridPos;
    private Vector3 basePos;

    public void CreateChunk(Vector2Int pos, int chunkSize, int heightmapResolution, int seed)
    {
        this.gridPos = pos;
        this.chunkSize = chunkSize;
        this.heightmapResolution = heightmapResolution;
        this.seed = seed;

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= (float)this.chunkSize / 2f;
        this.basePos.z -= (float)this.chunkSize / 2f;

        // Debug Spheres for visualizing the chunk position
        CreateDebugSphere(this.transform.position, Color.red, 1f, "MIDDLE DEBUG SPHERE");
        CreateDebugSphere(basePos, Color.blue, 2f, "CORNER DEBUG SPHERE");

        this.spaceBetweenGridVertexes = (float)((float)(this.chunkSize) / (float)(this.heightmapResolution));

        CreateTerrainData();

        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";
    }

    private void CreateTerrainData()
    {
        // Creating the terrain
        TerrainData clonedData = Instantiate(terrainTemplate);

        Terrain.CreateTerrainGameObject(clonedData).TryGetComponent<Terrain>(out this.terrain);
        this.terrain.name = terrainTemplate.name + " (Clone)";
        this.terrain.gameObject.transform.position = this.basePos;

        this.terrain.terrainData.size = Vector3.one * this.chunkSize;
        this.terrain.terrainData.heightmapResolution = this.heightmapResolution;
    }

    public void GenerateChunk()
    {
        FastNoiseLite noise = new FastNoiseLite();
        noise.SetSeed(this.seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.010f);

        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(3);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
        noise.SetFractalWeightedStrength(0);
        noise.SetFractalPingPongStrength(2.0f);

        Vector3 vertexPos = this.basePos;
        // The universal coordinates for the noise function
        int u_x, u_y = 0;
        int startX, startY, endX, endY;

        startX = this.gridPos.x * this.heightmapResolution;
        endX = startX + this.heightmapResolution;

        startY = this.gridPos.y * this.heightmapResolution;
        endY = startY + this.heightmapResolution;

        // Vertices ordered for mesh creation
        float[,] heights = new float[this.heightmapResolution, this.heightmapResolution];
        float[,] heightsForTheTerrainData = new float[this.heightmapResolution, this.heightmapResolution];
        for (int i = 0; i < this.heightmapResolution; i++)
        {
            u_x = startX + i - this.gridPos.x;
            
            for (int j = 0; j < this.heightmapResolution; j++)
            {
                u_y = startY + j - this.gridPos.y;

                heights[i,j] = (noise.GetNoise(u_x, u_y)
                   + 1f) // Noise goes from 1 to -1, so sum one and it wont go under and over unity terrain object limitations ( under 0 and over +1 )
                   / 2.0f / DEFAULT_UNITY_TERRAIN_HEIGHT_SCALE // The unity terrain scale is too dramatic
                   ;

                vertexPos.x += i * this.spaceBetweenGridVertexes;
                vertexPos.z += j * this.spaceBetweenGridVertexes;
                vertexPos.y = heights[i,j] * PROCEDURAL_AMPLITUTE;

                CreateDebugSphere(vertexPos, Color.hotPink, 0.5f, "sph_u_grid:" + u_x + "__"+ u_y + "_[" + i + "," + j + "]");
                vertexPos = this.basePos;

                heightsForTheTerrainData[j, i] = heights[i, j]; 
            }
        }

        SetHeights(heightsForTheTerrainData);
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
        sphereRenderer.material.color = color;
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
