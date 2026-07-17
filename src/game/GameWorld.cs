using Godot;
using System;

public partial class GameWorld : Node3D
{
    private GDNetBuffer _buffer = new();

    public override void _Ready()
    {
        _buffer.Clear();

        Godot.Collections.Dictionary sas = new();
        sas["sas??!?!?!"] = 78;
        _buffer.WriteVar(sas);

        _buffer.Seek(0);
        GD.Print(_buffer.ReadVar());
    }
}
