using Fusion;

public struct TargetingRequest
{
    public TargetMask Mask;
    public int Range;
    public string DisplayPattern;
    public NetworkId Caster;
    public bool IgnorePathfinding;
}
