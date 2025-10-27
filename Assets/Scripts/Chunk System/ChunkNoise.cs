public class ChunkNoise : FastNoiseLite
{
    private static ChunkNoise _instance;
    private static readonly object _lock = new object();

    private ChunkNoise()
    {
        this.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        this.SetFrequency(0.010f);

        this.SetFractalType(FastNoiseLite.FractalType.FBm);
        this.SetFractalOctaves(3);
        this.SetFractalLacunarity(2.0f);
        this.SetFractalGain(0.5f);
    }

    public static FastNoiseLite Instance
    {
        get
        {
            // Thread safe lock
            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = new ChunkNoise();
                }
                return _instance;
            }
        }
    }

    public static float GetNoise(int x, int y)
    {
        return Instance.GetNoise(x, y);
    }

    public static void SetSeed(int seed)
    {
        Instance.SetSeed(seed);
    }
}