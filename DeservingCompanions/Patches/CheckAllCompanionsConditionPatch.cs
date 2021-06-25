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
                    List<SkillObject> skills = new List<SkillObject>(shouldHaveAll.Keys);
                    foreach (SkillObject skill in skills)
                    {
                        shouldHaveAll[skill] = (int)Math.Round(shouldHaveAll[skill] * Settings.Instance.SkillRequirementsFactor);
                    }
                }
            if (shouldHaveOneOfThem is not null)
            {
                List<SkillObject> skills = new List<SkillObject>(shouldHaveOneOfThem.Keys);
                foreach (SkillObject skill in skills)
                {
                    shouldHaveOneOfThem[skill] = (int)Math.Round(shouldHaveOneOfThem[skill] * Settings.Instance.SkillRequirementsFactor);
                }
            }
        }
    }
}
