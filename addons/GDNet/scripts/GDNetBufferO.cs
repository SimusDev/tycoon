using Godot;
using System;

[GlobalClass]
public partial class GDNetBufferO : RefCounted
{
    private GDNetStream _stream = null;

    public GDNetBufferO()
    {
        _stream = new GDNetStream();
    }
    internal enum VarType: byte
    {
        Null,
        GodotVar,

        Int8,
        Int16,
        Int32,
        Int64,

        UInt8,
        UInt16,
        UInt32,
        UInt64,

    }

    public void Seek(int position) => _stream.Seek(position);
    public int Position => _stream.Position;
    public int Length => _stream.Length;

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

    public void WriteFullNodeRef(Node node)
    {
        WriteString(node.GetPath().ToString());
    }

    public Node ReadFullNodeRef()
    {
        return GDNet.Instance.GetNode(ReadString());
    }

    public void WriteLong(long value)
    {
        ulong zigzag = (ulong)((value << 1) ^ (value >> 63));

        while (zigzag >= 0x80)
        {
            WriteByte((byte)(zigzag | 0x80));
            zigzag >>= 7;
        }
        WriteByte((byte)zigzag);
    }

    public long ReadLong()
    {
        ulong result = 0;
        int shift = 0;
        byte b;

        do
        {
            b = ReadByte();
            result |= (ulong)(b & 0x7F) << shift;
            shift += 7;

            if (shift > 63)
                throw new InvalidOperationException("VLQ too long!");

        } while ((b & 0x80) != 0);

        return (long)((result >> 1) ^ (ulong)(-(long)(result & 1)));
    }

    public void WriteBytes(byte[] bytes) => _stream.WriteBytes(bytes);
    public byte[] ReadBytes(int count) => _stream.ReadBytes(count);

    public void WriteBytesDynamic(byte[] bytes)
    {
        WriteLong(bytes.Length);
        _stream.WriteBytes(bytes);
    }

    public byte[] ReadBytesDynamic()
    {
        return _stream.ReadBytes((int)ReadLong());
    }

    private void _WriteVarType(VarType type)
    {
        _stream.WriteByte((byte)type);
    }

    private VarType _ReadVarType()
    {
        return (VarType)_stream.ReadByte();
    }

    public void Write(object value)
    {
        if (value == null)
        {
            _WriteVarType(VarType.Null);
            return;
        }

        TypeCode type = Type.GetTypeCode(value.GetType());

        switch (type)
        {
            case TypeCode.Object:
                if (value is Variant)
                {
                    _WriteVarType(VarType.GodotVar);
                    WriteVar((Variant)value);
                }
                else
                {
                    _WriteVarType(VarType.Null);
                }

                break;
            case TypeCode.SByte:
                _WriteVarType(VarType.Int8);
                WriteInt8((sbyte)value);
                break;
            case TypeCode.Byte:
                _WriteVarType(VarType.UInt8);
                WriteUInt8((byte)value);
                break;
            case TypeCode.Int16:
                _WriteVarType(VarType.Int16);
                WriteInt16((short)value);
                break;
            case TypeCode.UInt16:
                _WriteVarType(VarType.UInt16);
                WriteUInt16((ushort)value);
                break;
           
        }

    }

    public object Read()
    {
        VarType type = _ReadVarType();
        return null;
    }

    public void WriteObject(object value)
    {

    }

    public object ReadObject()
    {
        return null;
    }

    public void WriteVar(Variant variant)
    {

    }

    public Variant ReadVar()
    {
        return new Variant();
    }

}
