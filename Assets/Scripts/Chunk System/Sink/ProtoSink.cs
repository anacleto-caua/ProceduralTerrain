using UnityEngine;

public abstract class ProtoSink
{
    // Universal coordinates
    public int u_x;
    public int u_y;

    // Local coords
    public int i;
    public int j;

    public SinkType type;

    public ProtoSink()
    {

    }

    public ProtoSink(int u_x, int u_y, int i, int j)
    {
        this.u_x = u_x;
        this.u_y = u_y;
        this.i = i;
        this.j = j;
    }

    public ProtoSink(int u_x, int u_y, int i, int j, SinkType type)
    {
        this.u_x = u_x;
        this.u_y = u_y;
        this.i = i;
        this.j = j;
        this.type = type;
    }

    public bool Equals(ProtoSink sink)
    {
        if (this == null || sink == null)
        {
            return false;
        }

        if (
            (this.u_x == sink.u_x) &&
            (this.u_y == sink.u_y)
            )
        {
            return true;
        }

        return false;
    }

    public double Distance(ProtoSink sink)
    {
        double distance = Mathf.Sqrt(Mathf.Pow(sink.i - this.i, 2) + Mathf.Pow(sink.i - this.i, 2));
        return distance;
    }

    public string ToString()
    {
        return "Sink: " + " type: " + this.type.ToString() + " x: " + u_x + " y: " + u_y;
    }
}