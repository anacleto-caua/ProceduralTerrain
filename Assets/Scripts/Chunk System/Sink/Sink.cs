public class Sink : ProtoSink
{
    // Weight measures how big and influent this Sink is
    public float weight;

    public Sink(PseudoSink psk, float weight) : base()
    {
        this.u_x = psk.u_x;
        this.u_y = psk.u_y;
        this.i = psk.i;
        this.j = psk.j;

        this.weight = weight;
    }
}