using UnityEngine;

public class Chunk
{
    Vector2Int pos;

    GameObject redSphere;

    private int CHUNK_SIZE = 50;

    public Chunk(Vector2Int pos, int chunkSize)
    {
        this.pos = pos;
        this.CHUNK_SIZE = chunkSize;
    }

    public void GenerateChunk()
    {
        SpawnRedSphere();
    }

    public void Load()
    {
        this.redSphere.SetActive(true);
    }
    public void Unload()
    {
        this.redSphere.SetActive(false);
    }

    public void SpawnRedSphere()
    {
        Vector3 spawnPos;
        spawnPos = new Vector3(pos.x * CHUNK_SIZE, 0, pos.y * CHUNK_SIZE);

        this.redSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        redSphere.name = "RedSphere";
        redSphere.transform.position = spawnPos;

        Renderer sphereRenderer = redSphere.GetComponent<Renderer>();

        if (sphereRenderer != null)
        {
            sphereRenderer.material.color = Color.red;
        }
        else
        {
            Debug.LogError("SphereRenderer not found on the newly created sphere GameObject!");
        }
    }

    void Log()
    {
        Debug.Log("Chunk-" + "x: " + pos.x + " y: " + pos.y);
    }
}
