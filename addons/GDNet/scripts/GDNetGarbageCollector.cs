using Godot;
using System;

[GlobalClass]
public partial class GDNetGarbageCollector : Node
{
	private Timer _timer = null;

	public static GDNetGarbageCollector Instance = null;

	[Signal] public delegate void TryCollectEventHandler();

	public static void SetCollectTime(float value)
	{
		if (!IsInstanceValid(Instance))
			return;

		if (IsInstanceValid(Instance._timer))
			Instance._timer.WaitTime = value;
	}

	public override void _Ready()
	{
		Instance = this;
		_timer = new Timer();
		_timer.Timeout += OnTimerTimeout;
		AddChild(_timer);
		_timer.WaitTime = 15;
		_timer.Start();
	}
	private void OnTimerTimeout()
	{
		EmitSignal(SignalName.TryCollect);
	}
}
