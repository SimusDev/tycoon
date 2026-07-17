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
    public static string ModeToString(Mode mode)
    {
        return mode switch
        {
            Mode.Reliable => "reliable",
            Mode.Unreliable => "unreliable",
            Mode.UnreliableOrdered => "unreliable_ordered",
            _ => "",
        };
    }

    public static string PermissionToString(Permission permission)
    {
        return permission switch
        {
            Permission.Any => "any",
            Permission.Authority => "authority",
            Permission.Server => "server",
            _ => "",
        };
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



