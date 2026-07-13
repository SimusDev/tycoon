using Godot;
using System;

public partial class GDNetMultiplayer : MultiplayerApiExtension
{
	private SceneMultiplayer _base = new();
	public GDNetMultiplayer()
	{

	}

	public override Error _Rpc(int peer, GodotObject @object, StringName method, Godot.Collections.Array args)
	{

		return Error.Ok;
	}

}
