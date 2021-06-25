using System;
using System.Collections.Generic;

using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Localization;
using Helpers;

using HarmonyLib;

namespace DeservingCompanions.Patches
{

    // Skill requirements are checked using two dictionaries: shouldHaveAll and shouldHaveOneOfThem
    // Both dictionaries are prefix-patched to adjust the skill values by a given factor.
    [HarmonyPatch(typeof(QuestHelper), "CheckAllCompanionsCondition")]
    class CheckAllCompanionsConditionPatch
    {
        static void Prefix(TroopRoster troopRoster, ref TextObject explanation, ref Dictionary<SkillObject, int> shouldHaveAll, ref Dictionary<SkillObject, int> shouldHaveOneOfThem)
        {
            if (shouldHaveAll is not null)
            {
                foreach (KeyValuePair<SkillObject, int> requiredSkill in shouldHaveAll)
                {
                    shouldHaveAll[requiredSkill.Key] = (int)Math.Round(requiredSkill.Value * Settings.Instance.SkillRequirementsFactor);
                }
            }
            if (shouldHaveOneOfThem is not null)
            {
                foreach (KeyValuePair<SkillObject, int> requiredSkill in shouldHaveOneOfThem)
                {
                    shouldHaveOneOfThem[requiredSkill.Key] = (int)Math.Round(requiredSkill.Value * Settings.Instance.SkillRequirementsFactor);
                }
            }
        }
    }
}
