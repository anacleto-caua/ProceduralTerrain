using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System.Linq;

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
    private readonly List<int> validResolutions = new List<int>
        { 33, 65, 129, 257, 513, 1025, 2049, 4097 };


    private void Awake()
    {
        // It needs to be cleared because the editor and play mode
        // share the same instance of the scriptable object and threat it differently
        ClearExistingChunks();
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
        Vector2Int currentChunkPos = new Vector2Int(Mathf.RoundToInt(playerTransform.position.x / chunkSize), Mathf.RoundToInt(playerTransform.position.z / chunkSize));

        for (int x = currentChunkPos.x - chunkRenderRadius; x < currentChunkPos.x + chunkRenderRadius + 1; x++)
        {
            for (int y = currentChunkPos.y - chunkRenderRadius; y < currentChunkPos.y + chunkRenderRadius + 1; y++)
            {
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
        // For now every chunk is just stored at loadedChunks, eventually I gotta implement the unload chunks functionality
    }

    void LoadChunk(Vector2Int pos)
    {
        if (!existingChunks.ContainsKey(pos)) {
            GenerateChunk(pos);
        }

        loadedChunks.Add(pos, existingChunks.Get(pos));
    }

    void GenerateChunk(Vector2Int pos)
    {
        // Instantiate the Chunk Prefab
        GameObject newChunk = Instantiate(chunkPrefab, new Vector3(pos.x * chunkSize, 0, pos.y * chunkSize), Quaternion.identity);

        if (!newChunk.TryGetComponent<Chunk>(out var chunkScript)) {
            Debug.Log("Chunk Script not found at prefab, creation script broken.");
            return;
        }

        chunkScript.CreateChunk(pos, chunkSize, heightmapResolution, seed);
        chunkScript.GenerateChunk();

        // What about chunk position???

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
    }

    public void OnEnable()
    {
        ClearExistingChunks();
    }

    public void OnDisable()
    {
    }

}
