using Godot;
using Godot.Collections;
using System;
using System.Buffers;
using System.Linq;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

[GlobalClass]
public partial class GDNetRpc : GDNetCommunicator
{
	private GDNetBuffer Buffer = new();
	private GDNetBuffer BufferLocal = new();

	private System.Collections.Generic.Dictionary<string, Callable> _methodBinds = new();
	private System.Collections.Generic.Dictionary<string, Delegate> _delegateBinds = new();

	private System.Collections.Generic.Dictionary<string, ushort> _rpcIdRegistry = new();
	private System.Collections.Generic.Dictionary<ushort, string> _rpcNameRegistry = new();
	private System.Collections.Generic.Dictionary<string, Dictionary<string, Variant>> _cfgRegistry = new();

	private ushort _nextRpcID = 0;

	public int Authority = GDNet.ServerID;
	private int _remoteSender = 0;

	private static object[] _tempArgs = new object[4];

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
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, args, args.Length);
	}

	public void Invoke<T1>(string method, T1 arg)
	{
		_tempArgs[0] = arg;
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, _tempArgs, 1);
	}

	public void Invoke<T1, T2>(string method, T1 arg1, T2 arg2)
	{
		_tempArgs[0] = arg1;
		_tempArgs[1] = arg2;
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, _tempArgs, 2);
	}

	public void Invoke<T1, T2, T3>(string method, T1 arg1, T2 arg2, T3 arg3)
	{
		_tempArgs[0] = arg1;
		_tempArgs[1] = arg2;
		_tempArgs[2] = arg3;
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, _tempArgs, 3);
	}

	public void Invoke<T1, T2, T3, T4>(string method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		_tempArgs[0] = arg1;
		_tempArgs[1] = arg2;
		_tempArgs[2] = arg3;
		_tempArgs[3] = arg4;
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.All, _tempArgs, 4);
	}

	public void InvokeOn(int id, string method, params object[] args)
	{
		InvokeByTypeInternal(id, method, RpcType.Target, args, args.Length);
	}

	public void InvokeOnServer(string method, params object[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method, RpcType.OnServer, args, args.Length);
	}

	public void Invoke(Delegate method, params object[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.All, args, args.Length);
	}

	public void Invoke<T1>(Delegate method, T1 arg)
	{
		_tempArgs[0] = arg;
		InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.All, _tempArgs, 1);
	}

	public void Invoke<T1, T2>(Delegate method, T1 arg1, T2 arg2)
	{
		_tempArgs[0] = arg1;
		_tempArgs[1] = arg2;
		InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.All, _tempArgs, 2);
	}
	public void Invoke<T1, T2, T3>(Delegate method, T1 arg1, T2 arg2, T3 arg3)
	{
		_tempArgs[0] = arg1;
		_tempArgs[1] = arg2;
		_tempArgs[2] = arg3;
		InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.All, _tempArgs, 3);
	}

	public void Invoke<T1, T2, T3, T4>(Delegate method, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		_tempArgs[0] = arg1;
		_tempArgs[1] = arg2;
		_tempArgs[2] = arg3;
		_tempArgs[3] = arg4;
		InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.All, _tempArgs, 4);
	}

	public void InvokeOn(int id, Delegate method, params object[] args)
	{
		InvokeByTypeInternal(id, method.Method.Name, RpcType.Target, args, args.Length);
	}

	public void InvokeOnServer(Delegate method, params object[] args)
	{
		InvokeByTypeInternal(GDNet.ServerID, method.Method.Name, RpcType.OnServer, args, args.Length);
	}

	private void InvokeByTypeInternal(int target, string method, RpcType type, object[] args, int argsSize)
	{
		if (!_rpcIdRegistry.ContainsKey(method))
		{
			if (GDNet.Debug)
				GD.PushError($"YOUR ID {GDNet.uniqueID}: Cant find {method} method in registry.");
			return;
		}
		
		if (GDNet.isServer)
		{
			ServerProcessRpc(GDNet.ServerID, target, method, type, args, argsSize);
			return;
		}

		Dictionary<string, Variant> cfg = _cfgRegistry[method];
		if (!ValidateWithError(GDNet.uniqueID, Authority, cfg))
			return;

		Buffer.Clear();
		ClientSerializeRpcBuffer(type, target, method, args, argsSize);
		UpdateModeAndChannel(cfg);
		SendToServer(Buffer.GetBytes());
		
	}

	private void ServerProcessRpc(long fromPeer, int target, string method, RpcType type, object[] args, int argsSize)
	{
		Dictionary<string, Variant> cfg = _cfgRegistry[method];
		if (!ValidateWithError(GDNet.uniqueID, Authority, cfg))
			return;

		_remoteSender = (int)fromPeer;

		if (type == RpcType.Target && target == GDNet.ServerID)
			type = RpcType.OnServer;

		Buffer.Clear();

		switch (type)
		{
			case RpcType.All:
				if (fromPeer != GDNet.ServerID)
					TryCallMethodLocal(method, args);

				if (_observersEnabled && Observers.Length == 0)
					break;

				ServerSerializeRpcBuffer(method, _remoteSender, args, argsSize);

				UpdateModeAndChannel(cfg);

				SendToAll(Buffer.GetBytes());

				break;
			case RpcType.OnServer:
				if (fromPeer ==  GDNet.uniqueID)
				{
					TryCallMethodLocalWithSerialization(method, args, argsSize);
					break;
				}

				TryCallMethodLocal(method, args);
				break;

			case RpcType.Target:
				ServerSerializeRpcBuffer(method, _remoteSender, args, argsSize);
				UpdateModeAndChannel(cfg);
				SendTo(_remoteSender, Buffer.GetBytes());
				break;
		}

		_remoteSender = 0;

	}

	private void ClientProcessRpc(int sender, ushort rpcId, object[] args)
	{
		if (sender == GDNet.uniqueID)
			return;

		if (_rpcNameRegistry.TryGetValue(rpcId, out string method))
		{
			_remoteSender = sender;
			TryCallMethodLocal(method, args);
			_remoteSender = 0;
			return;
		}

		if (GDNet.Debug)
			GD.PushError($"Cant Find {rpcId} rpcId in registry.");

	}



	private void ProcessRpcPacketServer(long peerId, byte[] data)
	{
		Buffer.SetBytes(data);
		Buffer.Seek(0);

		RpcType type = (RpcType)Buffer.ReadByte();
		int targetId = -1;

		if (type == RpcType.Target)
			targetId = (int)Buffer.ReadLong();
  
		ushort rpcId = (ushort)Buffer.ReadLong();

		if (!_rpcNameRegistry.TryGetValue(rpcId, out string method))
		{
			GD.PushError($"Cant find {rpcId} rpcId in registry");
			return;
		}

		byte argsLength = Buffer.ReadUInt8();

		object[] args = new object[argsLength];

		for (byte i = 0; i < argsLength; i++)
		{
			args[i] = Buffer.Read();
		}

		ServerProcessRpc(peerId, targetId, method, type, args, argsLength);

	}

	private void ProcessRpcPacketClient(byte[] data)
	{
		Buffer.SetBytes(data);
		Buffer.Seek(0);

		int sender = (int)Buffer.ReadLong();
		ushort rpcId = (ushort)Buffer.ReadLong();
		byte argsLength = Buffer.ReadUInt8();

		object[] args = new object[argsLength];

		for (byte i = 0; i < argsLength; i++)
		{
			args[i] = Buffer.Read();
		}

		ClientProcessRpc(sender, rpcId, args);
	}

	private void ServerSerializeRpcBuffer(string method, int sender, object[] args, int argsSize)
	{
		Buffer.WriteLong(sender);
		Buffer.WriteLong(_rpcIdRegistry[method]);
		Buffer.WriteUInt8((byte)argsSize);

		for (byte i = 0; i < argsSize; i++)
		{
			Buffer.Write(args[i]);
		}

	}

	private void ClientSerializeRpcBuffer(RpcType type, int target, string method, object[] args, int argsSize)
	{
		Buffer.WriteByte((byte)type);

		if (type == RpcType.Target)
		{
			Buffer.WriteLong(target);
		}

		Buffer.WriteLong(_rpcIdRegistry[method]);
		Buffer.WriteUInt8((byte)argsSize);

		for (int i = 0; i < argsSize; i++)
		{
			Buffer.Write(args[i]);
		}

	}

	public override void ReceivedBytes(long peerId, byte[] data)
	{
		bool fromServer = peerId == GDNet.ServerID;

		if (fromServer)
		{
			ProcessRpcPacketClient(data);
		}

		else
		{
			ProcessRpcPacketServer(peerId, data);
		}

	}

	private void TryCallMethodLocal(string method, object[] args)
	{
		if (_delegateBinds.TryGetValue(method, out var @delegate))
		{
			@delegate.DynamicInvoke(args);
		}

	}

	private void TryCallMethodLocalWithSerialization(string method, object[] args, int argsSize)
	{
		BufferLocal.Clear();
		for (byte i = 0; i < argsSize; i++)
		{
			BufferLocal.Write(args[i]);
		}

		BufferLocal.Seek(0);

		for (byte i = 0; i < argsSize; i++)
		{
			args[i] = BufferLocal.Read();
		}

		TryCallMethodLocal(method, args);
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
		if (cfg["permission"].As<string>() == "server_or_auth")
			return peerId == GDNet.ServerID || authority == peerId;
		return true;
	}

	private bool ValidateWithError(long peerId, int authority, Dictionary<string, Variant> cfg)
	{
		bool validation = Validate(peerId, authority, cfg);
		if (!validation && GDNet.Debug)
			GD.PushError($"Rpc Validation Failed for {peerId} id; {authority} auth; {GetHashedNetworkID()} networkId; {cfg}");
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
			override_cfg["permission"] = GDNetRpcAttribute.PermissionStringServerOrAuth;

		if (!override_cfg.ContainsKey("mode"))
			override_cfg["mode"] = GDNetRpcAttribute.ModeStringReliable;

		if (!override_cfg.ContainsKey("channel"))
			override_cfg["channel"] = 0;

	}
}
