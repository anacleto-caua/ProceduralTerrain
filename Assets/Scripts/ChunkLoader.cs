using System.Collections.Generic;
using UnityEngine;

public class ChunkLoader : MonoBehaviour
{
    public Transform playerTransform;

    public GameObject chunkPrefab;


    private const int CHUNK_SIZE = 50;
     
    private int chunkRenderRadius = 8;

    private Dictionary<Vector2Int, Chunk> loadedChunks = new Dictionary<Vector2Int, Chunk>();
    
    private Dictionary<Vector2Int, Chunk> existingChunks = new Dictionary<Vector2Int, Chunk>();

    void Start()
    {

    }
    
    void Update()
    {
        Vector2Int currentChunkPos = new Vector2Int(Mathf.RoundToInt(playerTransform.position.x / CHUNK_SIZE), Mathf.RoundToInt(playerTransform.position.z / CHUNK_SIZE));

        char[,] test = new char[100,100];

        for(int x = currentChunkPos.x - chunkRenderRadius; x < currentChunkPos.x + chunkRenderRadius + 1; x++){
            for(int y = currentChunkPos.y - chunkRenderRadius; y < currentChunkPos.y + chunkRenderRadius + 1; y++){
                if (
                    Mathf.Pow((x - currentChunkPos.x), 2) + Mathf.Pow((y - currentChunkPos.y), 2) 
                    <= Mathf.Pow(chunkRenderRadius, 2)
                    )
                {
                    Vector2Int loadPos = new Vector2Int(x, y);

                    // This chunk should be loaded
                    if (!loadedChunks.ContainsKey(loadPos))
                    {
                        Debug.Log("Loading pos- x: " + x + " y: " + y);
                        LoadChunk(loadPos);
                    }
                }
            }
        }
        // For now every chunk is just stored at loadedChunks, eventually I gotta implement the unload chunks funcionality
    }

    void LoadChunk(Vector2Int pos)
    {
        if (!existingChunks.ContainsKey(pos)) {
            GenerateChunk(pos);
        }

        loadedChunks.Add(pos, existingChunks[pos]);
    }

    void GenerateChunk(Vector2Int pos)
    {
        // Instantiate the Chunk Prefab
        GameObject newChunk = Instantiate(chunkPrefab, new Vector3(pos.x * CHUNK_SIZE, 0, pos.y * CHUNK_SIZE), Quaternion.identity);

        if (!newChunk.TryGetComponent<Chunk>(out var chunkScript)) {
            Debug.Log("Chunk Script not found at prefab, creation script broken.");
            return;
        }

        chunkScript.CreateChunk(pos, CHUNK_SIZE);
        chunkScript.GenerateChunk();

        existingChunks.Add(pos, chunkScript);
    }
}
