using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Unity.Hierarchy;
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

    public Vector2Int gridPos;
    private Vector3 basePos;

    private float[,] heightmap;
    FastNoiseLite noise;

    // Terrain generation steps
    private bool canDrawGizmos = false;
    private bool shouldDrawGizmos = false;

    // Sinks 
    private Sink[] SinksOnX; 
    private Sink[] SinksOnZ;

    public void Start()
    {
        GenerateChunkTerrain();
    }

    public void Update()
    {
    }

    // This function should run at the start of every Chunk life spam
    public void CreateChunk(
        Vector2Int pos, int chunkSize, 
        int heightmapResolution, int seed, 
        float terrainAmplitute, bool createDebugSpheres = false
        )
    {
        this.gridPos = pos;
        this.chunkSize = chunkSize;
        this.heightmapResolution = heightmapResolution;
        this.seed = seed;
        this.terrainAmplitude = terrainAmplitute;

        this.shouldDrawGizmos = createDebugSpheres;

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= (float)this.chunkSize / 2f;
        this.basePos.z -= (float)this.chunkSize / 2f;

        this.heightmap = new float[this.heightmapResolution, this.heightmapResolution];

        this.spaceBetweenGridVertexes = (float)((float)(this.chunkSize) / (float)(this.heightmapResolution));

        // Some functions to reduce bloat
        CreateNoiseInstance();
        CreateTerrainInstance();
        
        // Gives a unique name so we now what's this about
        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";

        // Sinks ident variables
        SinksOnX = new Sink[this.heightmapResolution];
        SinksOnZ = new Sink[this.heightmapResolution];
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

    private async void GenerateChunkTerrain()
    {
        await Task.Run(() => { FillTerrainHeightData(); });
        SetTerrainHeights();
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
                // Sets the first one as the smallest so we can compare with the ones to come
                if (j == 0)
                {
                    SinksOnZ[i] = new Sink(u_x, u_y, i, j);
                }

                u_y = (this.gridPos.y * this.heightmapResolution) + j - this.gridPos.y;

                heightmap[j, i] = (
                    (
                        noise.GetNoise(u_x, u_y)
                        + 1f) / 2.0f // Noise goes from 1 to -1 Unity's terrain need it between 0 and 1
                    )
                        * terrainAmplitude; // The Unity's terrain scale is too dramatic

                
                if (heightmap[j,i] < heightmap[SinksOnZ[i].j, SinksOnZ[i].i])
                {
                    SinksOnZ[i] = new Sink(u_x, u_y, i, j);
                }

                if (SinksOnX[j] == null)
                {
                    SinksOnX[j] = new Sink(u_x, u_y, i, j);
                } 
                else if (heightmap[j, i] < heightmap[SinksOnX[j].j, SinksOnX[j].i])
                {
                    SinksOnX[j] = new Sink(u_x, u_y, i, j);
                }

            }


        }

        if (shouldDrawGizmos)
        {
            canDrawGizmos = true;
        }
    }

    public void SetTerrainHeights()
    {
        terrain.terrainData.SetHeights(0, 0, heightmap);
    }

    private void OnDrawGizmos()
    {
        // Check if we should draw gizmos at all - MAY BE UNUSEFULL
        if (!canDrawGizmos)
        {
            return;
        }

        // Draw the chunk corner/middle markers
        // Check if basePos has been initialized
        if (chunkSize > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(this.transform.position, 1f); // MIDDLE
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(basePos, 2f); // CORNER
        }

        float terrainActualHeight = terrain.terrainData.size.y;
        Color sphereColor;
        float sphereRadius; // Gizmos.DrawSphere uses radius, not scale

        // In this loop: i = row (z-axis), j = column (x-axis)
        for (int i = 0; i < this.heightmapResolution; i++)
        {
            for (int j = 0; j < this.heightmapResolution; j++)
            {
                Vector3 vertexPos = this.basePos;
                vertexPos.x += j * this.spaceBetweenGridVertexes;
                vertexPos.z += i * this.spaceBetweenGridVertexes;

                // --- CORRECTED Y CALCULATION ---
                // Your original code multiplied by terrainAmplitude *twice* and used a magic number.
                // The *actual* world height is base_y + (heightmap_value * terrain_data_height).
                // We set terrain_data_height to chunkSize in CreateTerrainInstance.
                vertexPos.y = this.basePos.y + (heightmap[i, j] * terrainActualHeight);

                // Resets the colors for normal points
                sphereColor = Color.magenta; // Color.hotPink doesn't exist, magenta is close
                sphereRadius = 0.5f;

                if (SinksOnZ[j].j == i)
                {
                    sphereColor = new Color(0, 1, 1); // Cyan
                    sphereRadius = 1.3f;
                }

                if (SinksOnX[i].i == j)
                {
                    sphereColor = new Color(1, 1, 0); // Yellow
                    sphereRadius = 1.3f;
                }

                // That's a "major sink" where this point is the lowest both in its X and Z lines
                if ((SinksOnX[i].i == j) && (SinksOnZ[j].j == i))
                {
                    sphereColor = new Color(1, 0, 1); // Magenta (overwrites normal)
                    sphereRadius = 1.5f;
                }

                Gizmos.color = sphereColor;
                Gizmos.DrawSphere(vertexPos, sphereRadius);
            }
        }
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
