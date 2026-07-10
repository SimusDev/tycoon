using Godot;
using System;
using System.IO;


public partial class GameNetwork : Node
{
    public static GameNetwork Instance;

    private MemoryStream _writerStream;
    private BinaryWriter _writer;

    public GameNetwork()
    {
        _writerStream = new MemoryStream();
        _writer = new BinaryWriter(_writerStream);
    }

    public override void _EnterTree()
    {
        Instance = this;
    }


}
