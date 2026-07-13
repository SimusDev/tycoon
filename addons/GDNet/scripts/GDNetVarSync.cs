using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class GDNetVarSync : GDNetCommunicator
{

	private ushort _nextVarID = 0;

	private System.Collections.Generic.Dictionary<string, WeakRef> _bindings = new();

	private System.Collections.Generic.Dictionary<string, Dictionary<string, Variant>> _cfgRegistry = new();
	private System.Collections.Generic.Dictionary<string, ushort> _idRegistry = new(); 
	private System.Collections.Generic.Dictionary<ushort, string> _nameRegistry = new();

	internal enum PacketType : byte
	{
		SyncAll,
		SyncAllReceive,
		SyncReceive
	}

	public void Register(string name, Dictionary<string, Variant> cfg)
	{
		ParseCfgRef(cfg);
		_cfgRegistry[name] = cfg;
		_idRegistry[name] = _nextVarID;
		_nameRegistry[_nextVarID] = name;
		_nextVarID++;
	}

	public void RegisterAndBind(GodotObject obj, string name, Dictionary<string, Variant> cfg)
	{
		Register(name, cfg);
		BindVar(obj, name);
	}

	public void SyncAllVars()
	{
		if (GDNet.isServer || _cfgRegistry.Count == 0)
			return;



	}

	public void BindVar(GodotObject obj, string name)
	{
		_bindings[name] = WeakRef(obj);
	}

	private void ParseCfgRef(Dictionary<string, Variant> override_cfg)
	{
		if (!override_cfg.ContainsKey("mode"))
			override_cfg["mode"] = "reliable";

		if (!override_cfg.ContainsKey("channel"))
			override_cfg["channel"] = 0;
	}

	private void UpdateModeAndChannel(Dictionary<string, Variant> cfg)
	{
		Mode = StringToTransferMode(cfg["mode"].ToString());
		Channel = cfg["channel"].AsInt32();
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
