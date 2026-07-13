using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[GlobalClass]
public partial class GDNetCommunicator : RefCounted, IDisposable
{

	private ulong _networkID = 0;
	public MultiplayerPeer.TransferModeEnum Mode = MultiplayerPeer.TransferModeEnum.Reliable;
	public int Channel = 0;

	[Signal] public delegate void OnBytesReceivedEventHandler(int peer, byte[] data);

	private static Dictionary<ulong, ulong> _registry = new();

	protected int[] Observers;
	protected bool _observersEnabled = false;

    protected MemoryStream _stream;
    protected BinaryWriter _writer;
    protected BinaryReader _reader;

    public GDNetCommunicator()
    {
        _stream = new MemoryStream();
        _writer = new BinaryWriter(_stream);
        _reader = new BinaryReader(_stream);
        SetNetworkID(_networkID);
    }

    protected override void Dispose(bool disposing)
    {
        _writer?.Dispose();
        _reader?.Dispose();
        _stream?.Dispose();
        base.Dispose(disposing);
    }

    public int[] GetObservers()
	{  return Observers; }

	public void SetObservers(int[] observers)
	{
		Observers = observers;
	}

	public void SetObserversEnabled(bool enabled)
	{
		_observersEnabled = enabled;
	}

	public bool IsObserversEnabled()
	{
		return _observersEnabled;
	}

	public static GDNetCommunicator FindByNetworkID(ulong id)
	{
		GodotObject obj = InstanceFromId(_registry.GetValueOrDefault(id));
		if (obj != null)
		{
			return (GDNetCommunicator)obj;
		}

		return null;
	}

	protected virtual string GetHashSalt()
	{
		return "Communicator";
	}

    public static MultiplayerPeer.TransferModeEnum StringToTransferMode(string str)
    {
        switch (str)
        {
            case "reliable":
                return MultiplayerPeer.TransferModeEnum.Reliable;
            case "unreliable_ordered":
                return MultiplayerPeer.TransferModeEnum.UnreliableOrdered;
        }

        return MultiplayerPeer.TransferModeEnum.Unreliable;
    }

    public void SynchronizeNodeNetworkID(Node sceneTreeNode)
	{
		if (sceneTreeNode.IsInsideTree())
		{
			string path = sceneTreeNode.GetPath().ToString();
			string salt = $"{"GDNetNodeNetIDSalt"}_{path}_{GetHashSalt()}";
            SetNetworkID(GDNet.HashString64(salt));
        }

		else
		{
			GD.PushError($"{sceneTreeNode} Node must be in SceneTree!");
		}
	}

    public void SynchronizeResourceNetworkID(Resource diskResource)
    {	
        if (diskResource.ResourcePath != "")
        {
            string path = diskResource.ResourcePath;
            string salt = $"{"GDNetResourceNetIDSalt"}_{path}_{GetHashSalt()}";
            SetNetworkID(GDNet.HashString64(salt));
        }

        else
        {
            GD.PushError($"{diskResource} Resource must be saved on disk!");
        }
    }

	public void SynchronizeNetworkIDByUniqueName(string uniqueName)
	{
        string salt = $"{"GDNetUniqueNameNetIDSalt"}_{uniqueName}_{GetHashSalt()}";
		SetNetworkID(GDNet.HashString64(uniqueName));
    }

    public void SynchronizeNetworkIDByUniqueID(long id)
    {
        string salt = $"{"GDNetUniqueIDSalt"}_{id.ToString()}_{GetHashSalt()}";
        SetNetworkID(GDNet.HashString64(salt));
    }

	public void SetNetworkID(ulong id)
	{
		_registry.Remove(_networkID);
		_networkID = id;
		_registry[id] = this.GetInstanceId();
	}

	public ulong GetNetworkID()
	{
		return _networkID;
	}

	public void SendToServer(byte[] data)
	{
		GDNetMessageProcessor.Instance.___QueueCommunicator(_networkID, GDNet.ServerID, data, Mode, Channel);
	}

    public void SendTo(int peer, byte[] data)
    {
        GDNetMessageProcessor.Instance.___QueueCommunicator(_networkID, peer, data, Mode, Channel);
    }

    public void SendToAll(byte[] data)
    {
		if (_observersEnabled)
		{
			foreach (int observer in Observers)
			{
				GDNetMessageProcessor.Instance.___QueueCommunicator(_networkID, observer, data, Mode, Channel);
			}

            return;
		}

		foreach(int pid in GDNet.Instance.Multiplayer.GetPeers())
			GDNetMessageProcessor.Instance.___QueueCommunicator(_networkID, pid, data, Mode, Channel);
    }

    public virtual void ReceivedBytes(long peerId, byte[] data)
	{
		EmitSignal(SignalName.OnBytesReceived, peerId, data);
	}

	private void CleanUp()
	{
		_registry.Remove(_networkID);
	}

    public override void _Notification(int what)
    {
		if (what == NotificationPredelete)
		{
			CleanUp();
		}
    }

    private void OwnerNodeSynchronizeID(Node node)
    {
        if (node == null)
            return;

        SynchronizeNodeNetworkID(node);
    }

    public void BindOwnerAsResource(Resource resource)
    {
        SynchronizeResourceNetworkID(resource);
    }

    public void BindOwnerAsNode(Node node)
    {
        if (node.IsInsideTree())
            SynchronizeNodeNetworkID(node);

        node.TreeEntered += () => OwnerNodeSynchronizeID(node);
        node.Renamed += () => OwnerNodeSynchronizeID(node);
    }

}
