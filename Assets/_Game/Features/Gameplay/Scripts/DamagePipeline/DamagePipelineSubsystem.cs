public class DamagePipelineSubsystem : IDamagePipelineSubsystem
{
    public void Initialize() { }

    public void Dispose() { }

    // Track A: implement the 3-pass pipeline (Aggregate → Intercept → Commit).
    // Intercept pass: tile effects evaluated before unit status effects.
    // Returns final damage after all modifiers.
    public int Resolve(DamageContext context) => context.RawAmount;
}
