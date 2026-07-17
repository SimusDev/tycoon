using Godot;
using Godot.Collections;
using System;
using System.Reflection;

[GlobalClass]
public partial class GDNetRpc : GDNetCommunicator
{
	public GDNetBuffer Buffer = new();

	private System.Collections.Generic.Dictionary<string, Callable> _methodBinds = new();
	private System.Collections.Generic.Dictionary<string, MethodInfo> _methodInfoBinds = new();

    private System.Collections.Generic.Dictionary<string, ushort> _rpcIdRegistry = new();
	private System.Collections.Generic.Dictionary<ushort, string> _rpcNameRegistry = new();
	private System.Collections.Generic.Dictionary<string, Dictionary<string, Variant>> _cfgRegistry = new();

	private ushort _nextRpcID = 0;

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

	protected override string GetHashSalt()
	{
		return "RPC";
	}

	public void Invoke(string method, params object[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, args);
	}
	public void InvokeOn(int id, string method, params object[] args)
	{
		InvokeByTypeInternal(id, method, RpcType.Target, args);
	}

	public void InvokeOnServer(string method, params object[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.OnServer, args);
	}

    public void Invoke(Delegate method, params object[] args)
    {
        InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.All, args);
    }
    public void InvokeOn(int id, Delegate method, params object[] args)
    {
        InvokeByTypeInternal(id, method.Method.Name, RpcType.Target, args);
    }

    public void InvokeOnServer(Delegate method, params object[] args)
    {
        InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.OnServer, args);
    }

    private void InvokeByTypeInternal(int target, string method, RpcType type, object[] args)
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

		Buffer.Clear();
        Buffer.Write((byte)type);

		if (type == RpcType.Target)
		{
			Buffer.WriteInt32(target);
		}

		Buffer.WriteUInt16(_rpcIdRegistry[method]);
		Buffer.WriteUInt8((byte)args.Length);

		foreach (object variant in args)
		{
			Buffer.Write(variant);
		}

		UpdateModeAndChannel(cfg);
		SendToServer(Buffer.GetBytes());
	}

	private byte[] ServerSerializeRpc(int target, string method, RpcType type, object[] args)
	{
        Buffer.Clear();
        Buffer.Write((byte)type);

        if (type == RpcType.Target)
        {
            Buffer.WriteInt32(target);
        }

        Buffer.WriteUInt16(_rpcIdRegistry[method]);
        Buffer.WriteUInt8((byte)args.Length);

        foreach (object variant in args)
        {
            Buffer.Write(variant);
        }

        return Buffer.GetBytes();
    }

	public override void ReceivedBytes(long peerId, byte[] data)
	{
		Buffer.Clear();
		Buffer.SetBytes(data);

		RpcType type = (RpcType)Buffer.ReadUInt8();
		int targetId = -1;

		if (type == RpcType.Target)
		{
			targetId = Buffer.ReadInt32();
		}

		ushort rpcId = Buffer.ReadUInt16();
		byte argsLength = Buffer.ReadUInt8();

		object[] args = new object[argsLength];

		if (argsLength > 0)
		{
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

	private void ReceiveFromPeerType(long peerId, int target, string method, RpcType type, object[] args)
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

	private void TryCallMethodLocal(string method, object[] args)
	{

	}

	public void BindAll(object target)
	{
        var type = target.GetType();
        var methods = type.GetMethods(
            BindingFlags.Public |
            BindingFlags.NonPublic |
            BindingFlags.Instance
        );

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<GDNetRpcAttribute>();
            if (attr == null) continue;

			Dictionary<string, Variant> cfg = new();
			cfg["channel"] = attr.Channel;
			cfg["mode"] = GDNetRpcAttribute.ModeToString(attr.Mode);
			cfg["permission"] = GDNetRpcAttribute.PermissionToString(attr.Permission);
			Register(method.Name, cfg);
			_methodInfoBinds[method.Name] = method;
        }
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
}
