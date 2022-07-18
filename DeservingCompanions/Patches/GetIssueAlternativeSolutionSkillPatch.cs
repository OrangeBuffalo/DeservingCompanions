using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Issues;

namespace DeservingCompanions.Patches
{
    // Adjust the required skill values by a given factor.
    [HarmonyPatch(typeof(DefaultIssueModel), "GetIssueAlternativeSolutionSkill")]
    class GetIssueAlternativeSolutionSkillPatch
    {
        static void Postfix(ref ValueTuple<SkillObject, int> __result, Hero hero, IssueBase issue)
        {
            var original = issue.GetAlternativeSolutionRequiredCompanionSkill();
            var custom = new List<ValueTuple<SkillObject, int>> {};
            foreach((SkillObject, int) item in original) 
            {
                custom.Add(new ValueTuple<SkillObject, int> (item.Item1, (int)Math.Round(item.Item2 * Settings.Instance.SkillRequirementsFactor)));                
            }
            __result = custom.MaxBy((ValueTuple<SkillObject, int> x) => hero.GetSkillValue(x.Item1));
        }
    }
}
