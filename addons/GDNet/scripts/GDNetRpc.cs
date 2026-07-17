using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Reflection;
using System.Buffers;

[GlobalClass]
public partial class GDNetRpc : GDNetCommunicator
{
	public GDNetBuffer Buffer = new();

	private System.Collections.Generic.Dictionary<string, Callable> _methodBinds = new();
	private System.Collections.Generic.Dictionary<string, Delegate> _delegateBinds = new();

    private System.Collections.Generic.Dictionary<string, ushort> _rpcIdRegistry = new();
	private System.Collections.Generic.Dictionary<ushort, string> _rpcNameRegistry = new();
	private System.Collections.Generic.Dictionary<string, Dictionary<string, Variant>> _cfgRegistry = new();

	private ushort _nextRpcID = 0;

	public int Authority = GDNet.ServerID;
	private int _remoteSender = 0;

	public GDNetRpc()
	{

	}

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
            ReceivedRpcPacketLocally(GDNet.ServerID, target, GDNet.ServerID, method, type, args);
			return;
		}

		Dictionary<string, Variant> cfg = _cfgRegistry[method];
		if (!ValidateWithError(GDNet.uniqueID, Authority, cfg))
			return;

		Buffer.Clear();
        Buffer.WriteByte((byte)type);

		if (type == RpcType.Target)
		{
			Buffer.WriteInt32(target);
		}

		Buffer.WriteLong(_rpcIdRegistry[method]);
		Buffer.WriteUInt8((byte)args.Length);

		foreach (object variant in args)
		{
			Buffer.Write(variant);
		}

		UpdateModeAndChannel(cfg);
		SendToServer(Buffer.GetBytes());
	}

    private void ReceivedRpcPacketLocally(int fromPeer, int target, int sender, string method, RpcType type, object[] args)
    {
		bool isServer = GDNet.isServer;
    }

    public override void ReceivedBytes(long peerId, byte[] data)
	{
        bool isServer = GDNet.isServer;

        Buffer.SetBytes(data);
		Buffer.Seek(0);

		RpcType type = (RpcType)Buffer.ReadByte();
		int targetId = -1;
        int senderId = GDNet.ServerID;

		if (isServer)
		{
			if (type == RpcType.Target)
                targetId = (int)Buffer.ReadLong();
		}
		else
			senderId = (int)Buffer.ReadLong();
		
		ushort rpcId = (ushort)Buffer.ReadLong();

		if (!_rpcNameRegistry.TryGetValue(rpcId, out string method))
		{
			GD.PushError($"Cant find {rpcId} rpcId in registry");
			return;
		}

		byte argsLength = Buffer.ReadUInt8();

        object[] args = ArrayPool<object>.Shared.Rent(argsLength);

        try
        {
            for (byte i = 0; i < argsLength; i++)
            {
                args[i] = Buffer.Read();
            }

            ReceivedRpcPacketLocally((int)peerId, targetId, senderId, method, type, args);
        }

        finally
        {
            ArrayPool<object>.Shared.Return(args, clearArray: true);
        }


    }

	private void TryCallMethodLocal(string method, object[] args)
	{
		if (_delegateBinds.TryGetValue(method, out var @delegate))
		{
			@delegate.DynamicInvoke(args);
		}
	}

	public void BindDelegate(string rpcMethod, Delegate @delegate)
	{
		_delegateBinds[rpcMethod] = @delegate;
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

            var paramTypes = method.GetParameters().Select(p => p.ParameterType).ToArray();

            Type delegateType;
            if (method.ReturnType == typeof(void))
            {
                delegateType = paramTypes.Length == 0
                    ? typeof(Action)
                    : System.Linq.Expressions.Expression.GetDelegateType(
                        paramTypes.Concat(new[] { typeof(void) }).ToArray()
                    );
            }
            else
            {
                var types = paramTypes.Concat(new[] { method.ReturnType }).ToArray();
                delegateType = System.Linq.Expressions.Expression.GetDelegateType(types);
            }

            var @delegate = method.CreateDelegate(delegateType, target);

            Register(method.Name, new Dictionary<string, Variant>
            {
                ["channel"] = attr.Channel,
                ["mode"] = GDNetRpcAttribute.ModeToString(attr.Mode),
                ["permission"] = GDNetRpcAttribute.PermissionToString(attr.Permission)
            });

            BindDelegate(method.Name, @delegate);
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
