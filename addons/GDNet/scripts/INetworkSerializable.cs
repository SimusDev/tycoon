
namespace GDNetSerialization
{
    public interface INetworkSerializable
    {
        void Serialize(GDNetBufferO buffer) { }
        void Deserialize(GDNetBufferO buffer) { }

    }
}
