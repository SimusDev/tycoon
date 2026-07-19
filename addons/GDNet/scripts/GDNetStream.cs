using System;

public class GDNetStream : IDisposable
{
	private byte[] _buffer = new byte[512];
	private int _position = 0;
	private int _length = 0;

	public void WriteByte(byte value)
	{
		EnsureCapacity(1);
		_buffer[_position++] = value;
		if (_position > _length) _length = _position;
	}

	public void WriteBytes(byte[] data)
	{
		if (data == null || data.Length == 0) return;
		EnsureCapacity(data.Length);
		Buffer.BlockCopy(data, 0, _buffer, _position, data.Length);
		_position += data.Length;
		if (_position > _length) _length = _position;
	}


	public void WriteInt8(sbyte value)
	{
		WriteByte((byte)value);
	}

	public void WriteInt16(short value)
	{
		EnsureCapacity(2);
		_buffer[_position++] = (byte)(value & 0xFF);
		_buffer[_position++] = (byte)((value >> 8) & 0xFF);
		if (_position > _length) _length = _position;
	}

	public void WriteInt32(int value)
	{
		EnsureCapacity(4);
		_buffer[_position++] = (byte)(value & 0xFF);
		_buffer[_position++] = (byte)((value >> 8) & 0xFF);
		_buffer[_position++] = (byte)((value >> 16) & 0xFF);
		_buffer[_position++] = (byte)((value >> 24) & 0xFF);
		if (_position > _length) _length = _position;
	}

	public void WriteInt64(long value)
	{
		EnsureCapacity(8);
		_buffer[_position++] = (byte)(value & 0xFF);
		_buffer[_position++] = (byte)((value >> 8) & 0xFF);
		_buffer[_position++] = (byte)((value >> 16) & 0xFF);
		_buffer[_position++] = (byte)((value >> 24) & 0xFF);
		_buffer[_position++] = (byte)((value >> 32) & 0xFF);
		_buffer[_position++] = (byte)((value >> 40) & 0xFF);
		_buffer[_position++] = (byte)((value >> 48) & 0xFF);
		_buffer[_position++] = (byte)((value >> 56) & 0xFF);
		if (_position > _length) _length = _position;
	}

	public void WriteUInt8(byte value)
	{
		WriteByte(value);
	}

	public void WriteUInt16(ushort value)
	{
		EnsureCapacity(2);
		_buffer[_position++] = (byte)(value & 0xFF);
		_buffer[_position++] = (byte)((value >> 8) & 0xFF);
		if (_position > _length) _length = _position;
	}

	public void WriteUInt32(uint value)
	{
		EnsureCapacity(4);
		_buffer[_position++] = (byte)(value & 0xFF);
		_buffer[_position++] = (byte)((value >> 8) & 0xFF);
		_buffer[_position++] = (byte)((value >> 16) & 0xFF);
		_buffer[_position++] = (byte)((value >> 24) & 0xFF);
		if (_position > _length) _length = _position;
	}

	public void WriteUInt64(ulong value)
	{
		EnsureCapacity(8);
		_buffer[_position++] = (byte)(value & 0xFF);
		_buffer[_position++] = (byte)((value >> 8) & 0xFF);
		_buffer[_position++] = (byte)((value >> 16) & 0xFF);
		_buffer[_position++] = (byte)((value >> 24) & 0xFF);
		_buffer[_position++] = (byte)((value >> 32) & 0xFF);
		_buffer[_position++] = (byte)((value >> 40) & 0xFF);
		_buffer[_position++] = (byte)((value >> 48) & 0xFF);
		_buffer[_position++] = (byte)((value >> 56) & 0xFF);
		if (_position > _length) _length = _position;
	}

	public byte ReadByte()
	{
		if (_position >= _length)
			throw new InvalidOperationException("End of stream");
		return _buffer[_position++];
	}

	public byte[] ReadBytes(int count)
	{
		if (count == 0) return Array.Empty<byte>();
		if (_position + count > _length)
			throw new InvalidOperationException($"Not enough data: need {count}, have {_length - _position}");

		var result = new byte[count];
		Buffer.BlockCopy(_buffer, _position, result, 0, count);
		_position += count;
		return result;
	}

	public sbyte ReadInt8()
	{
		return (sbyte)ReadByte();
	}

	public short ReadInt16()
	{
		if (_position + 2 > _length)
			throw new InvalidOperationException("Not enough data for Int16");

		short value = (short)(
			_buffer[_position] |
			(_buffer[_position + 1] << 8)
		);
		_position += 2;
		return value;
	}

	public int ReadInt32()
	{
		if (_position + 4 > _length)
			throw new InvalidOperationException("Not enough data for Int32");

		int value = (
			_buffer[_position] |
			(_buffer[_position + 1] << 8) |
			(_buffer[_position + 2] << 16) |
			(_buffer[_position + 3] << 24)
		);
		_position += 4;
		return value;
	}

	public long ReadInt64()
	{
		if (_position + 8 > _length)
			throw new InvalidOperationException("Not enough data for Int64");

		long value = (
			_buffer[_position] |
			((long)_buffer[_position + 1] << 8) |
			((long)_buffer[_position + 2] << 16) |
			((long)_buffer[_position + 3] << 24) |
			((long)_buffer[_position + 4] << 32) |
			((long)_buffer[_position + 5] << 40) |
			((long)_buffer[_position + 6] << 48) |
			((long)_buffer[_position + 7] << 56)
		);
		_position += 8;
		return value;
	}

	public byte ReadUInt8()
	{
		return ReadByte();
	}

	public ushort ReadUInt16()
	{
		if (_position + 2 > _length)
			throw new InvalidOperationException("Not enough data for UInt16");

		ushort value = (ushort)(
			_buffer[_position] |
			(_buffer[_position + 1] << 8)
		);
		_position += 2;
		return value;
	}

	public uint ReadUInt32()
	{
		if (_position + 4 > _length)
			throw new InvalidOperationException("Not enough data for UInt32");

		uint value = (
			_buffer[_position] |
			((uint)_buffer[_position + 1] << 8) |
			((uint)_buffer[_position + 2] << 16) |
			((uint)_buffer[_position + 3] << 24)
		);
		_position += 4;
		return value;
	}

	public ulong ReadUInt64()
	{
		if (_position + 8 > _length)
			throw new InvalidOperationException("Not enough data for UInt64");

		ulong value = (
			_buffer[_position] |
			((ulong)_buffer[_position + 1] << 8) |
			((ulong)_buffer[_position + 2] << 16) |
			((ulong)_buffer[_position + 3] << 24) |
			((ulong)_buffer[_position + 4] << 32) |
			((ulong)_buffer[_position + 5] << 40) |
			((ulong)_buffer[_position + 6] << 48) |
			((ulong)_buffer[_position + 7] << 56)
		);
		_position += 8;
		return value;
	}

	private void EnsureCapacity(int needed)
	{
		if (_position + needed <= _buffer.Length) return;
		int newSize = Math.Max(_buffer.Length * 2, _position + needed);
		Array.Resize(ref _buffer, newSize);
	}

	public void SetBytes(byte[] data)
	{
		if (data.Length > _buffer.Length)
			_buffer = new byte[data.Length];
		Buffer.BlockCopy(data, 0, _buffer, 0, data.Length);
		_position = 0;
		_length = data.Length;
	}

	public byte[] GetBytes()
	{
		var result = new byte[_length];
		Buffer.BlockCopy(_buffer, 0, result, 0, _length);
		return result;
	}

	public Span<byte> AsSpan()
	{
		return _buffer.AsSpan(0, _length);
	}

	public void Clear()
	{
		_position = 0;
		_length = 0;
	}

	public void WriteFloat(float value)
	{
		WriteInt32(BitConverter.SingleToInt32Bits(value));
	}

	public float ReadFloat()
	{
		return BitConverter.Int32BitsToSingle(ReadInt32());
	}

	public void WriteDouble(double value)
	{
		WriteInt64(BitConverter.DoubleToInt64Bits(value));
	}

	public double ReadDouble()
	{
		return BitConverter.Int64BitsToDouble(ReadInt64());
	}

	public void WriteBool(bool value)
	{
		WriteByte(value ? (byte)1 : (byte)0);
	}

	public bool ReadBool()
	{
		return ReadByte() != 0;
	}

	public void WriteString(string value)
	{
		if (value == null)
		{
			WriteUInt16(0xFFFF);
			return;
		}
		byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
		WriteUInt32((uint)bytes.Length);
		WriteBytes(bytes);
	}

	public string ReadString()
	{
		uint length = ReadUInt32();
		if (length == 0xFFFF) return null;
		byte[] bytes = ReadBytes((int)length);
		return System.Text.Encoding.UTF8.GetString(bytes);
	}

	private bool _disposed = false;
	public void Dispose()
	{
		if (_disposed) return;

		_buffer = null;
		_position = 0;
		_length = 0;
		_disposed = true;
		GC.SuppressFinalize(this);
	}

	public void Seek(int position) => _position = position;

	public int Length => _length;
	public int Position => _position;


}
