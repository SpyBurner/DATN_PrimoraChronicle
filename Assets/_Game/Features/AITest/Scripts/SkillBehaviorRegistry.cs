using System.Collections.Generic;

public static class SkillBehaviorRegistry
{
    private static readonly List<SkillBehavior> _all = new()
    {
        new CorruptedCrestBehavior(),
        new GraveclawFrenzyBehavior(),
        new DeathsTollBehavior(),
        new BloomBehavior(),
        new RootOvergrowBehavior(),
        new LifeSappingThornBehavior(),
        new MasteryOfFlameBehavior(),
        new SeveredTailBehavior(),
        new MoltenDiveBehavior(),
        new CurseOfAshBehavior()
    };

    public static IReadOnlyList<SkillBehavior> All => _all;

    public static SkillBehavior Get(string id)
    {
        foreach (var b in _all)
        {
            if (b.Id == id) return b;
        }
        return null;
    }

    public static SkillBehavior GetRandom()
    {
        return _all[UnityEngine.Random.Range(0, _all.Count)];
    }

    public static SkillBehavior[] GetRandomPair()
    {
        var first = UnityEngine.Random.Range(0, _all.Count);
        var second = UnityEngine.Random.Range(0, _all.Count - 1);
        if (second >= first) second++;

        return new[] { _all[first], _all[second] };
    }
}
