using Godot;
using System;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace GDNetDebug
{
	public partial class SimpleProfiler : Control
	{

		[Export] private Label _label;
		[Export] private Timer _timer;

		[Export] public int Ping = -1;

		private string _info = "";

		private int _inPackets = 0;
		private int _inBandwith = 0;
		private int _outPackets = 0;
		private int _outBandwith = 0;


		private double _GetAverageList(List<int> values)
		{
			if (values == null || values.Count == 0)
				return 0;

			long sum = 0;
			int count = values.Count;

			for (int i = 0; i < count; i++)
			{
				sum += values[i];
			}

			return (double)sum / count;
		}

		public override void _Ready()
		{
			_info = _label.Text;
			_timer.Start();
			_timer.Timeout += OnTimerTick;
			OnTimerTick();

			GDNet.Instance.OnNetworkReady += OnNetworkReady;
			GDNet.Instance.OnNetworkDisconnected += OnNetworkDisconnected;
			if (GDNet.Instance.IsConnectedToServer())
				OnNetworkReady();

			GDNet.Instance.OnNetworkPacketSizeSent += OnNetworkPacketSent;
			GDNet.Instance.OnNetworkPacketSizeReceived += OnNetworkPacketReceived;

		}

		private void OnTimerTick()
		{
			_label.Text = $"Fps: {Engine.GetFramesPerSecond()}\nIn: {_inPackets} / {NetworkFormatter.FormatBytesPerSecond(_inBandwith)}\nOut: {_outPackets} / {NetworkFormatter.FormatBytesPerSecond(_outBandwith)}\nPing: {Ping}";

			_inPackets = 0;
			_inBandwith = 0;

			_outPackets = 0;
			_outBandwith = 0;

		}

		private void OnNetworkPacketReceived(int size)
		{
			_inPackets++;
			_inBandwith += size;
		}

		private void OnNetworkPacketSent(int size)
		{
			_outPackets++;
			_outBandwith += size;
		}

		private void OnNetworkReady()
		{
			
		}

		private void OnNetworkDisconnected()
		{

		}

	}

}
