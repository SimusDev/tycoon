#if TOOLS
using Godot;

[Tool]
public partial class GDNetPlugin : EditorPlugin
{
	public override void _EnterTree()
	{
		AddAutoloadSingleton("GDNet", "res://addons/GDNet/singletons/GDNet.tscn");
	}

	public override void _ExitTree()
	{
		RemoveAutoloadSingleton("GDNet");
	}
}
#endif
