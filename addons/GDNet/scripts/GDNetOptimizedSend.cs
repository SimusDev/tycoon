using GDNetUtils;
using Godot;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

[GlobalClass]
public partial class GDNetOptimizedSend : Node
{
	private SceneMultiplayer _api;

	private ChunkedList<QueuedPacket> _pendingChunkedPacketsQueue = new(256);
	private ConcurrentQueue<QueuedPacket> _pendingPacketsQueue = new();
	private ConcurrentQueue<ReceivedPacket> _pendingReceivedPacketsQueue = new();

	private ConcurrentQueue<BatchData> _pendingBatchesToCompress = new();

	public const int MTU = 1350;

	[Signal] public delegate void MultiplayerPeerPacketEventHandler(long id, byte[] bytes);

	const int CompressionThresholdDeflate = 256;
	const int CompressionThresholdZstd = 1024;

	enum CompressHeader: byte
	{
		None,
		Deflate,
		Zstd,
	}

	public void Setup(SceneMultiplayer api)
	{
		_api = api;
		_api.PeerPacket += OnApiPeerPacket;
	}

	private void OnApiPeerPacket(long id, byte[] packet)
	{
		_pendingReceivedPacketsQueue.Enqueue(
			new ReceivedPacket(id, packet)
			);

	}

	private void ProcessReceivedPackets()
	{
        var packets = new List<ReceivedPacket>();
        while (_pendingReceivedPacketsQueue.TryDequeue(out var packet))
        {
            packets.Add(packet);
        }

        if (packets.Count == 0) return;

        var decompressed = new byte[packets.Count][];
        var unbatchResults = new List<byte[]>[packets.Count];

        Parallel.For(0, packets.Count, (i) =>
        {
            decompressed[i] = TryDecompressBytes(packets[i].Data);
        });

        Parallel.For(0, packets.Count, (i) =>
        {
			byte[] rawBytes = decompressed[i];
            unbatchResults[i] = TryUnbatchRawPackets(rawBytes);
        });

        for (int i = 0; i < packets.Count; i++)
        {
            var unbatch = unbatchResults[i];
            if (unbatch == null || unbatch.Count == 0) continue;

			for (int j = 0; i + j < unbatch.Count; j++)
                EmitSignal(SignalName.MultiplayerPeerPacket, packets[i].SenderId, unbatch[j]);

        }

    }

	private List<byte[]> TryUnbatchRawPackets(byte[] bytes)
	{
		GDNetBuffer buffer = new();
		buffer.SetBytes(bytes);
		List<byte[]> result = new();
		while(buffer.AvailableBytes > 0)
		{
            result.Add(buffer.ReadBytes());
		}

		return result;
	}

    public void ProcessAll()
	{
		ProcessReceivedPackets();
		CollectAndBatchPendingPackets();
		CompressPendingBatches();
	}

    private void CollectAndBatchPendingPackets()
    {
        var packets = new List<QueuedPacket>();
        while (_pendingPacketsQueue.TryDequeue(out var packet))
        {
            packets.Add(packet);
        }

        if (packets.Count == 0) return;

        const int chunkSize = 200;
        int chunkCount = (packets.Count + chunkSize - 1) / chunkSize;
        var results = new List<KeyValuePair<BatchKey, BatchData>>[chunkCount];
        for (int i = 0; i < chunkCount; i++)
        {
            results[i] = new List<KeyValuePair<BatchKey, BatchData>>();
        }

        Parallel.For(0, chunkCount, (chunkIndex) =>
        {
            int start = chunkIndex * chunkSize;
            int end = Math.Min(start + chunkSize, packets.Count);
            var localBatches = new Dictionary<BatchKey, BatchData>();

            for (int i = start; i < end; i++)
            {
                var packet = packets[i];
                var key = new BatchKey(packet.TargetPeer, packet.Mode, packet.Channel);

                if (!localBatches.TryGetValue(key, out var data))
                {
                    data = new BatchData(key.TargetPeer, key.Mode, key.Channel);
                    localBatches[key] = data;
                }

                data.Buffer.WriteBytes(packet.Data);

                if (data.Buffer.Size >= MTU)
                {
                    lock (results[chunkIndex]) { results[chunkIndex].Add(new(key, data)); }
                    localBatches[key] = new BatchData(key.TargetPeer, key.Mode, key.Channel);
                }
            }

            foreach (var kvp in localBatches)
            {
                if (kvp.Value.Buffer.Size > 0)
                {
                    lock (results[chunkIndex]) { results[chunkIndex].Add(kvp); }
                }
            }
        });

        for (int i = 0; i < chunkCount; i++)
        {
            foreach (var result in results[i])
            {
                _pendingBatchesToCompress.Enqueue(result.Value);
            }
        }
    }

    private void CompressPendingBatches()
	{
		var datas = new List<BatchData>();
		while (_pendingBatchesToCompress.TryDequeue(out var data))
		{
			datas.Add(data);
		}

		if (datas.Count == 0) return;

		var compressedData = new (int Index, BatchData Data)[datas.Count];

		Parallel.For(0, datas.Count, (i) =>
		{
			BatchData pData = datas[i];
			byte[] compressed = TryCompressBytes(pData.Buffer.GetBytes());
			pData.Buffer.SetBytes(compressed);
			compressedData[i] = (i, pData);
		});

		for (int i = 0; i < compressedData.Length; i++)
		{
			var data = compressedData[i].Data;
			SendBytesInternal(data.Buffer.GetBytes(), (int)data.TargetPeer, data.Mode, data.Channel);
		}
	}

	private byte[] TryCompressBytes(byte[] bytes)
	{
		int length = bytes.Length;

		using var stream = new MemoryStream();
		using var writer = new BinaryWriter(stream);

		if (length < CompressionThresholdDeflate)
		{
			writer.Write((byte)CompressHeader.None);
			writer.Write(bytes);
			return stream.ToArray();
		}

		if (length >= CompressionThresholdZstd)
		{
			writer.Write((byte)CompressHeader.Zstd);
			writer.Write(length);
			var compressed = bytes.Compress(Godot.FileAccess.CompressionMode.Zstd);
			writer.Write(compressed);
		}
		else if (length >= CompressionThresholdDeflate)
		{
			writer.Write((byte)CompressHeader.Deflate);
			writer.Write(length);
			var compressed = bytes.Compress(Godot.FileAccess.CompressionMode.Deflate);
			writer.Write(compressed);
		}

		return stream.ToArray();
	}

	private byte[] TryDecompressBytes(byte[] bytes)
	{
		using var stream = new MemoryStream(bytes);
		using var reader = new BinaryReader(stream);

		var header = (CompressHeader)reader.ReadByte();

		if (header == CompressHeader.None)
		{
			return bytes.Skip(1).ToArray();
		}

		int originalSize = reader.ReadInt32();

		int compressedSize = bytes.Length - 1 - 4;
		byte[] compressedData = reader.ReadBytes(compressedSize);

		switch (header)
		{
			case CompressHeader.Deflate:
				return compressedData.Decompress(originalSize, Godot.FileAccess.CompressionMode.Deflate);
			case CompressHeader.Zstd:
				return compressedData.Decompress(originalSize, Godot.FileAccess.CompressionMode.Zstd);
			default:
				return compressedData;
		}
	}

	private struct QueuedPacket
	{
		public QueuedPacket(byte[] data, int targetPeer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			Data = data;
			TargetPeer = targetPeer;
			Mode = mode;
			Channel = channel;
		}

		public byte[] Data;
		public int TargetPeer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
	}

	private struct ReceivedPacket
	{
		public ReceivedPacket(long senderId, byte[] data)
		{
			SenderId = senderId;
			Data = data;
		}

		public long SenderId;
		public byte[] Data;
	}
	private struct BatchKey
	{
		public BatchKey(long targetPeer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			TargetPeer = targetPeer;
			Mode = mode;
			Channel = channel;
		}

		public long TargetPeer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
	}

	private struct BatchData
	{
		public BatchData(long targetPeer, MultiplayerPeer.TransferModeEnum mode, int channel)
		{
			TargetPeer = targetPeer;
			Mode = mode;
			Channel = channel;
			Buffer = new();
		}

		public long TargetPeer;
		public MultiplayerPeer.TransferModeEnum Mode;
		public int Channel;
		public GDNetBuffer Buffer;
	}

	public void MultiplayerSendBytes(byte[] data, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		_pendingPacketsQueue.Enqueue(
			new QueuedPacket(
				data,
				id,
				mode,
				channel
				)
			);
	}

	private void SendBytesInternal(byte[] data, int id, MultiplayerPeer.TransferModeEnum mode, int channel)
	{
		if (_api.GetUniqueId() == id)
		{
			OnApiPeerPacket(id, data);
			return;
		}

		_api.SendBytes(data, id, mode, channel);
	}
}
