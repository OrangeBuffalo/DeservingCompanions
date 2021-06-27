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
    // This is not gonna work for LandLordTheArtOfTheTrade issue that uses hardcoded values instead.
    [HarmonyPatch(typeof(QuestHelper))]
    [HarmonyPatch("CheckCompanionForAlternativeSolution")]
    [HarmonyPatch(new Type[] { typeof(CharacterObject), typeof(TextObject), typeof(Dictionary<SkillObject, int>), typeof(Dictionary<SkillObject, int>) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Ref, ArgumentType.Normal, ArgumentType.Normal })]
    class CheckCompanionForAlternativeSolutionPatch
    {
        static void Prefix(CharacterObject character, ref TextObject explanation, ref Dictionary<SkillObject, int> shouldHaveAll, ref Dictionary<SkillObject, int> shouldHaveOneOfThem)
        {
            foreach (var requirements in new List<Dictionary<SkillObject, int>> { shouldHaveAll, shouldHaveOneOfThem })
            {
                if (requirements is null)
                {
                    break;
                }

                if (requirements.Count > 0)
                {
                    var skills = new List<SkillObject>(requirements.Keys);
                    foreach (SkillObject skill in skills)
                    {
                        requirements[skill] = (int)Math.Round(requirements[skill] * Settings.Instance?.SkillRequirementsFactor ?? 1f);
                    }
                }
            }
        }
    }
}
