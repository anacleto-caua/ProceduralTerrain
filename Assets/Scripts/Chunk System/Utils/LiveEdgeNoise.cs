public class LiveEdgeNoise : FastNoiseLite
{
    private static LiveEdgeNoise _instance;
    private static readonly object _lock = new object();

    private LiveEdgeNoise()
    {
        this.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        this.SetFrequency(0.042f);

        this.SetFractalType(FastNoiseLite.FractalType.FBm);
        this.SetFractalOctaves(3);
        this.SetFractalLacunarity(2.0f);
        this.SetFractalGain(0.5f);
        this.SetFractalWeightedStrength(0.14f);
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
                    _instance = new LiveEdgeNoise();
                }
                return _instance;
            }
        }
    }
    public static new float GetNoise(float x, float y)
    {
        // Noise goes from 1 to -1 Unity's terrain need it between 0 and 1
        return (Instance.GetNoise(x, y) + 1) / 2f;
    }

    public static new void SetSeed(int seed)
    {
        Instance.SetSeed(seed);
    }
}