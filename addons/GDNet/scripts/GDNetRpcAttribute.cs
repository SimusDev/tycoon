using Godot;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class GDNetRpcAttribute : Attribute
{
    public Permission Permission { get; set; }
    public Mode Mode { get; init; } = Mode.Reliable;
    public int Channel { get; init; }

    public GDNetRpcAttribute(Permission permission = Permission.Server)
    {
        Permission = permission;
    }
}

public enum Permission
{
    Server,    
    Authority,     
    Any,        
}

public enum Mode
{
    Reliable,
    Unreliable,
    UnreliableOrdered,
}


