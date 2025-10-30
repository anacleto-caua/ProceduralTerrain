using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.iOS;

public class Chunk : MonoBehaviour
{
    public Terrain terrain;
    public TerrainData terrainTemplate;

    private int chunkSize = 0;
    private int heightmapResolution = 10;
    private float terrainAmplitude;
    
    private float spaceBetweenGridVertexes;

    public Vector2Int gridPos;
    private Vector3 basePos;

    private float[,] heightmap;

    // Terrain generation steps
    public bool isChunkGenerated = false;
    public bool canDrawGizmos = false;

    // TODO: Consider a better naming for sinks
    // Sinks Map: organize the sinks on x, z and major on a single matrix
    private Sink[,] SinksMap;

    // Sinks: the lowest point per line (X and Z) on this chunk
    private Sink[] SinksOnX; 
    private Sink[] SinksOnZ;
    
    // Major Sinks: The points that are the lowest on both the X and Z axis
    private Sink[] MajorSinks;
    
    // Real Sinks: The ones used to generate the river graph
    private List<Sink> WorldSinks;


    public void Start()
    {
        GenerateChunkTerrain();
    }

    public void Update()
    {
    }

    // This function should run at the start of every Chunk life spam
    public void CreateChunk(
        Vector2Int pos, 
        int chunkSize, 
        int heightmapResolution, 
        float terrainAmplitude
        )
    {
        this.gridPos = pos;
        this.chunkSize = chunkSize;
        this.heightmapResolution = heightmapResolution;
        this.terrainAmplitude = terrainAmplitude;

        // This calculates the "0 0 position" for the chunk, since its placed by the center
        this.basePos = this.transform.position;
        this.basePos.x -= (float)this.chunkSize / 2f;
        this.basePos.z -= (float)this.chunkSize / 2f;

        this.heightmap = new float[this.heightmapResolution, this.heightmapResolution];

        this.spaceBetweenGridVertexes = (float)((float)(this.chunkSize) / (float)(this.heightmapResolution));

        // Some functions to reduce bloat
        CreateTerrainInstance();
        
        // Gives a unique name so we now what's this about
        this.gameObject.name = "Chunk|" + pos.x + "|" + pos.y + "|";

        // Sinks ident variables
        SinksOnX = new Sink[this.heightmapResolution];
        SinksOnZ = new Sink[this.heightmapResolution];
        SinksMap = new Sink[this.heightmapResolution, this.heightmapResolution];
        MajorSinks = new Sink[this.heightmapResolution];
        WorldSinks = new List<Sink>();
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
                        ChunkNoise.GetNoise(u_x, u_y)
                        + 1f) / 2.0f // Noise goes from 1 to -1 Unity's terrain need it between 0 and 1
                    )
                        * terrainAmplitude; // The Unity's terrain scale is too dramatic

                if (
                    (j == 0) || 
                    (heightmap[j, i] < heightmap[SinksOnZ[i].j, SinksOnZ[i].i])
                    )
                {
                    SinksOnZ[i] = new(u_x, u_y, i, j, SinkType.SinkOnZ);
                }

                if (
                    (SinksOnX[j] == null) || 
                    (heightmap[j, i] < heightmap[SinksOnX[j].j, SinksOnX[j].i])
                    )
                {
                    SinksOnX[j] = new(u_x, u_y, i, j, SinkType.SinkOnX);
                }
            }

            //    // --------- NEEDS REWORK ---------
            //    // It would be possible and faster to identify major sinks here instead of outside this nested loop
            //    // Identify major sinks
            //    // Major sinks are points where that's the lowest point on both this line and column
            //    if (SinksOnZ[i].Equals(SinksOnX[j]))
            //    {
            //        SinksOnZ[i].type = SinkType.MajorSink;
            //        SinksOnX[j] = SinksOnZ[i];
            //        MajorSinks[i] = SinksOnZ[i];
            //    }
        }
        WriteSinkMap();
    }

    private void WriteSinkMap()
    {

        void addToMap(Sink sink) => SinksMap[sink.i, sink.j] = sink;
        Array.ForEach(SinksOnZ, addToMap);

        foreach (Sink sink in SinksOnX)
        {
            // If there's already a sink in, means that this point is a major sink
            if (!(SinksMap[sink.i, sink.j] == null))
            {
                sink.type = SinkType.MajorSink;
                SinksMap[sink.i, sink.j] = sink;
                SinksOnZ[sink.i] = sink;
                MajorSinks[sink.i] = sink;
            }
            // Else just adds the sink to the map anyway
            else
            {
                SinksMap[sink.i, sink.j] = sink;
            }
        }
    }

    private async void GenerateChunkTerrain()
    {
        await Task.Run(() => { FillTerrainHeightData(); });
        SetTerrainHeights();
    }

    private void SetTerrainHeights()
    {
        terrain.terrainData.SetHeights(0, 0, heightmap);
        isChunkGenerated = true;
    }

    private void OnDrawGizmos()
    {
        // Check if we should draw gizmos at all
        if (!canDrawGizmos || !isChunkGenerated)
        {
            return;
        }

        // Draw the chunk corner/middle markers
        // Check if basePos has been initialized
        if (chunkSize > 0)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(this.transform.position, 1f); // MIDDLE
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(basePos, 2f); // CORNER
        }

        // Possible colors:
        Color DefaultSinkColor = new(0, .5f, 1, 1); // Light blue
        float DefaultScale = .5f;

        Color XSinkColor = new(.5f, 1, 0, 1); // Lime green
        Color ZSinkColor = new(1, .5f, 0, 1); // Orange
        float SinkScale = 1.3f;

        Color MajorSinkColor = new(1, 0, 1, 1); // Magenta
        float MajorSinkScale = 1.5f;


        float terrainActualHeight = terrain.terrainData.size.y;
        Color cubeColor;
        float cubeScale;

        // In this loop: i = row (z-axis), j = column (x-axis)
        for (int i = 0; i < this.heightmapResolution; i++)
        {
            for (int j = 0; j < this.heightmapResolution; j++)
            {
                Vector3 vertexPos = this.basePos;
                vertexPos.x += j * this.spaceBetweenGridVertexes;
                vertexPos.z += i * this.spaceBetweenGridVertexes;

                vertexPos.y = 
                    this.basePos.y 
                    + (heightmap[i, j] * terrainActualHeight)
                    + .3f; // A little more height helps to make it more visible
                
                // Sets the default scale and color
                cubeColor = DefaultSinkColor;
                cubeScale = DefaultScale;

                // Picks a new scale and color for sinks
                if ( SinksMap[j, i] != null )
                {
                    switch ( SinksMap[j, i].type )
                    {
                        case SinkType.SinkOnX:
                            cubeColor = XSinkColor;
                            cubeScale = SinkScale;
                            break;

                        case SinkType.SinkOnZ:
                            cubeColor = ZSinkColor;
                            cubeScale = SinkScale;
                            break;

                        case SinkType.MajorSink:
                            cubeColor = MajorSinkColor;
                            cubeScale = MajorSinkScale;
                            break;

                        default:
                            throw new InvalidEnumArgumentException();
                    }
                }

                Gizmos.color = cubeColor;
                Gizmos.DrawCube(vertexPos, Vector3.one * cubeScale);

                // Wire Cubes are barely visible on the current setup, although more efficient, may use it later
                //Gizmos.DrawWireCube(vertexPos, Vector3.one * cubeScale);
            }
        }

        canDrawGizmos = false;
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
