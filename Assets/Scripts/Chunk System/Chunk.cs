using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Chunk : MonoBehaviour
{
    public GameObject debugSpherePrefab;
    public Terrain terrain;
    public TerrainData terrainTemplate;

    private int chunkSize = 0;
    private int heightmapResolution = 10;
    private int seed = 0;
    public float terrainAmplitude;
    
    private float spaceBetweenGridVertexes;

    // This constant tries to match the terrain amplitude closer to that of Unitys terrain
    // Only works on amplitude = 0.3 :)
    private const float TERRAIN_AMPLITUDE_RELATED_TO_DEBUG_SPHERES = 500f;

    private Vector2Int gridPos;
    private Vector3 basePos;

    private float[,] heightmap;
    FastNoiseLite noise;

    // False to break the terrain generation process in steps
    private bool canGenerateTerrain = false;
    private bool isHeightmapFilled = false;

    public void Update()
    {
        // Looks terrible, but it's just to ensure both functions run only once
        if (canGenerateTerrain) 
        {
            FillTerrainWrapper();
        }else if (isHeightmapFilled)
        {
            CreateTerrainDebugSpheres();
            SetTerrainHeights();
            isHeightmapFilled = false;
        }

    }

    // This function should run at the start of every Chunk life spam
    public void CreateChunk(Vector2Int pos, int chunkSize, int heightmapResolution, int seed, float terrainAmplitute)
    {
        this.gridPos = pos;
        this.chunkSize = chunkSize;
        this.heightmapResolution = heightmapResolution;
        this.seed = seed;
        this.terrainAmplitude = terrainAmplitute;

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= (float)this.chunkSize / 2f;
        this.basePos.z -= (float)this.chunkSize / 2f;

        this.heightmap = new float[this.heightmapResolution, this.heightmapResolution];

        this.spaceBetweenGridVertexes = (float)((float)(this.chunkSize) / (float)(this.heightmapResolution));


        // Debug Spheres for visualizing the chunk position
        CreateDebugSphere(this.transform.position, Color.red, 1f, "MIDDLE DEBUG SPHERE");
        CreateDebugSphere(basePos, Color.blue, 2f, "CORNER DEBUG SPHERE");


        CreateNoiseInstance();
        CreateTerrainInstance();
        
        // Gives a unique name so we now what's this about
        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";

        canGenerateTerrain = true; // Allow the code to proceed and generate the terrain at the update function
    }

    private void CreateNoiseInstance()
    {
        // Creates the noise instance
        this.noise = new FastNoiseLite();
        noise.SetSeed(this.seed);
        noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        noise.SetFrequency(0.010f);

        noise.SetFractalType(FastNoiseLite.FractalType.FBm);
        noise.SetFractalOctaves(3);
        noise.SetFractalLacunarity(2.0f);
        noise.SetFractalGain(0.5f);
        noise.SetFractalWeightedStrength(0);
        noise.SetFractalPingPongStrength(2.0f);
    }

    private void CreateTerrainInstance()
    {
        // Creating the terrain
        TerrainData clonedData = Instantiate(terrainTemplate);

        Terrain.CreateTerrainGameObject(clonedData).TryGetComponent<Terrain>(out this.terrain);
        this.terrain.name = terrainTemplate.name + " (Clone)";
        this.terrain.gameObject.transform.position = this.basePos;

        this.terrain.terrainData.size = Vector3.one * this.chunkSize;
        this.terrain.terrainData.heightmapResolution = this.heightmapResolution;

        this.terrain.transform.parent = this.gameObject.transform;
    }

    private async void FillTerrainWrapper()
    {
        canGenerateTerrain = false;
        await Task.Run(() => { FillTerrainHeightData(); });
        isHeightmapFilled = true;
    }

    private void FillTerrainHeightData()
    {
        // The universal coordinates for the noise function
        int u_x, u_y = 0;

        for (int i = 0; i < this.heightmapResolution; i++)
        {
            u_x = (this.gridPos.x * this.heightmapResolution) + i - this.gridPos.x;

            for (int j = 0; j < this.heightmapResolution; j++)
            {
                u_y = (this.gridPos.y * this.heightmapResolution) + j - this.gridPos.y;

                heightmap[j, i] = (
                    (
                        noise.GetNoise(u_x, u_y)
                        + 1f) / 2.0f // Noise goes from 1 to -1 Unity's terrain need it between 0 and 1
                    )
                        * terrainAmplitude; // The Unity's terrain scale is too dramatic
            }
        }
    }

    public void SetTerrainHeights()
    {
        terrain.terrainData.SetHeights(0, 0, heightmap);
    }

    public void CreateTerrainDebugSpheres()
    {
        int u_x, u_y = 0;

        for (int i = 0; i < this.heightmapResolution; i++)
        {
            u_x = (this.gridPos.x * this.heightmapResolution) + i - this.gridPos.x;

            for (int j = 0; j < this.heightmapResolution; j++)
            {
                u_y = (this.gridPos.y * this.heightmapResolution) + j - this.gridPos.y;

                Vector3 vertexPos = this.basePos;
                vertexPos.x += j * this.spaceBetweenGridVertexes;
                vertexPos.z += i * this.spaceBetweenGridVertexes;
                vertexPos.y = heightmap[i, j] * terrainAmplitude * TERRAIN_AMPLITUDE_RELATED_TO_DEBUG_SPHERES;

                CreateDebugSphere(vertexPos, Color.hotPink, 0.5f, "sph_u_grid:" + u_x + "__" + u_y + "_[" + i + "," + j + "]");
            }
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
