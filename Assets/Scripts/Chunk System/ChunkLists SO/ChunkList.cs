using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ChunkList", menuName = "Scriptable Objects/ChunkList")]
public class ChunkList : ScriptableObject, IEnumerable<Chunk>
{
    private Dictionary<Vector2Int, Chunk> chunks = new Dictionary<Vector2Int, Chunk>();

    public bool ContainsKey(Vector2Int id)
    {
        if(chunks.ContainsKey(id))
        {
            return true;
        }

        return false;
    }

    public void Add(Vector2Int pos, Chunk chunk)
    {
        if (!chunks.ContainsKey(pos))
        {
            chunks.Add(pos, chunk);
        }
        else
        {
            AnaLogger.Log("Chunk already exists at position: " + pos);
        }
    }

    public void Remove(Vector2Int pos)
    {
        if (chunks.ContainsKey(pos))
        {
            chunks.Remove(pos);
        }
        else
        {
            AnaLogger.Log("Chunk doesn't exists at position: " + pos);
        }
    }

    public Chunk Get(Vector2Int pos)
    {
        return chunks[pos];
    }

    public void DestroyList()
    {
        foreach(Chunk chunk in chunks.Values)
        {
            if(chunk == null) continue; // In case the chunk was already destroyed somewhere else

            #if UNITY_EDITOR
                DestroyImmediate(chunk.gameObject);
            #else
                Destroy(chunk.gameObject);
            #endif
        }
        chunks.Clear();
    }

    public void Dump()
    {
        Debug.Log("Dumping ChunkList!");

        foreach (KeyValuePair<Vector2Int, Chunk> entry in chunks)
        {
            Debug.Log("Chunk at pos: " + entry.Key + " is " + (entry.Value == null ? "null" : "not null"));
        }
    }

    public bool TryGetChunk(Vector2Int pos, out Chunk chunk)
    {
        return chunks.TryGetValue(pos, out chunk);
    }

    // IEnumerator utils
    public IEnumerator<Chunk> GetEnumerator()
    {
        // Simply return the enumerator for the dictionary's values
        return chunks.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        // This just calls the generic version above
        return GetEnumerator();
    }
}
