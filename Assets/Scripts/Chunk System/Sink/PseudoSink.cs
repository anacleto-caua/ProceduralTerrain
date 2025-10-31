public class PseudoSink : ProtoSink
{
    // Track if the sink was used to make a world sink already
    public bool isAvailable = true;

    public PseudoSink(int u_x, int u_y, int i, int j, SinkType type) : base(u_x, u_y, i, j, type)
    {

    }
}