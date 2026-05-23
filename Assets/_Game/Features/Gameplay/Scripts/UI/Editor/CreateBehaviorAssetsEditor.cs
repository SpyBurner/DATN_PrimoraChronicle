#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CreateBehaviorAssetsEditor
{
    [MenuItem("Tools/Primora/Create F7 Behavior Assets")]
    public static void CreateAll()
    {
        EnsureFolders();
        CreateSkills();
        CreateStatusEffects();
        CreateMainPhaseSpells();
        CreateEvolutions();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[F7] Behavior assets created.");
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Resources");
        EnsureFolder("Assets/Resources/Behaviors");
        EnsureFolder("Assets/Resources/Behaviors/Skills");
        EnsureFolder("Assets/Resources/Behaviors/StatusEffects");
        EnsureFolder("Assets/Resources/Behaviors/MainPhaseSpells");
        EnsureFolder("Assets/Resources/Behaviors/Evolutions");
    }

    static void EnsureFolder(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            int slash = path.LastIndexOf('/');
            AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
        }
    }

    static void CreateSkills()
    {
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_CorruptedCrest.asset",
            s => { s.behaviorId = "skb_corrupted_crest"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_GraveclawFrenzy.asset",
            s => { s.behaviorId = "skb_graveclaw_frenzy"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_DeathsToll.asset",
            s => { s.behaviorId = "skb_deaths_toll"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_Cemetary.asset",
            s => { s.behaviorId = "skb_cemetary"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_Arise.asset",
            s => { s.behaviorId = "skb_arise"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_GroveheartsAscendance.asset",
            s => { s.behaviorId = "skb_grovehearts_ascendance"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_Sprout.asset",
            s => { s.behaviorId = "skb_sprout"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_Bloom.asset",
            s => { s.behaviorId = "skb_bloom"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_RootOvergrow.asset",
            s => { s.behaviorId = "skb_root_overgrow"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_DeepWoodsEntangle.asset",
            s => { s.behaviorId = "skb_deep_woods_entangle"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_NaturesGift.asset",
            s => { s.behaviorId = "skb_natures_gift"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_LifeSappingThorn.asset",
            s => { s.behaviorId = "skb_life_sapping_thorn"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_WildGrowth.asset",
            s => { s.behaviorId = "skb_wild_growth"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_SporeBurst.asset",
            s => { s.behaviorId = "skb_spore_burst"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_BarkskinWard.asset",
            s => { s.behaviorId = "skb_barkskin_ward"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_SummonSeedling.asset",
            s => { s.behaviorId = "skb_summon_seedling"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_MasteryOfFlame.asset",
            s => { s.behaviorId = "skb_mastery_of_flame"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_SeveredTail.asset",
            s => { s.behaviorId = "skb_severed_tail"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_BannerOfCinders.asset",
            s => { s.behaviorId = "skb_banner_of_cinders"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_Firetrap.asset",
            s => { s.behaviorId = "skb_firetrap"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_MoltenDive.asset",
            s => { s.behaviorId = "skb_molten_dive"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_CurseOfAsh.asset",
            s => { s.behaviorId = "skb_curse_of_ash"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_LegionsLastStand.asset",
            s => { s.behaviorId = "skb_legions_last_stand"; });
        Create<GenericCombatSkillBehaviorSO>("Assets/Resources/Behaviors/Skills/SKB_MarchOfEmbers.asset",
            s => { s.behaviorId = "skb_march_of_embers"; });
    }

    static void CreateStatusEffects()
    {
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_Burning.asset",
            s => { s.effectId = "burning"; s.damagePerTurn = 10; s.interceptAmount = 0; });
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_Melting.asset",
            s => { s.effectId = "melting"; s.damagePerTurn = 20; s.interceptAmount = 0; });
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_BarkskinWard.asset",
            s => { s.effectId = "barkskin_ward"; s.damagePerTurn = 0; s.interceptAmount = 15; });
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_Decay.asset",
            s => { s.effectId = "decay"; s.damagePerTurn = 0; s.interceptAmount = 0; s.preventsHealing = true; });
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_Rooted.asset",
            s => { s.effectId = "rooted"; s.damagePerTurn = 0; s.interceptAmount = 0; s.preventsMovement = true; });
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_BurningTrail.asset",
            s => { s.effectId = "burning_trail"; s.damagePerTurn = 0; s.interceptAmount = 0; s.leavesTrailOnMove = true; s.trailTileEffectId = "melting"; });
        Create<GenericStatusEffectBehaviorSO>("Assets/Resources/Behaviors/StatusEffects/SE_LegionsBuff.asset",
            s => { s.effectId = "legions_buff"; s.damagePerTurn = 0; s.interceptAmount = 0; });
    }

    static void CreateMainPhaseSpells()
    {
        // mpsb_call_of_death: "Draw 3 cards, take 1 DMG" — self-cast, no tile target
        Create<GenericMainPhaseSpellBehaviorSO>("Assets/Resources/Behaviors/MainPhaseSpells/MPS_CallOfDeath.asset",
            s => { s.behaviorId = "mpsb_call_of_death"; });
        // mpsb_back_to_the_grave: "Take 1 DMG, deal 1 DMG to all enemy champions" — global, no tile target
        Create<GenericMainPhaseSpellBehaviorSO>("Assets/Resources/Behaviors/MainPhaseSpells/MPS_BackToTheGrave.asset",
            s => { s.behaviorId = "mpsb_back_to_the_grave"; });
        // mpsb_transplant: "Move all tile effects of a selected tile up to 1 hex range" — targets a tile
        Create<GenericMainPhaseSpellBehaviorSO>("Assets/Resources/Behaviors/MainPhaseSpells/MPS_Transplant.asset",
            s => { s.behaviorId = "mpsb_transplant"; });
    }

    static void CreateEvolutions()
    {
        Create<GenericEvolutionBehaviorSO>("Assets/Resources/Behaviors/Evolutions/EVO_SeedlingToSapling.asset",
            s => { s.behaviorId = "evo_seedling_sapling"; s.requiredStacks = 4; s.nextFormCardId = "troop_sapling"; });
        Create<GenericEvolutionBehaviorSO>("Assets/Resources/Behaviors/Evolutions/EVO_SaplingToYoungTreant.asset",
            s => { s.behaviorId = "evo_sapling_young_treant"; s.requiredStacks = 4; s.nextFormCardId = "troop_young_treant"; });
        Create<GenericEvolutionBehaviorSO>("Assets/Resources/Behaviors/Evolutions/EVO_YoungTreantToThornColossus.asset",
            s => { s.behaviorId = "evo_young_treant_thorn_colossus"; s.requiredStacks = 4; s.nextFormCardId = "troop_thorn_colossus"; });
    }

    static void Create<T>(string path, System.Action<T> configure) where T : ScriptableObject
    {
        if (AssetDatabase.LoadAssetAtPath<T>(path) != null)
            return;
        try
        {
            var asset = ScriptableObject.CreateInstance<T>();
            configure(asset);
            AssetDatabase.CreateAsset(asset, path);
            Debug.Log($"[F7] Created {path}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[F7] Failed to create {path}: {ex}");
        }
    }
}
#endif
