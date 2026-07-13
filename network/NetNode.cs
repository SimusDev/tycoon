using Godot;

[GlobalClass]
public partial class NetNode : Node
{

	public override void _Ready()
	{
        GDNet.Instance.OnNetworkReady += OnNetworkReady;
        GDNet.Instance.OnNetworkDisconnected += OnNetworkDisconnected;

        if (GDNet.Instance.IsConnectedToServer())
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
