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
        if (GameNetwork.Instance.IsServer)
        {
            foreach (var node in Targets)
            {
                node.QueueFree();
            }
        }

    }
}
