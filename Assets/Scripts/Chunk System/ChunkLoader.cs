using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class ChunkLoader : MonoBehaviour
{
    [InspectorLabel("Chunk Lists")]
    public ChunkList loadedChunks;
    public ChunkList existingChunks;

    [InspectorLabel("Necessary References")]
    public Transform playerTransform;
    public GameObject chunkPrefab;

    [InspectorLabel("Chunk Configs")]
    public int chunkRenderRadius = 1;
    public int seed = 0;
    public int chunkSize = 50;
    
    [Tooltip("Resolution will be clamped to the nearest valid value.")]
    public int heightmapResolution = 513;

    [Tooltip("This defines the difference between the lower and highest part of the terrain, keep between 0 and 1.")]
    public float terrainAmplitude = 1f;

    [Tooltip("Determine how much the chunk noise and the live edge noise affect the end result, their sum shall equate to 1f")]
    public float noiseWeight = .1f;
    public float slopeWeight = .9f;

    [InspectorLabel("Generation debug options")]
    public bool createDebugSpheres = true;
    public int debugSpheresRange = 1;

    private readonly List<int> validResolutions = new List<int>
        { 33, 65, 129, 257, 513, 1025, 2049, 4097 };

    private void Awake()
    {
        // It needs to be cleared because of the editor and play mode
        // share the same instance of the scriptable object and threat it differently
        ClearExistingChunks();

        // Feeds the seed into the Noise Singleton
        ChunkNoise.SetSeed(seed);
    }

    void Start()
    {

    }
    
    void Update()
    {
        HandleChunkLoading();
    }

    public void HandleChunkLoading()
    {
        // Translate word position to the grid pos position
        Vector2Int currentChunkPos = new Vector2Int(
                Mathf.RoundToInt(playerTransform.position.x / chunkSize), 
                Mathf.RoundToInt(playerTransform.position.z / chunkSize)
            );

        // Loops through the chunks supposed to be around the player
        for (int x = currentChunkPos.x - chunkRenderRadius; x < currentChunkPos.x + chunkRenderRadius + 1; x++)
        {
            for (int y = currentChunkPos.y - chunkRenderRadius; y < currentChunkPos.y + chunkRenderRadius + 1; y++)
            {
                // Is this coordinate inside the circle
                // Consider if removing this if would just make it all better(at least cleaner, but faster?)
                if (
                    Mathf.Pow((x - currentChunkPos.x), 2) + Mathf.Pow((y - currentChunkPos.y), 2)
                    <= Mathf.Pow(chunkRenderRadius, 2)
                    )
                {
                    Vector2Int loadPos = new Vector2Int(x, y);

                    // This chunk should be loaded
                    if (!loadedChunks.ContainsKey(loadPos))
                    {
                        //Debug.Log("Loading pos- x: " + x + " y: " + y);
                        LoadChunk(loadPos);
                    }
                }
            }
        }

        List<Action> UnloadCommandList = new List<Action>();
        // Loops at all the chunks loaded and check if they should be loaded on not
        foreach ( Chunk chunk in loadedChunks )
        {
            if (Vector2Int.Distance(chunk.gridPos, currentChunkPos) > chunkRenderRadius)
            {
                // Stack a list of actions, as calling the function now would damage the foreach loop
                UnloadCommandList.Add(() => UnloadChunk(chunk));
            }
        }

        // Unstack the actions, seems like bad code...
        foreach (Action action in UnloadCommandList)
        {
            action();
        }
    }

    void LoadChunk(Vector2Int pos)
    {
        if (!existingChunks.ContainsKey(pos))
        {
            GenerateChunk(pos);
        }

        Chunk chunk = existingChunks.Get(pos); 
        chunk.Load();

        loadedChunks.Add(pos, chunk);
    }

    void UnloadChunk(Chunk chunk)
    {
        loadedChunks.Remove(chunk.gridPos);
        chunk.Unload();
    }

    void GenerateChunk(Vector2Int pos)
    {
        // Instantiate the Chunk Prefab
        GameObject newChunk = Instantiate(chunkPrefab, new Vector3(pos.x * chunkSize, 0, pos.y * chunkSize), Quaternion.identity);

        if (!newChunk.TryGetComponent<Chunk>(out var chunkScript)) {
            Debug.Log("Chunk Script not found at prefab, creation script broken.");
            return;
        }

        chunkScript.CreateChunk(
            pos, 
            chunkSize, 
            heightmapResolution, 
            terrainAmplitude,
            noiseWeight,
            slopeWeight
        );

        existingChunks.Add(pos, chunkScript);
    }

    public void ClearExistingChunks()
    {
        existingChunks.DestroyList();
        loadedChunks.DestroyList();
    }

    // This function is called in the editor when the script is loaded or a value is changed in the Inspector.
    private void OnValidate()
    {
        // Find the closest value in our list to the one entered in the inspector.
        int closest = validResolutions.OrderBy(item => Mathf.Abs(heightmapResolution - item)).First();

        // Snap the resolution to that closest valid value.
        heightmapResolution = closest;

        // Keep amplitude between 0 and 1
        if (terrainAmplitude <= 0)
        {
            terrainAmplitude = 0;
        }
        else if (terrainAmplitude > 1)
        {
            terrainAmplitude = 1;
        }
    }

    private void OnDrawGizmos()
    {
        Vector2Int currentChunkPos = new Vector2Int(
            Mathf.RoundToInt(playerTransform.position.x / chunkSize),
            Mathf.RoundToInt(playerTransform.position.z / chunkSize)
        );

        // Loops through the chunks supposed to be around the player
        for (int x = currentChunkPos.x - debugSpheresRange; x < currentChunkPos.x + this.debugSpheresRange + 1; x++)
        {
            for (int y = currentChunkPos.y - debugSpheresRange; y < currentChunkPos.y + this.debugSpheresRange + 1; y++)
            {
                // Test if they're in a circle, may not be needed, just bad
                if (
                    Mathf.Pow((x - currentChunkPos.x), 2) + Mathf.Pow((y - currentChunkPos.y), 2)
                    <= Mathf.Pow(debugSpheresRange, 2)
                    )
                {
                    Vector2Int pos = new(x, y);
                    if (loadedChunks.ContainsKey(pos))
                    {
                        loadedChunks.Get(pos).canDrawGizmos = true;
                    }
                }
            }
        }
    }

    public void OnEnable()
    {
        ClearExistingChunks();
    }

    public void OnDisable()
    {
    }

}
