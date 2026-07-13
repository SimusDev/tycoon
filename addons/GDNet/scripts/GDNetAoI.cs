using Godot;
using System.Collections.Generic;

[GlobalClass]
public partial class GDNetAoI : RefCounted
{
    public const string META = "GDNetVisibility";

    public HashSet<long> Peers = new HashSet<long>();

    public static GDNetAoI GetOrCreate(GodotObject obj)
    {
        if (IsInstanceValid(obj))
        {
            GDNetAoI visibility = new GDNetAoI();

            if (!obj.HasMeta(META))
            {
                SetIn(obj, visibility);
            }

            return (GDNetAoI)obj.GetMeta(META);
        }

        return new GDNetAoI();
    }

    public static GDNetAoI SetIn(GodotObject obj, GDNetAoI visibility)
    {
        obj.SetMeta(META, visibility);
        return visibility;
    }

    public static GDNetAoI TryFindIn(GodotObject obj)
    {
        if (IsInstanceValid(obj))
        {
            if (obj.HasMeta(META))
            {
                return (GDNetAoI)obj.GetMeta(META);
            }
        }

        return null;
    }

    public bool IsVisibleFor(long peer)
    {
        if (Peers.Count == 0)
            return true;

        return Peers.Contains(peer);
    }

    public bool IsPublicVisible()
    {
        return Peers.Count == 0;
    }

    public GDNetAoI SetVisibleFor(long peer, bool visible)
    {
        if (visible)
        {
            Peers.Add(peer);
        }
        else
        {
            Peers.Remove(peer);
        }

        return this;
    }


}