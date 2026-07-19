using Godot;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class GDNetRpcAttribute : Attribute
{
    public Permission Permission { get; set; }
    public Mode Mode { get; init; } = Mode.Reliable;
    public int Channel { get; init; }

    public const string PermissionStringServerOrAuth = "server_or_auth";
    public const string PermissionStringAny = "any";
    public const string ModeStringReliable = "reliable";
    public const string ModeStringUnreliable = "unreliable";
    public const string ModeStringUnreliableOrdered = "unreliable_ordered";

    public GDNetRpcAttribute(Permission permission = Permission.ServerOrAuth)
    {
        Permission = permission;
    }
    public static string ModeToString(Mode mode)
    {
        return mode switch
        {
            Mode.Reliable => ModeStringReliable,
            Mode.Unreliable => ModeStringUnreliable,
            Mode.UnreliableOrdered => ModeStringUnreliableOrdered,
            _ => "",
        };
    }

    public static string PermissionToString(Permission permission)
    {
        return permission switch
        {
            Permission.ServerOrAuth => PermissionStringServerOrAuth,
            Permission.Any => PermissionStringAny,
            _ => "",
        };
    }
}

public enum Permission
{
    ServerOrAuth,     
    Any,
}

public enum Mode
{
    Reliable,
    Unreliable,
    UnreliableOrdered,
}



