using Godot;
using System;
using System.Collections.Generic;
using System.Text;

[GlobalClass, Icon("")]
public partial class GDNetBuffer : RefCounted
{
	#region Enums

	private enum DataType : byte
	{
		Null = 0x01,
		BoolTrue,
		BoolFalse,
		Var,
		Int8,
		Int16,
		Int32,
		Int64,
		UInt64,
		Float,
		Double,
		String,
		Vector2,
		Vector3,
		ByteArrayEmpty,
		ByteArrayU8,
		ByteArrayU16,
		ByteArrayU32,
		ArraySimple,
		ArrayComplex,
		ArrayStart,
		ArrayEnd,
		DictVar,
		DictBuffer,
		DictStart,
		DictEnd,
		Resource,
		FullObject,
		Custom,
		NodeReference
	}

	private enum FullObjectType : byte
	{
		Object = 0x01,
		Node,
		Resource,
		RefCounted,
		Other
	}

	#endregion

	#region Constants

	private const string CustomSerializeMethod = "GDNetSerialize";
	private const string CustomDeserializeMethod = "GDNetDeserialize";

	#endregion

	#region Fields

	private readonly StreamPeerBuffer _stream = new();
	private readonly HashSet<string> _blockedWriteMethods = new();
	private readonly HashSet<string> _blockedReadMethods = new();

	private readonly Dictionary<Variant.Type, Action<Variant>> _writeMethods;
	private readonly Dictionary<DataType, Func<Variant>> _readMethods;

	#endregion

	#region Constructor

	public GDNetBuffer()
	{
		_writeMethods = new Dictionary<Variant.Type, Action<Variant>>
		{
			[Variant.Type.Nil] = v => WriteNull(),
			[Variant.Type.Bool] = v => WriteBool(v.As<bool>()),
			[Variant.Type.Int] = v => WriteInt(v.As<long>()),
			[Variant.Type.String] = v => WriteString(v.As<string>()),
			[Variant.Type.Vector3] = v => WriteVector3(v.As<Vector3>()),
			[Variant.Type.Vector2] = v => WriteVector2(v.As<Vector2>()),
			[Variant.Type.Float] = v => WriteFloat(v.As<float>()),
			[Variant.Type.PackedByteArray] = v => WriteBytes(v.As<byte[]>()),
			[Variant.Type.Array] = v => WriteArraySimple(v.As<Godot.Collections.Array>()),
			[Variant.Type.Object] = v => WriteObjectAuto(v.As<GodotObject>())
		};

		_readMethods = new Dictionary<DataType, Func<Variant>>
		{
			[DataType.Null] = () => ReadNull(),
			[DataType.Int8] = () => ReadInt8(),
			[DataType.Int16] = () => ReadInt16(),
			[DataType.Int32] = () => ReadInt32(),
			[DataType.Int64] = () => ReadInt64(),
			[DataType.UInt64] = () => ReadUInt64(),
			[DataType.BoolTrue] = () => ReadBool(),
			[DataType.BoolFalse] = () => ReadBool(),
			[DataType.Var] = () => ReadVar(),
			[DataType.String] = () => ReadString(),
			[DataType.Vector3] = () => ReadVector3(),
			[DataType.Vector2] = () => ReadVector2(),
			[DataType.Float] = () => ReadFloat(),
			[DataType.Double] = () => ReadFloat(),
			[DataType.ByteArrayEmpty] = () => ReadBytes(),
			[DataType.ByteArrayU8] = () => ReadBytes(),
			[DataType.ByteArrayU16] = () => ReadBytes(),
			[DataType.ByteArrayU32] = () => ReadBytes(),
			[DataType.ArraySimple] = () => ReadArraySimple(),
			[DataType.ArrayComplex] = () => ReadArrayComplex(),
			[DataType.Resource] = () => ReadResource(),
			[DataType.FullObject] = () => ReadFullObject(),
			[DataType.Custom] = () => ReadCustomObject(),
			[DataType.NodeReference] = () => ReadNodeReference()
		};
	}

	#endregion

	#region Public API

	public Godot.Collections.Array ToArray()
	{
		var result = new Godot.Collections.Array();
		int rememberedPosition = Position;
		Seek(0);

		while (_stream.GetAvailableBytes() > 0)
		{
			result.Add(Read());
		}

		Seek(rememberedPosition);
		return result;
	}

	public GDNetBuffer Write(Variant value)
	{
		if (_writeMethods.TryGetValue(value.VariantType, out Action<Variant> method))
		{
			method(value);
		}
		else
		{
			WriteVar(value);
		}

		return this;
	}

	public Variant Read()
	{
		DataType type = ReadType();
		_stream.Seek(_stream.GetPosition() - 1);

		if (_readMethods.TryGetValue(type, out Func<Variant> method))
		{
			return method();
		}

		GD.PushError($"Unknown type for auto-read: {type}");
		return default;
	}

	public GDNetBuffer WriteVar(Variant variant)
	{
		WriteType(DataType.Var);
		_stream.PutVar(variant);
		return this;
	}

	public Variant ReadVar()
	{
		DataType type = ReadType();
		Assert(type == DataType.Var, $"Expected Var, got {type}");
		return _stream.GetVar();
	}

	public GDNetBuffer SetBytes(byte[] data)
	{
		_stream.DataArray = data;
		return this;
	}

	public byte[] GetBytes() => _stream.DataArray;
	public int AvailableBytes => _stream.GetAvailableBytes();
	public int Position => _stream.GetPosition();
	public int Size => _stream.GetSize();

	public GDNetBuffer Seek(int position)
	{
		_stream.Seek(position);
		return this;
	}

	public GDNetBuffer Clear()
	{
		_stream.Clear();
		Seek(0);
		return this;
	}

	#endregion

	#region Write Methods

	private void WriteType(DataType type) => _stream.PutU8((byte)type);
	private DataType ReadType() => (DataType)_stream.GetU8();

	public GDNetBuffer WriteNull()
	{
		WriteType(DataType.Null);
		return this;
	}

	public Variant ReadNull()
	{
		ReadType();
		return default;
	}

	public GDNetBuffer WriteBool(bool value)
	{
		WriteType(value ? DataType.BoolTrue : DataType.BoolFalse);
		return this;
	}

	public bool ReadBool() => ReadType() == DataType.BoolTrue;

	public GDNetBuffer WriteInt8(sbyte value)
	{
		WriteType(DataType.Int8);
		_stream.Put8(value);
		return this;
	}

	public sbyte ReadInt8()
	{
		Assert(ReadType() == DataType.Int8, $"Expected Int8, got {ReadType()}");
		return _stream.Get8();
	}

	public GDNetBuffer WriteInt16(short value)
	{
		WriteType(DataType.Int16);
		_stream.Put16(value);
		return this;
	}

	public short ReadInt16()
	{
		Assert(ReadType() == DataType.Int16, $"Expected Int16, got {ReadType()}");
		return _stream.Get16();
	}

	public GDNetBuffer WriteInt32(int value)
	{
		WriteType(DataType.Int32);
		_stream.Put32(value);
		return this;
	}

	public int ReadInt32()
	{
		Assert(ReadType() == DataType.Int32, $"Expected Int32, got {ReadType()}");
		return _stream.Get32();
	}

	public GDNetBuffer WriteInt64(long value)
	{
		WriteType(DataType.Int64);
		_stream.Put64(value);
		return this;
	}

	public long ReadInt64()
	{
		Assert(ReadType() == DataType.Int64, $"Expected Int64, got {ReadType()}");
		return _stream.Get64();
	}

	public GDNetBuffer WriteInt(long value)
	{
		if (value is >= -128 and <= 127)
			WriteInt8((sbyte)value);
		else if (value is >= -32768 and <= 32767)
			WriteInt16((short)value);
		else if (value is >= -2147483648 and <= 2147483647)
			WriteInt32((int)value);
		else
			WriteInt64(value);

		return this;
	}

	public long ReadInt()
	{
		DataType type = ReadType();
		_stream.Seek(_stream.GetPosition() - 1);

		return _readMethods.TryGetValue(type, out Func<Variant> method)
			? method().As<long>()
			: 0;
	}

	public GDNetBuffer WriteUInt64(ulong value)
	{
		WriteType(DataType.UInt64);
		_stream.PutU64(value);
		return this;
	}

	public ulong ReadUInt64()
	{
		var type = ReadType();
		return _stream.GetU64();
	}

	public GDNetBuffer WriteBytes(byte[] value)
	{
		int length = 0;
		if (value != null)
			length = value.Length;

		switch (length)
		{
			case 0:
				WriteType(DataType.ByteArrayEmpty);
				break;
			case < 255:
				WriteType(DataType.ByteArrayU8);
				_stream.PutU8((byte)length);
				_stream.PutData(value);
				break;
			case < 65535:
				WriteType(DataType.ByteArrayU16);
				_stream.PutU16((ushort)length);
				_stream.PutData(value);
				break;
			default:
				WriteType(DataType.ByteArrayU32);
				_stream.PutU32((uint)length);
				_stream.PutData(value);
				break;
		}

		return this;
	}

	public byte[] ReadBytes()
	{
		DataType type = ReadType();

		return type switch
		{
			DataType.ByteArrayEmpty => Array.Empty<byte>(),
			DataType.ByteArrayU8 => (byte[])_stream.GetData(_stream.GetU8())[1],
			DataType.ByteArrayU16 => (byte[])_stream.GetData(_stream.GetU16())[1],
			DataType.ByteArrayU32 => (byte[])_stream.GetData((int)_stream.GetU32())[1],
			_ => AssertAndReturnEmpty($"Expected ByteArray, got {type}")
		};
	}

	public GDNetBuffer WriteString(string value)
	{
		WriteType(DataType.String);
		WriteBytes(Encoding.UTF8.GetBytes(value));
		return this;
	}

	public string ReadString()
	{
		Assert(ReadType() == DataType.String, $"Expected String, got {ReadType()}");
		return Encoding.UTF8.GetString(ReadBytes());
	}

	public GDNetBuffer WriteVector3(Vector3 value)
	{
		WriteType(DataType.Vector3);
		_stream.PutFloat(value.X);
		_stream.PutFloat(value.Y);
		_stream.PutFloat(value.Z);
		return this;
	}

	public Vector3 ReadVector3()
	{
		Assert(ReadType() == DataType.Vector3, $"Expected Vector3, got {ReadType()}");
		return new Vector3(_stream.GetFloat(), _stream.GetFloat(), _stream.GetFloat());
	}

	public GDNetBuffer WriteVector2(Vector2 value)
	{
		WriteType(DataType.Vector2);
		_stream.PutFloat(value.X);
		_stream.PutFloat(value.Y);
		return this;
	}

	public Vector2 ReadVector2()
	{
		Assert(ReadType() == DataType.Vector2, $"Expected Vector2, got {ReadType()}");
		return new Vector2(_stream.GetFloat(), _stream.GetFloat());
	}

	public GDNetBuffer WriteFloat(float value)
	{
		if (float.IsFinite(value))
		{
			WriteType(DataType.Float);
			_stream.PutFloat(value);
		}
		else
		{
			WriteType(DataType.Double);
			_stream.PutDouble(value);
		}

		return this;
	}

	public float ReadFloat()
	{
		DataType type = ReadType();

		return type switch
		{
			DataType.Float => _stream.GetFloat(),
			DataType.Double => (float)_stream.GetDouble(),
			_ => AssertAndReturn<float>($"Expected Float or Double, got {type}")
		};
	}

	public GDNetBuffer WriteArraySimple(Godot.Collections.Array array)
	{
		WriteType(DataType.ArraySimple);
		_stream.PutVar(array);
		return this;
	}

	public Godot.Collections.Array ReadArraySimple()
	{
		Assert(ReadType() == DataType.ArraySimple, $"Expected Array, got {ReadType()}");
		return _stream.GetVar().As<Godot.Collections.Array>();
	}

	public GDNetBuffer WriteArrayComplex(Godot.Collections.Array array)
	{
		WriteType(DataType.ArrayComplex);

		var buffer = new GDNetBuffer();
		buffer.WriteInt(array.Count);
		WriteArrayComplexInternal(buffer, array);

		WriteBytes(buffer.GetBytes());
		return this;
	}

	private void WriteArrayComplexInternal(GDNetBuffer buffer, Godot.Collections.Array array)
	{
		foreach (Variant value in array)
		{
			switch (value.VariantType)
			{
				case Variant.Type.Array:
					buffer.Write((byte)DataType.ArrayStart);
					WriteArrayComplexInternal(buffer, value.As<Godot.Collections.Array>());
					buffer.Write((byte)DataType.ArrayEnd);
					break;

				case Variant.Type.Dictionary:
					buffer.Write((byte)DataType.DictStart);
					buffer.Write((byte)DataType.DictEnd);
					break;

				default:
					buffer.Write(value);
					break;
			}
		}
	}

	public Godot.Collections.Array ReadArrayComplex()
	{
		Assert(ReadType() == DataType.ArrayComplex, $"Expected ArrayComplex, got {ReadType()}");

		var buffer = new GDNetBuffer();
		buffer.SetBytes(ReadBytes());

		int count = (int)buffer.ReadInt();
		var result = new Godot.Collections.Array();

		for (int i = 0; i < count; i++)
		{
			result.Add(ReadArrayElement(buffer));
		}

		return result;
	}

	private Variant ReadArrayElement(GDNetBuffer buffer)
	{
		Variant value = buffer.Read();

		if (value.VariantType == Variant.Type.Int && value.As<int>() == (int)DataType.ArrayStart)
		{
			var subArray = new Godot.Collections.Array();

			while (true)
			{
				Variant element = ReadArrayElement(buffer);

				if (element.VariantType == Variant.Type.Int && element.As<int>() == (int)DataType.ArrayEnd)
					break;

				subArray.Add(element);
			}

			return subArray;
		}

		if (value.VariantType == Variant.Type.Int && value.As<int>() == (int)DataType.DictStart)
		{
			return new Godot.Collections.Dictionary();
		}

		return value;
	}

	public GDNetBuffer WriteObjectAuto(GodotObject obj)
	{
		if (!IsInstanceValid(obj))
		{
			WriteNull();
			return this;
		}

		if (HasCustomSerialization(obj))
		{
			WriteCustomObject(obj);
			return this;
		}

		return obj switch
		{
			Node node when node.IsInsideTree() => WriteNodeReference(node),
			Resource resource => WriteResource(resource),
			_ => WriteFullObject(obj)
		};
	}

	public GDNetBuffer WriteNodeReference(Node node)
	{
		WriteType(DataType.NodeReference);
		WriteString(node.GetPath().ToString());
		return this;
	}

	public Node ReadNodeReference()
	{
		Assert(ReadType() == DataType.NodeReference, $"Expected NodeReference, got {ReadType()}");
		return GDNet.Instance.GetNode(ReadString());
	}

	public GDNetBuffer WriteResource(Resource resource)
	{
		long hashId = -1;

		if (!string.IsNullOrEmpty(resource.ResourcePath))
		{
			string uid = ResourceUid.PathToUid(resource.ResourcePath);
			hashId = ResourceUid.TextToId(uid);
		}

		if (hashId == -1)
		{
			return WriteFullObject(resource);
		}

		WriteType(DataType.Resource);
		WriteInt(hashId);

		return this;
	}

	public Resource ReadResource()
	{
		DataType type = ReadType();

		return type switch
		{
			DataType.Resource => GD.Load(ResourceUid.GetIdPath(ReadInt())),
			DataType.FullObject => (Resource)ReadFullObject(),
			_ => null
		};
	}

	public GDNetBuffer WriteFullObject(GodotObject obj)
	{
		WriteType(DataType.FullObject);

		FullObjectType type = obj switch
		{
			Node => FullObjectType.Node,
			Resource => FullObjectType.Resource,
			RefCounted => FullObjectType.RefCounted,
			GodotObject => FullObjectType.Object,
			_ => FullObjectType.Other
		};

		_stream.PutU8((byte)type);

		if (type == FullObjectType.Other)
		{
			WriteString(obj.GetClass());
		}

		bool hasScript = obj.GetScript().VariantType != Variant.Type.Nil;
		WriteBool(hasScript);

		if (hasScript)
		{
			WriteResource((Resource)obj.GetScript());
		}

		return this;
	}

	public GodotObject ReadFullObject()
	{
		Assert(ReadType() == DataType.FullObject, $"Expected FullObject, got {ReadType()}");

		FullObjectType type = (FullObjectType)_stream.GetU8();

		GodotObject obj = type switch
		{
			FullObjectType.Object => new GodotObject(),
			FullObjectType.Node => new Node(),
			FullObjectType.Resource => new Resource(),
			FullObjectType.RefCounted => new RefCounted(),
			FullObjectType.Other => (GodotObject)ClassDB.Instantiate(ReadString()),
			_ => null
		};

		if (ReadBool())
		{
			obj.SetScript(ReadResource());
		}

		return obj;
	}

	public GDNetBuffer WriteCustomObject(GodotObject obj)
	{
		WriteType(DataType.Custom);
		WriteResource((Resource)obj.GetScript());

		byte[] bytes = new byte[0];
		obj.Call(CustomSerializeMethod, bytes);

		WriteBytes(bytes);
		return this;
	}

	public GodotObject ReadCustomObject()
	{
		Assert(ReadType() == DataType.Custom, $"Expected Custom, got {ReadType()}");

		Resource script = ReadResource();
		byte[] bytes = ReadBytes();

		return (GodotObject)script.Call(CustomDeserializeMethod, bytes);
	}

	#endregion

	#region Blocking Methods

	public GDNetBuffer BlockWriteMethod(string methodName)
	{
		_blockedWriteMethods.Add(methodName.ToSnakeCase());
		return this;
	}

	public GDNetBuffer UnblockWriteMethod(string methodName)
	{
		_blockedWriteMethods.Remove(methodName.ToSnakeCase());
		return this;
	}

	public GDNetBuffer BlockReadMethod(string methodName)
	{
		_blockedReadMethods.Add(methodName.ToSnakeCase());
		return this;
	}

	public GDNetBuffer UnblockReadMethod(string methodName)
	{
		_blockedReadMethods.Remove(methodName.ToSnakeCase());
		return this;
	}

	public GDNetBuffer BlockWriteMethods()
	{
		foreach (var pair in _writeMethods)
		{
			BlockWriteMethod(pair.Value.Method.Name);
		}
		return this;
	}

	public GDNetBuffer UnblockWriteMethods()
	{
		foreach (var pair in _writeMethods)
		{
			UnblockWriteMethod(pair.Value.Method.Name);
		}
		return this;
	}

	public GDNetBuffer BlockReadMethods()
	{
		foreach (var pair in _readMethods)
		{
			BlockReadMethod(pair.Value.Method.Name);
		}
		return this;
	}

	public GDNetBuffer UnblockReadMethods()
	{
		foreach (var pair in _readMethods)
		{
			UnblockReadMethod(pair.Value.Method.Name);
		}
		return this;
	}

	public GDNetBuffer ClearBlockers()
	{
		_blockedWriteMethods.Clear();
		_blockedReadMethods.Clear();
		return this;
	}

	public GDNetBuffer ClearReadBlockers()
	{
		_blockedReadMethods.Clear();
		return this;
	}

	public GDNetBuffer ClearWriteBlockers()
	{
		_blockedWriteMethods.Clear();
		return this;
	}

	#endregion

	#region Private Helpers

	private bool HasCustomSerialization(GodotObject obj) =>
		obj.HasMethod(CustomSerializeMethod) && obj.HasMethod(CustomDeserializeMethod);

	private void Assert(bool condition, string message)
	{
		if (!condition)
		{
			GD.PushError(message);
		}
	}

	private byte[] AssertAndReturnEmpty(string message)
	{
		Assert(false, message);
		return Array.Empty<byte>();
	}

	private T AssertAndReturn<T>(string message)
	{
		Assert(false, message);
		return default;
	}

	#endregion
}
