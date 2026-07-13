using Godot;

[GlobalClass]
public partial class NetNodeFreeNodesIfServer : NetNode
{
    [Export] private Godot.Collections.Array<Node> Targets;

    protected override void OnNetworkDisconnected()
    {

    }

    protected override void OnNetworkReady()
    {
        if (GDNet.isServer)
        {
            foreach (var node in Targets)
            {
                if (!IsInstanceValid(node)) { continue; }
                node.QueueFree();
            }
        }

    }
}
