using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using UnityEngine.InputSystem.iOS;
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
    // PseudoSinks Map: organize the sinks on x, z and major on a single matrix
    private PseudoSink[,] PseudoSinksMap;

    // PseudoSinks: the lowest point per line (X and Z) on this chunk
    private PseudoSink[] PseudoSinksOnX; 
    private PseudoSink[] PseudoSinksOnZ;
    
    // Major PseudoSinks: The points that are the lowest on both the X and Z axis
    private PseudoSink[] MajorPseudoSinks;
    
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
        PseudoSinksOnX = new PseudoSink[this.heightmapResolution];
        PseudoSinksOnZ = new PseudoSink[this.heightmapResolution];
        PseudoSinksMap = new PseudoSink[this.heightmapResolution, this.heightmapResolution];
        MajorPseudoSinks = new PseudoSink[this.heightmapResolution];
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


    private async void GenerateChunkTerrain()
    {
        await Task.Run(() => { FillTerrainHeightData(); });

        WriteSinkMap();
        IdentifyWorldSinks();

        SetTerrainHeights();
    }

    private void SetTerrainHeights()
    {
        terrain.terrainData.SetHeights(0, 0, heightmap);
        isChunkGenerated = true;
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
                    (heightmap[j, i] < heightmap[PseudoSinksOnZ[i].j, PseudoSinksOnZ[i].i])
                    )
                {
                    PseudoSinksOnZ[i] = new(u_x, u_y, i, j, SinkType.SinkOnZ);
                }

                if (
                    (PseudoSinksOnX[j] == null) || 
                    (heightmap[j, i] < heightmap[PseudoSinksOnX[j].j, PseudoSinksOnX[j].i])
                    )
                {
                    PseudoSinksOnX[j] = new(u_x, u_y, i, j, SinkType.SinkOnX);
                }
            }

            //    // --------- NEEDS REWORK ---------
            //    // It would be possible and faster to identify major sinks here instead of outside this nested loop
            //    // Identify major sinks
            //    // Major sinks are points where that's the lowest point on both this line and column
            //    if (PseudoSinksOnZ[i].Equals(PseudoSinksOnX[j]))
            //    {
            //        PseudoSinksOnZ[i].type = SinkType.MajorSink;
            //        PseudoSinksOnX[j] = PseudoSinksOnZ[i];
            //        MajorPseudoSinks[i] = PseudoSinksOnZ[i];
            //    }
        }
    }

    private void WriteSinkMap()
    {

        void addToMap(PseudoSink sink) => PseudoSinksMap[sink.i, sink.j] = sink;
        Array.ForEach(PseudoSinksOnZ, addToMap);

        foreach (PseudoSink sink in PseudoSinksOnX)
        {
            // If there's already a sink in, means that this point is a major sink
            if (!(PseudoSinksMap[sink.i, sink.j] == null))
            {
                sink.type = SinkType.MajorSink;
                PseudoSinksMap[sink.i, sink.j] = sink;
                PseudoSinksOnZ[sink.i] = sink;
                MajorPseudoSinks[sink.i] = sink;
            }
            // Else just adds the sink to the map anyway
            else
            {
                PseudoSinksMap[sink.i, sink.j] = sink;
            }
        }
    }

    private void IdentifyWorldSinks()
    {
        float majorPseudoSinkWeightGain = 5f;
        float pseudoSinkWeightGain = 1f;

        int worldSinkSwallowRadius = 5;

        // Each MajorPSink becomes a new World Sink
        foreach (PseudoSink psk in MajorPseudoSinks)
        {
            if (
                ( psk == null ) ||
                ( !psk.isAvailable )
                )
            {
                continue;
            }
            Sink newSink = new(psk, majorPseudoSinkWeightGain);
            psk.isAvailable = false;

            // TODO: Check if adding all the sinks to a list and using a foreach wouldn't be faster
            // than this hellhole nested loops and if checks chain
            // Swallow all PseudoSinks around the New World Sink
            for (int x = newSink.i - worldSinkSwallowRadius; x < newSink.i + worldSinkSwallowRadius + 1; x++)
            {
                for (int y = newSink.j - worldSinkSwallowRadius; y < newSink.j + worldSinkSwallowRadius + 1; y++)
                {
                    if (
                        (x < 0 || x >= this.heightmapResolution)    ||      // Ignore coordinates outside the heightmap
                        (y < 0 || y >= this.heightmapResolution)    ||
                        (PseudoSinksMap[x, y] == null)              ||      // Ignore sinks that are not filled
                        (!PseudoSinksMap[x, y].isAvailable)                 // Ignore sinks that where already used
                        )
                    {
                        continue;
                    }

                    PseudoSink currentSink = PseudoSinksMap[x, y];
                    // Is this coordinate inside the circle
                    if (
                        Mathf.Pow((x - newSink.i), 2) + Mathf.Pow((y - newSink.j), 2)
                        <= Mathf.Pow(worldSinkSwallowRadius, 2)
                        )
                    {
                        float weightToSum = 0;

                        switch (currentSink.type)
                        {
                            case SinkType.MajorSink:
                                weightToSum = majorPseudoSinkWeightGain;
                                break;

                            default:
                                weightToSum = pseudoSinkWeightGain;
                                break;
                        }
                        newSink.weight += weightToSum;
                        currentSink.isAvailable = false;
                    }
                }
            }

            WorldSinks.Add(newSink);
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

    private void OnDrawGizmos()
    {
        // Check if we should draw gizmos at all
        if (!canDrawGizmos || !isChunkGenerated)
        {
            return;
        }

        // Draw the chunk corner/middle markers
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, 1f); // MIDDLE
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(basePos, 2f); // CORNER
        
        // Needed consts:
        float gizmosBonusHeight = .3f;
        
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
                    + gizmosBonusHeight; // A little more height helps to make it more visible
                
                // Sets the default scale and color
                cubeColor = DefaultSinkColor;
                cubeScale = DefaultScale;

                // Picks a new scale and color for sinks
                if ( PseudoSinksMap[j, i] != null )
                {
                    switch ( PseudoSinksMap[j, i].type )
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

        Color worldSinkColor = Color.darkBlue;
        float worldSinkBaseScale = 1.0f;
        float worldSinkBonusHeight = 1.0f;
        foreach (Sink sink in WorldSinks)
        {
            Vector3 vertexPos = this.basePos;
            vertexPos.x += sink.i * this.spaceBetweenGridVertexes;
            vertexPos.z += sink.j * this.spaceBetweenGridVertexes;
            vertexPos.y =
                this.basePos.y
                + (heightmap[sink.j, sink.i] * terrainActualHeight)
                + worldSinkBonusHeight; // A little more height helps to make it more visible

            Gizmos.color = worldSinkColor;
            Gizmos.DrawWireSphere(
                vertexPos, 
                worldSinkBaseScale * sink.weight
                );
        }

        canDrawGizmos = false;
    }
}
