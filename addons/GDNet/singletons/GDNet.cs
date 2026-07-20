using GDNetExtensions;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

public partial class GDNet : Node
{
	public static GDNet Instance = null;

	private StreamPeerBuffer _buffer = new();

	public static bool Debug = true;

	public const int ServerID = 1;

	[Signal] public delegate void OnNetworkPeerConnectionStatusChangedEventHandler(MultiplayerPeer.ConnectionStatus status);
	[Signal] public delegate void OnNetworkReadyEventHandler();
	[Signal] public delegate void OnNetworkConnectingEventHandler();
	[Signal] public delegate void OnNetworkDisconnectedEventHandler();
	[Signal] public delegate void OnNetworkPeerConnectedEventHandler(int peer);
	[Signal] public delegate void OnNetworkPeerDisconnectedEventHandler(int peer);


	[Export] private Timer _tickTimer;

	public event Action<PacketType, byte[], long> OnNetworkPacket;

	public event Action<int> OnNetworkPacketSizeSent;
	public event Action<int> OnNetworkPacketSizeReceived;

    private readonly MemoryStream _stream = new();
	private readonly BinaryWriter _writer;
	private readonly BinaryReader _reader;

	public GDNet()
	{
        Instance = this;
        _writer = new BinaryWriter(_stream);
		_reader = new BinaryReader(_stream);
	}
	public enum PacketType
	{
		RpcRequest,
		RpcReceive,

		CommunicationMessage,
	}

	private MultiplayerPeer.ConnectionStatus _connectionStatus = MultiplayerPeer.ConnectionStatus.Disconnected;
	public MultiplayerPeer.ConnectionStatus ConnectionStatus => _connectionStatus;
	public static bool isConnectedToServer = false;
	public static bool isServer = true;
	public static int uniqueID = ServerID;

	[Export] private GDNetOptimizedSend _optimizedSend;
	[Export] private GDNetMessageProcessor _messageProcessor;

	public const string MetaHashID = "GDNetID";
	public const string HashIDSalt = "GDNetHash";
	public const string HashIDSaltResource = "GDNetHashResource";

	private static long _NextUniqueID = 0;

	private ConcurrentDictionary<ulong, ulong> _ObjectsByHashID = new();
	private ConcurrentDictionary<ulong, ulong> _HashIDByObjects = new();



	public bool IsConnectedToServer()
	{
		return isConnectedToServer;
	}

	public bool IsServer()
	{
		return isServer;
	}

	public int GetUniqueID()
	{
		return uniqueID;
	}
	public static long GenerateUniqueID()
	{
		_NextUniqueID++;
		return _NextUniqueID;
	}

	public static ulong HashString64(string input)
	{
		string combined = input + "GDNetSalt";

		int h1_int = GD.Hash(combined);
		int h2_int = GD.Hash(combined + "_salt_" + input.Length);

		uint h1 = (uint)h1_int;
		uint h2 = (uint)h2_int;

		return ((ulong)h1 << 32) | h2;
	}

	public override void _EnterTree()
	{
		Instance = this;
	}

	public override void _Ready()
	{
		Debug = OS.IsDebugBuild();

		_tickTimer.Timeout += UpdateNetworkStateTick;
		_tickTimer.Start();

		_messageProcessor.SingletonReady();

	}

	public override void _PhysicsProcess(double delta)
	{
		_optimizedSend.ProcessAll();
		_messageProcessor.ProcessAll();
	}

	private void OnTryCollectGarbage()
	{
		
	}

	private void UpdateNetworkStateTick()
	{
		MultiplayerPeer peer = Multiplayer.MultiplayerPeer;
		if (peer == null)
			return;

		if (peer is OfflineMultiplayerPeer)
		{
			return;
		}

		if (peer.GetConnectionStatus() != _connectionStatus)
		{
			_connectionStatus = peer.GetConnectionStatus();
			ConnectionStatusChanged();
			EmitSignal(SignalName.OnNetworkPeerConnectionStatusChanged, ((int)_connectionStatus));
		}
	}

	private void ConnectionStatusChanged()
	{
		isServer = Multiplayer.IsServer();
		isConnectedToServer = _connectionStatus == MultiplayerPeer.ConnectionStatus.Connected;
		uniqueID = Multiplayer.GetUniqueId();

		switch (_connectionStatus)
		{
			case MultiplayerPeer.ConnectionStatus.Disconnected:
				EmitSignal(SignalName.OnNetworkDisconnected);
				break;
			case MultiplayerPeer.ConnectionStatus.Connecting:
				EmitSignal(SignalName.OnNetworkConnecting);
				break;
			case MultiplayerPeer.ConnectionStatus.Connected:
				EmitSignal(SignalName.OnNetworkReady);
				break;
		}
	}

	public void Setup(SceneMultiplayer api)
	{
		GetTree().SetMultiplayer(api);
		_optimizedSend.Setup(api);
		_optimizedSend.MultiplayerPeerPacket += OnOptimizedPeerPacket;
		_optimizedSend.MultiplayerPeerPacketRawSent += OnOptimizedPeerRawSent;
		_optimizedSend.MultiplayerPeerPacketRawReceived += OnOptimizedPeerRawReceived;

        api.PeerConnected += OnApiPeerConnected;
		api.PeerDisconnected += OnApiPeerDisconnected;

	}

    private void OnApiPeerConnected(long id)
	{
		EmitSignal(SignalName.OnNetworkPeerConnected, (int)id);
	}

	private void OnApiPeerDisconnected(long id)
	{
		EmitSignal(SignalName.OnNetworkPeerDisconnected, (int)id);
	}

	public int[] GetConnectedPeers()
	{
		return Multiplayer.GetPeers();
	}

	public void Setup()
	{
		Setup(new());
	}

	public void SendPacket(PacketType type, byte[] bytes, int peer, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_stream.Position = 0;
		_stream.SetLength(0);

		_writer.Write((byte)type);
		_writer.Write(bytes);

		var data = _stream.ToArray();
        _optimizedSend.MultiplayerSendBytes(data, peer, mode, channel);
        
    }

	private void OnOptimizedPeerPacket(long id, byte[] bytes)
	{
        _stream.Position = 0;
		_stream.SetLength(0);
		_stream.Write(bytes, 0, bytes.Length);
		_stream.Position = 0;

		var type = (PacketType)_reader.ReadByte();
		var data = _reader.ReadBytes((int)(_stream.Length - 1));

		OnNetworkPacket?.Invoke(type, data, id);
		
	}
    private void OnOptimizedPeerRawReceived(long id, byte[] bytes)
    {
        OnNetworkPacketSizeReceived?.Invoke(bytes.Length);
    }

    private void OnOptimizedPeerRawSent(long id, byte[] bytes)
    {
        OnNetworkPacketSizeSent?.Invoke(bytes.Length);
    }



}
