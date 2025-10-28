public enum SinkType
{
    SinkOnX,
    SinkOnZ,
    MajorSink
}

public class Sink
{
    // Universal coordinates
    public int u_x;
    public int u_y;

    // Local coords
    public int i;
    public int j;

    // Track if the sink was used to make a world sink already
    public bool isAvailable = true;

    public SinkType type;

    public Sink(int u_x, int u_y, int i, int j)
    {
        this.u_x = u_x;
        this.u_y = u_y;
        this.i = i;
        this.j = j;
    }

    public Sink(int u_x, int u_y, int i, int j, SinkType type)
    {
        this.u_x = u_x;
        this.u_y = u_y;
        this.i = i;
        this.j = j;
        this.type = type;
    }

    public bool Equals(Sink sink)
    {
        if (this == null || sink == null)
        {
            return false;
        }

        if(
            (this.u_x == sink.u_x) && 
            (this.u_y == sink.u_y)
            )
        {
            return true;
        }

        return false;
    }

    public string ToString()
    {
        return "Sink: " + " type: " + this.type.ToString() + " x: " + u_x + " y: " + u_y;
    }
}