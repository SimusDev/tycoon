using Godot;
using System;

[GlobalClass]
public partial class GDNetBuffer : RefCounted
{
	private GDNetStream _stream = null;

	public GDNetBuffer()
	{
		_stream = new GDNetStream();
	}
	internal enum VarType: byte
	{
		Null,
		GodotVar,
		Object,

		BytesDynamic,
		Bool,
		Int8,
		Int16,
		Int32,
		Int64,

		UInt8,
		UInt16,
		UInt32,
		UInt64,

		Vector3,
		Vector2,
	}

	internal enum GodotVarType : byte
	{
		Null,
		Internal,
		RawBytes,
		Int,
		Bool,
		String,
	}

	private delegate void WriteDelegate(GDNetBuffer buffer, object value);
	private static readonly System.Collections.Generic.Dictionary<Type, WriteDelegate> _writers = new();

	private delegate object ReadDelegate(GDNetBuffer buffer);
	private static readonly System.Collections.Generic.Dictionary<VarType, ReadDelegate> _readers = new();

	private delegate void WriteGodotVarDelegate(GDNetBuffer buffer, Variant value);
	private static readonly System.Collections.Generic.Dictionary<Variant.Type, WriteGodotVarDelegate> _writersGodotVar = new();

	private delegate object ReadGodotVarDelegate(GDNetBuffer buffer);
	private static readonly System.Collections.Generic.Dictionary<GodotVarType, ReadGodotVarDelegate> _readersGodotVar = new();

	static GDNetBuffer()
	{
		_writers[typeof(object)] = (b, v) => { b._WriteVarType(VarType.Object); b.WriteObject(v); };
		_readers[VarType.Object] = b => b.ReadObject();

		_writers[typeof(Variant)] = (b, v) => { b._WriteVarType(VarType.GodotVar); b.WriteVar((Variant)v); };
		_readers[VarType.GodotVar] = b => b.ReadVar();

		_writers[typeof(bool)] = (b, v) => { b._WriteVarType(VarType.Bool); b.WriteBool((bool)v); };
		_readers[VarType.Bool] = b =>  b.ReadBool();

		_writers[typeof(byte)] = (b, v) => { b._WriteVarType(VarType.UInt8); b.WriteUInt8((byte)v); };
		_readers[VarType.UInt8] = b => b.ReadUInt8();

		_writers[typeof(sbyte)] = (b, v) => { b._WriteVarType(VarType.Int8); b.WriteInt8((sbyte)v); };
		_readers[VarType.Int8] = b => b.ReadInt8();

		_writers[typeof(short)] = (b, v) => { b._WriteVarType(VarType.Int16); b.WriteInt16((short)v); };
		_readers[VarType.Int16] = b => b.ReadInt16();

		_writers[typeof(ushort)] = (b, v) => { b._WriteVarType(VarType.UInt16); b.WriteUInt16((ushort)v); };
		_readers[VarType.UInt16] = b => b.ReadUInt16();

		_writers[typeof(int)] = (b, v) => { b._WriteVarType(VarType.Int32); b.WriteInt32((int)v); };
		_readers[VarType.Int32] = b => b.ReadInt32();

		_writers[typeof(uint)] = (b, v) => { b._WriteVarType(VarType.UInt32); b.WriteUInt32((uint)v); };
		_readers[VarType.UInt32] = b => b.ReadUInt32();

		_writers[typeof(long)] = (b, v) => { b._WriteVarType(VarType.Int64); b.WriteInt64((long)v); };
		_readers[VarType.Int64] = b => b.ReadInt64();

		_writers[typeof(ulong)] = (b, v) => { b._WriteVarType(VarType.UInt64); b.WriteUInt64((ulong)v); };
		_readers[VarType.UInt64] = b => b.ReadUInt64();

		_writers[typeof(byte[])] = (b, v) => { b._WriteVarType(VarType.BytesDynamic); b.WriteBytesDynamic((byte[])v); };
		_readers[VarType.BytesDynamic] = b => b.ReadBytesDynamic();

		_writers[typeof(Vector3)] = (b, v) => { b._WriteVarType(VarType.Vector3); b.WriteVector3((Vector3)v); };
		_readers[VarType.Vector3] = b => b.ReadVector3();

		_writers[typeof(Vector2)] = (b, v) => { b._WriteVarType(VarType.Vector2); b.WriteVector2((Vector2)v); };
		_readers[VarType.Vector2] = b => b.ReadVector2();

		_writersGodotVar[Variant.Type.Int] = (b, v) => { b._WriteGodotVarType(GodotVarType.Int); b.WriteInt64((long)v); };
		_readersGodotVar[GodotVarType.Int] = b =>  b.ReadUInt64();

		_writersGodotVar[Variant.Type.Bool] = (b, v) => { b._WriteGodotVarType(GodotVarType.Bool); b.WriteBool((bool)v); };
		_readersGodotVar[GodotVarType.Bool] = b => b.ReadBool();

		_writersGodotVar[Variant.Type.String] = (b, v) => { b._WriteGodotVarType(GodotVarType.String); b.WriteString((string)v); };
		_readersGodotVar[GodotVarType.String] = b => b.ReadString();

		_writersGodotVar[Variant.Type.PackedByteArray] = (b, v) => { b._WriteGodotVarType(GodotVarType.RawBytes); b.WriteBytesDynamic((byte[])v); };
		_readersGodotVar[GodotVarType.RawBytes] = b => b.ReadBytesDynamic();
		_readersGodotVar[GodotVarType.Internal] = b => GD.BytesToVar(b.ReadBytesDynamic());
	}

	public void Seek(int position) => _stream.Seek(position);
	public int Position => _stream.Position;
	public int Length => _stream.Length;

	public int AvailableBytes => Length - Position;

	public int Size => _stream.Length;

	public void Clear() => _stream.Clear();

	protected override void Dispose(bool disposing)
	{
		_stream?.Dispose();
		base.Dispose(disposing);
	}

	public void SetBytes(byte[] bytes)
	{
		_stream.SetBytes(bytes);
	}

	public byte[] GetBytes()
	{
		return _stream.GetBytes();
	}

	public void WriteByte(byte value) => _stream.WriteByte(value);
	public byte ReadByte() => _stream.ReadByte();
	public void WriteBool(bool value) => _stream.WriteBool(value);
	public bool ReadBool() => _stream.ReadBool();
	public void WriteInt8(sbyte value) => _stream.WriteInt8(value);
	public sbyte ReadInt8() => _stream.ReadInt8();
	public void WriteInt16(short value) => _stream.WriteInt16(value);
	public short ReadInt16() => _stream.ReadInt16();
	public void WriteInt32(int value) => _stream.WriteInt32(value);
	public int ReadInt32() => _stream.ReadInt32();
	public void WriteInt64(long value) => _stream.WriteInt64(value);
	public long ReadInt64() => _stream.ReadInt64();
	public void WriteUInt8(byte value) => _stream.WriteUInt8(value);
	public byte ReadUInt8() => _stream.ReadUInt8();
	public void WriteUInt16(ushort value) => _stream.WriteUInt16(value);
	public ushort ReadUInt16() => _stream.ReadUInt16();
	public void WriteUInt32(uint value) => _stream.WriteUInt32(value);
	public uint ReadUInt32() => _stream.ReadUInt32();
	public void WriteUInt64(ulong value) => _stream.WriteUInt64(value);
	public ulong ReadUInt64() => _stream.ReadUInt64();
	public void WriteString(string value) => _stream.WriteString(value);
	public string ReadString() => _stream.ReadString();
	public void WriteFloat(float value) => _stream.WriteFloat(value);
	public float ReadFloat() => _stream.ReadFloat();
	public void WriteDouble(double value) => _stream.WriteDouble(value);
	public double ReadDouble() => _stream.ReadDouble();


	public void WriteVector3(Vector3 value)
	{
		WriteFloat(value.X);
		WriteFloat(value.Y);
		WriteFloat(value.Z);
	}

	public Vector3 ReadVector3()
	{
		return new Vector3(ReadFloat(), ReadFloat(), ReadFloat());
	}

	public void WriteVector2(Vector2 value)
	{
		WriteFloat(value.X);
		WriteFloat(value.Y);
	}

	public Vector2 ReadVector2()
	{
		return new Vector2(ReadFloat(), ReadFloat());
	}

	public void WriteFullNodeRef(Node node)
	{
		WriteString(node.GetPath().ToString());
	}

	public Node ReadFullNodeRef()
	{
		return GDNet.Instance.GetNode(ReadString());
	}

	public void WriteIntVar(int value)
	{
		uint zigzag = (uint)((value << 1) ^ (value >> 31)); 

		while (zigzag >= 0x80)
		{
			WriteByte((byte)(zigzag | 0x80));
			zigzag >>= 7;
		}
		WriteByte((byte)zigzag);
	}

	public int ReadIntVar()
	{
		uint result = 0;
		int shift = 0;
		byte b;

		do
		{
			b = ReadByte();
			result |= (uint)(b & 0x7F) << shift;
			shift += 7;

			if (shift > 35) 
				throw new InvalidOperationException("VarInt too long!");

		} while ((b & 0x80) != 0);

		return (int)(result >> 1) ^ -(int)(result & 1);
	}

	public void WriteLongVar(long value)
	{
		ulong zigzag = (ulong)((value << 1) ^ (value >> 63));

		while (zigzag >= 0x80)
		{
			WriteByte((byte)(zigzag | 0x80));
			zigzag >>= 7;
		}
		WriteByte((byte)zigzag);
	}

	public long ReadLongVar()
	{
		ulong result = 0;
		int shift = 0;
		byte b;

		do
		{
			if (shift >= 64) 
				throw new InvalidOperationException("VarLong too long!");

			b = ReadByte();
			result |= (ulong)(b & 0x7F) << shift;
			shift += 7;

		} while ((b & 0x80) != 0);

		return (long)(result >> 1) ^ -(long)(result & 1);
	}

	public void WriteBytes(byte[] bytes) => _stream.WriteBytes(bytes);
	public byte[] ReadBytes(int count) => _stream.ReadBytes(count);

	public void WriteBytesDynamic(byte[] bytes)
	{
		WriteIntVar(bytes.Length);
		_stream.WriteBytes(bytes);
	}

	public byte[] ReadBytesDynamic()
	{
		return _stream.ReadBytes(ReadIntVar());
	}

	private void _WriteVarType(VarType type)
	{
		_stream.WriteByte((byte)type);
	}

	private VarType _ReadVarType()
	{
		return (VarType)_stream.ReadByte();
	}

	private void _WriteGodotVarType(GodotVarType type)
	{
		_stream.WriteByte((byte)type);
	}

	private GodotVarType _ReadGodotVarType()
	{
		return (GodotVarType)_stream.ReadByte();
	}

	public void Write(object value)
	{
		if (value == null)
		{
			_WriteVarType(VarType.Null);
			return;
		}

		if (_writers.TryGetValue(value.GetType(), out var writer))
		{
			writer(this, value);
		}

		else
		{
			_WriteVarType(VarType.Object);
			WriteObject(value);
		}

	}

	private void WriteObject(object value)
	{
		if (value is IGDNetSerializable)
		{
			WriteByte(1);
			WriteSerializable((IGDNetSerializable)value);
		}

		else if (value is Resource)
		{
			var resource = (Resource)value;
			if (resource.ResourcePath != "")
			{
				WriteByte(2);
				WriteResource(resource);
			}
		}

		else
		{
			WriteByte(0);
			GD.PushError($"Unsupported type: {value.GetType()}");
		}

	}

	private object ReadObject()
	{
		byte type = ReadByte();
		switch (type)
		{
			case 1:
				return ReadSerializable();
			case 2:
				return ReadResource();
		}

		GD.PushError($"Unsupported type: {type}");
		return null;
	}

	public void WriteResource(Resource resource)
	{
		if (resource == null)
		{
			WriteBool(true);
			return;
		}

		long hash = ResourceUid.TextToId(ResourceUid.PathToUid(resource.ResourcePath));
		WriteInt64(hash);
	}

	public Resource ReadResource()
	{
		bool isNull = ReadBool();
		if (isNull)
			return null;

		string id = ResourceUid.IdToText(ReadInt64());
		return GD.Load(id);
	}

	public T ReadResource<T>() where T : Resource
	{
		return (T)ReadResource();
	}

	public void WriteSerializable(IGDNetSerializable value)
	{
		WriteString(value.GetType().FullName);
		value.Serialize(this);
	}

	public void WriteSerializableOrNull(IGDNetSerializable value)
	{
		WriteBool(value != null);
		if (value != null)
			WriteSerializable(value);
	}

	public T ReadSerializableOrNull<T>() where T : IGDNetSerializable
	{
		if (ReadBool())
			return ReadSerializable<T>();
		return default;
	}

	public T ReadSerializable<T>() where T : IGDNetSerializable
	{
		Type type = Type.GetType(ReadString());
		var obj = (IGDNetSerializable)Activator.CreateInstance(type);
		obj.Deserialize(this);
		return (T)obj;
	}

	public IGDNetSerializable ReadSerializable()
	{
		Type type = Type.GetType(ReadString());
		var obj = (IGDNetSerializable)Activator.CreateInstance(type);
		obj.Deserialize(this);
		return obj;
	}

	public void WriteList<T>(System.Collections.Generic.List<T> value)
	{
		WriteIntVar(value.Count);
		for (int i = 0; i < value.Count; i++)
			Write(value[i]);
	}

	public System.Collections.Generic.List<T> ReadList<T>()
	{
		System.Collections.Generic.List<T> result = new();
		long length = ReadIntVar();
		for (int i = 0; i < length; i++)
			result.Add((T)Read());
		return result;

	}

	public object Read()
	{
		VarType type = _ReadVarType();

		if (_readers.TryGetValue(type, out var reader))
		{
			return reader(this);
		}

		GD.PushError($"Unknown VarType: {type}");
		return null;
	}

	public void WriteVar(Variant value)
	{
		if (_writersGodotVar.TryGetValue(value.VariantType, out var writer))
		{
			writer(this, value);
		}
		else
		{
			_WriteGodotVarType(GodotVarType.Internal);
			WriteBytesDynamic(GD.VarToBytes(value));
		}
	}

	public Variant ReadVar()
	{
		GodotVarType type = _ReadGodotVarType();

		if (_readersGodotVar.TryGetValue(type, out var reader))
		{
			return (Variant)reader(this);
		}

		GD.PushError($"Unknown VarType: {type}");
		return new Variant();
	}

}
