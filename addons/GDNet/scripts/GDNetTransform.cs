using Godot;
using System;

[GlobalClass]
public partial class GDNetTransform : Node
{
	public override void _Ready()
	{

	}

	public override void _Process(double delta)
	{
	}

	public void Flush()
	{
		if (Multiplayer.GetUniqueId() != GetMultiplayerAuthority())
			return;



	}

}
