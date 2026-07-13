using Godot;
using Godot.Collections;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[GlobalClass]
public partial class GDNetRpc : GDNetCommunicator, IDisposable
{
	public GDNetBuffer Buffer = new();

	private System.Collections.Generic.Dictionary<string, Callable> _methodBinds = new();

	private System.Collections.Generic.Dictionary<string, ushort> _rpcIdRegistry = new();
	private System.Collections.Generic.Dictionary<ushort, string> _rpcNameRegistry = new();
	private System.Collections.Generic.Dictionary<string, Dictionary<string, Variant>> _cfgRegistry = new();

	private ushort _nextRpcID = 0;

	private MemoryStream _stream;
	private BinaryWriter _writer;
	private BinaryReader _reader;

	public int Authority = GDNet.ServerID;
	private int _remoteSender = 0;
	public int GetRemoteSender()
	{
		return _remoteSender;
	}

	internal enum RpcType : byte
	{
		All,
		Target,
		OnServer,
		Async,
	}

	public void SetAuthority(int value)
	{ this.Authority = value; }

	public int GetAuthority() { return Authority; }

	public bool IsAuthority()
	{
		return Authority == GDNet.uniqueID;
	}

	public GDNetRpc()
	{
		_stream = new MemoryStream();
		_writer = new BinaryWriter(_stream);
		_reader = new BinaryReader(_stream);
	}

	protected override void Dispose(bool disposing)
	{
		_writer?.Dispose();
		_reader?.Dispose();
		_stream?.Dispose();
		base.Dispose(disposing);
	}

	protected override string GetHashSalt()
	{
		return "RPC";
	}

	public void Invoke(string method, params Variant[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, args);
	}
	public void InvokeOn(int id, string method, params Variant[] args)
	{
		InvokeByTypeInternal(id, method, RpcType.Target, args);
	}

	public void InvokeOnServer(string method, params Variant[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.OnServer, args);
	}

	public void Invoke(string method, Godot.Collections.Array args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, args.ToArray());
	}
	public void InvokeOn(int id, string method, Godot.Collections.Array args)
	{
		InvokeByTypeInternal(id, method, RpcType.Target, args.ToArray());
	}

	public void InvokeOnServer(string method, Godot.Collections.Array args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.OnServer, args.ToArray());
	}

	private void InvokeByTypeInternal(int target, string method, RpcType type, Variant[] args)
	{
		if (!_rpcIdRegistry.ContainsKey(method))
		{
			if (GDNet.Debug)
				GD.PushError($"YOUR ID {GDNet.uniqueID}: Cant find {method} method in registry.");
			return;
		}
		
		if (GDNet.isServer)
		{
			ReceiveFromPeerType(GDNet.uniqueID, target, method, type, args);
			return;
		}

		Dictionary<string, Variant> cfg = _cfgRegistry[method];
		if (!ValidateWithError(GDNet.uniqueID, Authority, cfg))
			return;

		_stream.SetLength(0);
		_stream.Position = 0;

		_writer.Write((byte)type);

		if (type == RpcType.Target)
		{
			_writer.Write(target);
		}

		_writer.Write(_rpcIdRegistry[method]);
		_writer.Write((byte)args.Length);

		Buffer.Clear();

		foreach (Variant variant in args)
		{
			Buffer.Write(variant);
		}

		_writer.Write(Buffer.GetBytes());

		UpdateModeAndChannel(cfg);
		SendToServer(_stream.ToArray());
	}

	private byte[] ServerSerializeRpc(int target, string method, RpcType type, Variant[] args)
	{
		_stream.SetLength(0);
		_stream.Position = 0;

		_writer.Write((byte)type);

		if (type == RpcType.Target)
		{
			_writer.Write(target);
		}

		_writer.Write(_rpcIdRegistry[method]);
		_writer.Write((byte)args.Length);

		Buffer.Clear();

		foreach (Variant variant in args)
		{
			Buffer.Write(variant);
		}

		_writer.Write(Buffer.GetBytes());

		return _stream.ToArray();
	}

	public override void ReceivedBytes(long peerId, byte[] data)
	{
		_stream.SetLength(0);
		_stream.Position = 0;
		_stream.Write(data, 0, data.Length);
		_stream.Position = 0;

		RpcType type = (RpcType)_reader.ReadByte();
		int targetId = -1;

		if (type == RpcType.Target)
		{
			targetId = _reader.ReadInt32();
		}

		ushort rpcId = _reader.ReadUInt16();
		byte argsLength = _reader.ReadByte();

		Variant[] args = new Variant[argsLength];

		if (argsLength > 0)
		{
			byte[] argsBytes = _reader.ReadBytes((int)(_stream.Length - _stream.Position));
			Buffer.SetBytes(argsBytes);
			Buffer.Seek(0);

			for (int i = 0; i < argsLength; i++)
			{
				args[i] = Buffer.Read();
			}
		}

		if (!_rpcNameRegistry.TryGetValue(rpcId, out string method))
		{
			GD.PushError($"Cant find {rpcId} rpcId in registry");
			return;
		}

		ReceiveFromPeerType(peerId, targetId, method, type, args);
	}

	private void ReceiveFromPeerType(long peerId, int target, string method, RpcType type, Variant[] args)
	{
		bool isClientReceiver = !GDNet.isServer;

		Dictionary<string, Variant> cfg = _cfgRegistry[method];
		if (!ValidateWithError(peerId, Authority, cfg))
			return;

		if (type == RpcType.OnServer)
		{
			type = RpcType.Target;
			target = GDNet.ServerID;
		}

		_remoteSender = (int)peerId;

		switch (type)
		{
			case RpcType.All:
				TryCallMethodLocal(method, args);

				if (isClientReceiver || (_observersEnabled && Observers.Length == 0))	
					break;

				UpdateModeAndChannel(cfg);
				SendToAll(ServerSerializeRpc((int)peerId, method, type, args));

				break;

			case RpcType.Target:
				if (target == GDNet.uniqueID)
				{
					TryCallMethodLocal(method, args);
					break;
				}

				if (!isClientReceiver)
				{
					UpdateModeAndChannel(cfg);
					SendTo(target, ServerSerializeRpc((int)peerId, method, type, args));
				}

				break;

			case RpcType.Async:

				break;
			
		}

		_remoteSender = 0;
	}

	private Godot.Collections.Array _allocatedGDArgsArray = new();

	private object TryCallMethodLocal(string method, Variant[] args)
	{
		if (_methodBinds.TryGetValue(method, out var methodLocal))
		{
			_allocatedGDArgsArray.Clear();
			for (int i = 0; i < args.Length; i++)
			{
				_allocatedGDArgsArray.Add(args[i]);
			}
				
			return methodLocal.Target.Callv(method, _allocatedGDArgsArray);
		}
		return null;
	}

	public void BindMethod(string method, Callable callable)
	{
		_methodBinds[method] = callable;
	}

	private bool Validate(long peerId, int authority, Dictionary<string, Variant> cfg)
	{
		switch (cfg["permission"].As<string>())
		{
			case "authority":
				return authority == peerId;
			case "server":
				return peerId == GDNet.ServerID;
		}

		return true;
	}

	private bool ValidateWithError(long peerId, int authority, Dictionary<string, Variant> cfg)
	{
		bool validation = Validate(peerId, authority, cfg);
		if (!validation && GDNet.Debug)
			GD.PushError($"Rpc Validation Failed for {peerId} id; {authority} auth; {GetNetworkID()} networkId; {cfg}");
		return validation;
	}

	private void UpdateModeAndChannel(Dictionary<string, Variant> cfg)
	{
		Mode = StringToTransferMode(cfg["mode"].ToString());
		Channel = cfg["channel"].AsInt32();
	}

	public void Register(string method, Dictionary<string, Variant> cfg)
	{
		ParseCfgRef(cfg);
		_rpcIdRegistry[method] = _nextRpcID;
		_rpcNameRegistry[_nextRpcID] = method;
		_cfgRegistry[method] = cfg;
		_nextRpcID++;
	}

	private void ParseCfgRef(Dictionary<string, Variant> override_cfg)
	{
		if (!override_cfg.ContainsKey("permission"))
			override_cfg["permission"] = "authority";

		if (!override_cfg.ContainsKey("mode"))
			override_cfg["mode"] = "reliable";

		if (!override_cfg.ContainsKey("channel"))
			override_cfg["channel"] = 0;

	}

	public void BindOwnerAsNode(Node node)
	{
		if (node.IsInsideTree())
			SynchronizeNodeNetworkID(node);

		node.TreeEntered += () => OwnerNodeSynchronizeID(node);
		node.Renamed += () => OwnerNodeSynchronizeID(node);

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


}
