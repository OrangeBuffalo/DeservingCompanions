using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using HarmonyLib;
using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Issues;

namespace DeservingCompanions.Patches
{
    // Harmony should not be needed in that case, as there is an OnIssueUpdated event we can listen to.
    // However, some crucial fields are cleared from the IssueBase object before sending the event, such as
    // AlternativeSolutionHero and AlternativeSolutionSentTroops.
    [HarmonyPatch(typeof(IssueBase), "CompleteIssueWithAlternativeSolution")]
    internal class CompleteIssueWithAlternativeSolutionPatch
    {

        // Prefix patch is required to read AlternativeSolutionHero and AlternativeSolutionSentTroops
        // Both fields are cleared by CompleteIssueWithAlternativeSolution.
        private static void Prefix(IssueBase __instance, out ValueTuple<Hero, int> __state)
        {
            __state.Item1 = __instance.AlternativeSolutionHero;
            __state.Item2 = __instance.AlternativeSolutionSentTroops.TotalManCount - 1;
        }

        private static void Postfix(IssueBase __instance, ValueTuple<Hero, int> __state)
        {
            Hero companion = __state.Item1;
            int menLeaded = __state.Item2;

            if (companion is not null && companion.Clan == Clan.PlayerClan && menLeaded > 0)
            {
                SubModule.Instance.Log.LogInformation($"Distributing additional XP to {companion.Name} after return from {__instance.GetType().Name}...");
                DistributeAdditionalXpRewards(__instance, companion, menLeaded);
            }
        }

        private static void DistributeAdditionalXpRewards(IssueBase issue, Hero companion, int menLeaded)
        {
            int issueDuration = (int)issue.GetTotalAlternativeSolutionDurationInDays(companion);
            SubModule.Instance.Log.LogInformation($"{menLeaded} mens leaded for {issueDuration} days.");

            var shouldHaveOneOfThem = new Dictionary<SkillObject, int>();
            List<(SkillObject, int)> list = issue.GetAlternativeSolutionRequiredCompanionSkill();
            foreach ((SkillObject, int) item in issue.GetAlternativeSolutionRequiredCompanionSkill())
            {
                shouldHaveOneOfThem.Add(item.Item1, item.Item2);
            }

            // Keep track of the original reward as the additional rewards are based on it
            MethodInfo GetCompanionReward = typeof(IssueBase).GetMethod("get_CompanionSkillRewardXP", BindingFlags.Instance | BindingFlags.NonPublic);
            int originalReward = (int)GetCompanionReward.Invoke(issue, null);

            // Distribute additional XP rewards
            Dictionary<SkillObject, int> rewardFromRequiredSkill = XpRewardFromRequiredSkills(companion, originalReward, shouldHaveOneOfThem, issueDuration);
            Dictionary<SkillObject, int> rewardFromLeadingTroops = XpRewardFromLeadingTroops(menLeaded, issueDuration);
            foreach (Dictionary<SkillObject, int> reward in new object[] { rewardFromRequiredSkill, rewardFromLeadingTroops })
            {
                if (reward.Count > 0)
                {
                    foreach (var skillReward in reward)
                    {
                        int xp = AdjustXpReward(skillReward.Key, skillReward.Value);
                        companion.AddSkillXp(skillReward.Key, xp);
                        SubModule.Instance.Log.LogInformation($"Rewarded {xp} {skillReward.Key.Name} XP.");
                    }
                }
            }
        }

        private static int AdjustXpReward(SkillObject skill, int xp)
        {
            // Randomize XP amount
            float xpF = (float)xp;
            xpF = (xpF * MBRandom.RandomFloatRanged(0.75f, 1.25f));

            // Apply adjusting factor from settings
            xpF = xpF * Settings.Instance?.XpGainsFactor ?? 1f;

            return (int)Math.Round(xpF);
        }

        private static Dictionary<SkillObject, int> XpRewardFromLeadingTroops(int menLeaded, int issueDuration)
        {
            float menLeadedF = (float)menLeaded;
            float issueDurationF = (float)issueDuration;
            var reward = new Dictionary<SkillObject, int>();

            if (menLeaded <= 0 || issueDuration <= 0)
            {
                return reward;
            }

            if (MBRandom.RandomFloatRanged(0, 1) < Settings.Instance.LeadershipProba)
            {
                reward.Add(DefaultSkills.Leadership, (int)Math.Round(menLeadedF * issueDurationF * 10f));
            }

            if (MBRandom.RandomFloatRanged(0, 1) < Settings.Instance.StewardProba)
            {
                reward.Add(DefaultSkills.Steward, (int)Math.Round(menLeadedF * issueDurationF * 10f));
            }

            if (MBRandom.RandomFloatRanged(0, 1) < Settings.Instance.ScoutingProba)
            {
                reward.Add(DefaultSkills.Scouting, (int)Math.Round(menLeadedF * issueDurationF * 10f));
            }

            if (MBRandom.RandomFloatRanged(0, 1) < Settings.Instance.MedicineProba)
            {
                reward.Add(DefaultSkills.Medicine, (int)Math.Round(menLeadedF * issueDurationF * 10f));
            }

            if (MBRandom.RandomFloatRanged(0, 1) < Settings.Instance.TacticsProba)
            {
                reward.Add(DefaultSkills.Tactics, (int)Math.Round(menLeadedF * issueDurationF * 10f));
            }

            return reward;
        }

        private static Dictionary<SkillObject, int> XpRewardFromRequiredSkills(Hero companion, int originalReward, Dictionary<SkillObject, int> shouldHaveOneOfThem, int issueDuration)
        {
            var reward = new Dictionary<SkillObject, int>();

            if (shouldHaveOneOfThem.Count > 0)
            {
                // In shouldHaveOneOfThem, reward the skill for which the hero has the highest value
                int heroSkillValue = 0;
                SkillObject skillToReward = shouldHaveOneOfThem.GetRandomElementInefficiently().Key;
                foreach (KeyValuePair<SkillObject, int> requiredSkill in shouldHaveOneOfThem)
                {

                    int _heroSkillValue = companion.GetSkillValue((SkillObject)requiredSkill.Key);
                    if (_heroSkillValue > heroSkillValue)
                    {
                        heroSkillValue = _heroSkillValue;
                        skillToReward = requiredSkill.Key;
                    }
                }
                reward.Add(skillToReward, (int)Math.Round((float)originalReward * (float)issueDuration * 2f));
            }

            return reward;
        }
    }
}
