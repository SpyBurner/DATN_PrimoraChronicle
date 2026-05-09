using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Core.GDS
{
    [Serializable]
    public class HexCoordinate
    {
        public int n;
        public int p;
        public int q;
    }

    [Serializable]
    public class GDSMetadata
    {
        public string version;
        public string checksum;
        public string exported_at;
    }

    [Serializable]
    public class CardData
    {
        public string string_id;
        public string name;
        public string faction;
        public string rarity;
        public string description;
        public int mana_cost;
        public string type;

        // Unit specific
        public int hp;
        public int death_anchor;
        public float speed;
        public int n_atk_dmg;
        public List<HexCoordinate> n_atk_pattern;
        public string grants_skill;
        public bool is_summonable;

        // Champion specific
        public List<List<object>> grants_cards; // [[quantity, card_id], ...]

        // Spell specific
        public string main_phase_spell_behavior_id;
        public List<string> grants_skills;
        public List<StatusEffectRef> grants_status_effects;
    }

    [Serializable]
    public class StatusEffectRef
    {
        public string string_id;
        public int duration;
    }

    [Serializable]
    public class SkillData
    {
        public string string_id;
        public string name;
        public string description;
        public string type;
        public bool one_time;
        public int cooldown;
        public int target_condition;
        public List<StatusEffectRef> status_effects;
        public List<HexCoordinate> target_pattern;
        public List<HexCoordinate> display_pattern;
        public string skill_behavior_id;
    }

    [Serializable]
    public class StatusEffectData
    {
        public string string_id;
        public string name;
        public string description;
        public string type;
        public int max_stack;
        public List<HexCoordinate> effect_pattern;
        public string status_effect_behavior_id;
    }

    [Serializable]
    public class MasterGDSData
    {
        public GDSMetadata metadata;
        public Dictionary<string, CardData> cards;
        public Dictionary<string, SkillData> skills;
        public Dictionary<string, StatusEffectData> effects;
    }

    [Serializable]
    public class GDSResponse
    {
        public MasterGDSData data;
    }
}
