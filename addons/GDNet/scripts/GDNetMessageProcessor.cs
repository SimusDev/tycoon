using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[GlobalClass]
public partial class GDNetMessageProcessor : Node
{
	public static GDNetMessageProcessor Instance;

	private GDNetUtils.ChunkedList<PendingCommunication> _pendingCommunicator = new(512);
	private List<CommunicationPacket> _pendingCommunicatorPacket = new(1024);

	private ConcurrentDictionary<CommunicationBatchKey, CommunicationBatch> _batchProcess = new();

	const int MTU = 1400;

	internal struct PendingCommunication
	{
		public ulong NetworkID;
		public long Peer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
		public byte[] Data;

		public PendingCommunication(ulong networkId, long peer, MultiplayerPeer.TransferModeEnum mode, int channel, byte[] data)
		{
			NetworkID = networkId;
			Peer = peer;
			Mode = mode;
			Channel = channel;
			Data = data;
		}
	}

	internal struct CommunicationBatch : IDisposable, IEquatable<CommunicationBatch>
	{
		public long Peer;
		public byte Mode;
		public byte Channel;

		public GDNetBuffer Buffer;

		public CommunicationBatch(long peer, byte mode, byte channel)
		{
			Peer = peer;
			Mode = mode;
			Channel = channel;
			Buffer = new GDNetBuffer();
		}

		public void Dispose()
		{

		}

		public bool Equals(CommunicationBatch other)
		{
			return Peer == other.Peer && Mode == other.Mode && Channel == other.Channel;
		}

		public override bool Equals(object other)
		{
			return other is CommunicationBatch && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Peer, Mode, Channel);
		}
	}

	internal struct CommunicationBatchKey : IEquatable<CommunicationBatchKey>
	{
		public readonly long Peer;
		public readonly byte Mode;
		public readonly byte Channel;

		public CommunicationBatchKey(long peer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			Peer = peer;
			Mode = (byte)mode;
			Channel = (byte)channel;
		}

		public bool Equals(CommunicationBatchKey other)
		{
			return Peer == other.Peer && Mode == other.Mode && Channel == other.Channel;
		}

		public override bool Equals(object obj)
		{
			return obj is CommunicationBatchKey other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Peer, Mode, Channel);
		}
	}

	public override void _EnterTree()
	{
		Instance = this;
	}

	public void SingletonReady()
	{
		GDNet.Instance.OnNetworkPacket += OnGDNetPacket;
	}

	public void ProcessAll()
	{
		ProcessCommunication();
		ProcessCommunicationReceive();
	}

	private void ProcessCommunication()
	{
		List<PendingCommunication[]> pending = _pendingCommunicator.TakeOwnership();

		if (pending.Count == 0) return;

		var results = new List<CommunicationBatch>[pending.Count];
		for (int i = 0; i < pending.Count; i++)
		{
			results[i] = new List<CommunicationBatch>();
		}

		Parallel.For(0, pending.Count, (chunkIndex) =>
		{
			PendingCommunication[] chunk = pending[chunkIndex];
			var localBatches = new Dictionary<CommunicationBatchKey, CommunicationBatch>();

			for (int j = 0; j < chunk.Length; j++)
			{
				PendingCommunication data = chunk[j];
				
				if (data.Data == null)
					continue;

				var key = new CommunicationBatchKey(data.Peer, data.Mode, data.Channel);

				if (!localBatches.TryGetValue(key, out var batch))
				{
					batch = new CommunicationBatch(key.Peer, key.Mode, key.Channel);
					localBatches[key] = batch;
				}

				batch.Buffer.WriteUInt64(data.NetworkID);
				batch.Buffer.WriteBytesDynamic(data.Data);

				if (batch.Buffer.Size >= MTU)
				{
					lock (results)
					{
						results[chunkIndex].Add(batch);
					}

					localBatches[key] = new CommunicationBatch(key.Peer, key.Mode, key.Channel);
				}
			}

			foreach (var kvp in localBatches)
			{
				lock (results)
				{
					results[chunkIndex].Add(kvp.Value);
				}
			}
		});

		for (int i = 0; i < results.Length; i++)
		{
			foreach (var batch in results[i])
			{
				GDNet.Instance.SendPacket(GDNet.PacketType.CommunicationMessage, batch.Buffer.GetBytes(), (int)batch.Peer, (MultiplayerPeer.TransferModeEnum)batch.Mode, batch.Channel);
				batch.Dispose();
			}
		}

		_batchProcess.Clear();
	}

	private void OnGDNetPacket(GDNet.PacketType type, byte[] data, long peer)
	{
		if (type == GDNet.PacketType.CommunicationMessage)
		{
			_pendingCommunicatorPacket.Add(new CommunicationPacket(peer, data));
		}
	}

	internal struct CommunicationPacket
	{
		public readonly long Peer;
		public byte[] Data;

		public CommunicationPacket(long peer, byte[] data)
		{
			Peer = peer;
			Data = data;
		}
	}

	internal struct ProcessedCommunicationPacket
	{
		public Dictionary<ulong, List<byte[]>> Result;
		public long PeerId;

		public ProcessedCommunicationPacket(Dictionary<ulong, List<byte[]>> result, long peer)
		{
			Result = result;
			PeerId = peer;
		}
	}

	private void ProcessCommunicationReceive()
	{
		List<CommunicationPacket> local = _pendingCommunicatorPacket;
		_pendingCommunicatorPacket = new List<CommunicationPacket>();

		if (local.Count == 0) return;

		ProcessedCommunicationPacket[] results = new ProcessedCommunicationPacket[local.Count];

		Parallel.For(0, local.Count, (index) =>
		{
			var processResult = ProcessReceivedCommunicatorPacket(local[index]);
			results[index] = processResult;
		});

		foreach (ProcessedCommunicationPacket p in results)
		{
			foreach(ulong netId in p.Result.Keys)
			{	
				GDNetCommunicator obj = GDNetCommunicator.FindByNetworkID(netId);
				if (obj == null)
				{
					if (GDNet.Debug)
						GD.PushError($"YOUR UNIQUE ID: {GDNet.uniqueID}, Cant Find GDNetCommunicator by NetworkID: {netId}");

					continue;
				}

				List<byte[]> packets = p.Result[netId];

				foreach (byte[] raw in packets)
					obj.ReceivedBytes(p.PeerId, raw);

			}
		}
		
	}

	private ProcessedCommunicationPacket ProcessReceivedCommunicatorPacket(CommunicationPacket packet)
	{
		Dictionary<ulong, List<byte[]>> Result = new();

		GDNetBuffer buffer = new();
		buffer.SetBytes(packet.Data);

		while (buffer.AvailableBytes > 0)
		{
			ulong netId = buffer.ReadUInt64();
			byte[] data = buffer.ReadBytesDynamic();

			if (!Result.TryGetValue(netId, out var packets))
			{
				packets = new List<byte[]>();
				Result[netId] = packets;
			}

			packets.Add(data);


		}

		buffer.Dispose();
		return new ProcessedCommunicationPacket(Result, packet.Peer);
	}


	public void ___QueueCommunicator(ulong networkID, int peer, byte[] data, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_pendingCommunicator.Add(new PendingCommunication(networkID, peer, mode, channel, data));
	}



}
