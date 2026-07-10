using Godot;

[GlobalClass]
public partial class NetNode : Node
{

	public override void _Ready()
	{
        GameNetwork.Instance.OnNetworkReady += OnNetworkReady;
        GameNetwork.Instance.OnNetworkDisconnected += OnNetworkDisconnected;

        if (GameNetwork.Instance.IsConnectedToServer)
        {
            OnNetworkReady();
        }

        else
        {
            OnNetworkNotReady();
        }

    }

    protected virtual void OnNetworkDisconnected()
    {

    }

    protected virtual void OnNetworkNotReady()
    {

    }

    protected virtual void OnNetworkReady()
    {

    }
}
