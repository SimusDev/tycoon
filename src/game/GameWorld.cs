using Godot;
using Godot.Collections;
using System;

public partial class GameWorld : Node3D
{
	[Export] MyNetworkedClass _test = new();

	public override void _Ready()
	{
        _test.InitOnClient(12321312);
	}

}
