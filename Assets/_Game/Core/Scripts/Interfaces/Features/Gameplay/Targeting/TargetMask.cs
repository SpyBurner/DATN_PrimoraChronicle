using System;

[Flags]
public enum TargetMask
{
    None      = 0,
    Enemy     = 1,
    Ally      = 2,
    EmptyTile = 4,
    Self      = 8
}
