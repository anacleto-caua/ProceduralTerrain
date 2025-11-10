using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using UnityEngine;

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
        AnaLogger.Log($"Filing terrain data at chunk: {this.name}");
        GenerateChunkTerrain();
    }

    public void Update()
    {
        // TODO:
        // This update shouldn't exist
        // consider deleting the script after the chunk have been generated
        // and having another(non MonoBehaviour) script to keep track of the chunk's existence
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
        await Task.Run(() => {
            // This batch is tied to *this specific worker thread*
            using (AnaLogger.BeginBatch())
            {
                FillTerrainHeightData();
            }
        });

        await Task.Run(() => {
            using (AnaLogger.BeginBatch())
            {
                WriteSinkMap();
            }
        });

        await Task.Run(() => {
            using (AnaLogger.BeginBatch())
            {
                IdentifyWorldSinks();
            }
        });

        await Task.Run(() => {
            using (AnaLogger.BeginBatch())
            {
                FindFlowBetweenWorldSinks();
            }
        });

        SetTerrainHeights();
    }

    private void SetTerrainHeights()
    {
        terrain.terrainData.SetHeights(0, 0, heightmap);
        isChunkGenerated = true;
    }

    private void FillTerrainHeightData()
    {
        AnaLogger.Log($"Began filling heightmap at chunk x: {this.gridPos.x} y: {this.gridPos.y}");
        // The universal coordinates for the ChunkNoise function
        int u_x, u_y = 0;

        // i is the growth on x - j is the growth on z
        for (int i = 0; i < this.heightmapResolution; i++)
        {
            u_x = (this.gridPos.x * this.heightmapResolution) + i - this.gridPos.x;

            for (int j = 0; j < this.heightmapResolution; j++)
            {
                u_y = (this.gridPos.y * this.heightmapResolution) + j - this.gridPos.y;

                // The coordinates for "steepness" around the chunk
                float northEdgeSteepness    = LiveEdgeNoise.GetNoise(this.gridPos.x - .5f, this.gridPos.y);
                float southEdgeSteepness    = LiveEdgeNoise.GetNoise(this.gridPos.x + .5f, this.gridPos.y);
                float eastEdgeSteepness     = LiveEdgeNoise.GetNoise(this.gridPos.x, this.gridPos.y - .5f);
                float westEdgeSteepness     = LiveEdgeNoise.GetNoise(this.gridPos.x, this.gridPos.y + .5f);

                // Just to make the code seems cleaner
                float a = (this.heightmapResolution) - 1;
                float n = 2*a;
                float m = a;

                float northTentDecline = 1 - Mathf.Abs((j / n) * 2 - 1);
                float southTentDecline = 1 - Mathf.Abs(( (n - j) / n) * 2 - 1);

                float eastTentDecline = 1 - Mathf.Abs((i / n) * 2 - 1);
                float westTentDecline = 1 - Mathf.Abs(( (n - i) / n) * 2 - 1);

                northTentDecline = 1;
                southTentDecline = 1;
                eastTentDecline = 1;
                westTentDecline = 1;

                float northSteepnessWeight  = (((1 - i) + (m - 1)) / m);
                float southSteepnessWeight  = 1 - (((1 - i) + (m - 1)) / m);

                float eastSteepnessWeight   = (((1 - j) + (m - 1)) / m);
                float westSteepnessWeight   = 1 - (((1 - j) + (m - 1)) / m);

                northSteepnessWeight *= northTentDecline;
                southSteepnessWeight *= southTentDecline;

                eastSteepnessWeight *= eastTentDecline;
                westSteepnessWeight *= westTentDecline;

                // TODO: Fiddle with this "magic value"
                float noiseWeight = .1f; 
                float edgeWeight =  1f - noiseWeight;

                // Pre-calculate the values
                float chunkNoiseValue = ChunkNoise.GetNoise(u_x, u_y) * noiseWeight;
                    chunkNoiseValue = 0;
                float edgeValue = (
                        northEdgeSteepness * northSteepnessWeight
                        + southEdgeSteepness * southSteepnessWeight
                        + eastEdgeSteepness * eastSteepnessWeight
                        + westEdgeSteepness * westSteepnessWeight
                    );
                float finalEdgeValue = edgeValue * edgeWeight;

                float actualHeightmap = 
                    (
                        chunkNoiseValue
                        +
                        finalEdgeValue
                    );
                // Logging
                AnaLogger.Log(
                    // Pad gridPos to 2 chars, i/j to 3, universal coords to 4
                    $"Chunk[{this.gridPos.x,2},{this.gridPos.y,2}] Coords[i:{i,3},j:{j,3}] (u:{u_x,4},{u_y,4})" +

                    // Pad all float values to 6 chars, while keeping 3 decimal places
                    $" | Noise: {chunkNoiseValue,6:F3}" +
                    $" | N-Edge: {northEdgeSteepness,6:F3} (N-W: {northSteepnessWeight,6:F3})" +
                    $" | S-Edge: {southEdgeSteepness,6:F3} (S-W: {southSteepnessWeight,6:F3})" +
                    $" | E-Edge: {eastEdgeSteepness,6:F3} (E-W: {eastSteepnessWeight,6:F3})" +
                    $" | W-Edge: {westEdgeSteepness,6:F3} (W-W: {westSteepnessWeight,6:F3})" +
                    $" | EdgeValue: {edgeValue,6:F3}" +
                    $" | FinalEdgeValue: {finalEdgeValue,6:F3}" +
                    $" | FinalNoiseValue: {actualHeightmap,6:F3}"
                );

                // Actually fills in the heightmap matrix
                heightmap[j, i] = (actualHeightmap);

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

    public void FindFlowBetweenWorldSinks()
    {
        float minSinkPointDistance = 30f;

        foreach (Sink sink in WorldSinks)
        {
            foreach(Sink innerSink in WorldSinks)
            {
                if (sink == innerSink)
                {
                    continue;
                }

                if(sink.Distance(innerSink) <= minSinkPointDistance)
                {
                    sink.pointsTo.Add(innerSink);
                }
            }
        }
    }

    public void GetNoise(int u_x, int u_y)
    {

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
        // Turn off the flag so gizmos will only run if solicited at this chunk
        canDrawGizmos = false;

        HandlePosRefGizmos();
        HandlePointGizmos();
        HandleSinkGizmos();
    }

    public void HandlePosRefGizmos()
    {
        // Draw the chunk corner/center markers
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(this.transform.position, 3f); // CENTER
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(basePos, 3f); // CORNER
    }

    public void HandlePointGizmos()
    {
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
                if (PseudoSinksMap[j, i] != null)
                {
                    switch (PseudoSinksMap[j, i].type)
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
    }
    
    public void HandleSinkGizmos()
    {
        float terrainActualHeight = terrain.terrainData.size.y;

        Color worldSinkColor = Color.darkBlue;
        float worldSinkBaseScale = 1.0f;

        Color gizmoArrowColor = Color.purple;

        float flowGraphBonusHeight = 15f;

        foreach (Sink sink in WorldSinks)
        {
            Vector3 vertexPos = this.basePos;
            vertexPos.x += sink.i * this.spaceBetweenGridVertexes;
            vertexPos.z += sink.j * this.spaceBetweenGridVertexes;
            vertexPos.y =
                this.basePos.y
                + (heightmap[sink.j, sink.i] * terrainActualHeight)
                + flowGraphBonusHeight; // A little more height helps to make it more visible

            Gizmos.color = worldSinkColor;
            Gizmos.DrawWireSphere(
                vertexPos,
                worldSinkBaseScale * sink.weight
                );

            // Draw the arrows point to nearby World Sinks
            Gizmos.color = gizmoArrowColor;
            foreach (Sink innerSink in sink.pointsTo)
            {
                Vector3 innerSinkPos = this.basePos;
                innerSinkPos.x += innerSink.i * this.spaceBetweenGridVertexes;
                innerSinkPos.z += innerSink.j * this.spaceBetweenGridVertexes;
                innerSinkPos.y =
                    this.basePos.y
                    + (heightmap[innerSink.j, innerSink.i] * terrainActualHeight)
                    + flowGraphBonusHeight; // A little more height helps to make it more visible

                GizmoArrow.Draw(vertexPos, innerSinkPos);
            }
        }
    }
}
