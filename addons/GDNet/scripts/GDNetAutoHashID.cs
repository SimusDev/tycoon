using Godot;
using System;

public partial class GDNetAutoHashID : RefCounted
{

	private WeakRef _ref;

	public static GDNetAutoHashID Create(GodotObject obj)
	{
		GDNetAutoHashID result = new();
		if (obj is Resource || obj is Node)
		{
			result.Initialize(obj);
		}

		return result;
	}

	private void Initialize(GodotObject obj)
	{
		_ref = WeakRef(obj);

		if (obj is Node)
		{
			//Node node = (Node)obj;
			//if (node.IsInsideTree())
			//	GDNet.Instance.SetObjectHashID(node, GDNet.GenerateObjectHashID(node));

			//node.TreeEntered += NodeGenerateHashID;
			//node.Renamed += NodeGenerateHashID;

			//GDNetMeta.Set(obj, "AutoHashID", this);

			return;
		}

		if (obj is Resource)
		{
			//ulong hash = GDNet.GenerateObjectHashID(obj);
			//GDNet.Instance.SetObjectHashID(obj, hash);
			return;
		}

	}

	private void NodeGenerateHashID()
	{
		Node node = (Node)_ref.GetRef();
		if (!IsInstanceValid(node))
		{
			return;
		}

		//GDNet.Instance.SetObjectHashID(node, GDNet.GenerateObjectHashID(node));

	}
}
