using UnityEngine;

public class Chunk : MonoBehaviour
{
    private const float UNITY_DEFAULT_PLANE_SCALE = 10f;

    public GameObject plane;
    

    private Vector2Int pos;

    private GameObject redSphere;

    private int CHUNK_SIZE = 50;

    public void CreateChunk(Vector2Int pos, int chunkSize)
    {
        this.pos = pos;
        this.CHUNK_SIZE = chunkSize;

        this.gameObject.name = "Chunk-" + pos.x + "-" + pos.y;
        this.plane.transform.localScale = Vector3.one * CHUNK_SIZE / UNITY_DEFAULT_PLANE_SCALE;
        //SpawnRedSphere();
    }

    public void GenerateChunk()
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
