using Fusion;

public struct TargetingRequest
{
    public TargetMask Mask;
    public int Range;
    public string DisplayPattern;
    public NetworkId Caster;
    public bool IgnorePathfinding;
    public string TargetPatternSkillId; // if set, subsystem resolves target_pattern from this skill ID
    public string TargetPatternCardId;  // if set, subsystem resolves n_atk_pattern from this card ID
}
