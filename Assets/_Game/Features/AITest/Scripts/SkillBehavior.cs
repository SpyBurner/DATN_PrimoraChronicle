public abstract class SkillBehavior
{
    public abstract string Id { get; }
    public abstract string Name { get; }
    public abstract int DefaultCooldown { get; }
    public abstract bool IsOneTime { get; }

    public abstract void Execute(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects);

    public virtual float EvaluateValue(Unit caster, Unit target, Tile targetTile, BoardController board, EffectController effects)
    {
        return caster.Attack;
    }

    protected int HexDistance(int p1, int q1, int p2, int q2)
    {
        int r1 = -p1 - q1;
        int r2 = -p2 - q2;
        return (System.Math.Abs(p1 - p2) + System.Math.Abs(q1 - q2) + System.Math.Abs(r1 - r2)) / 2;
    }
}
