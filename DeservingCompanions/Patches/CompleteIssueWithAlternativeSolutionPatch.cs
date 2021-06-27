using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;

using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

using HarmonyLib;

namespace DeservingCompanions.Patchess
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

            if (companion is not null && companion.CompanionOf == Clan.PlayerClan && menLeaded > 0)
            {
                SubModule.Instance.Log.LogInformation($"Distributing additional XP to {companion.Name} after return from {__instance.GetType().Name}...");
                DistributeAdditionalXpRewards(__instance, companion, menLeaded);
            }
        }

        private static void DistributeAdditionalXpRewards(IssueBase issue, Hero companion, int menLeaded)
        {
            MethodInfo GetIssueDuration = typeof(IssueBase).GetMethod("get_AlternativeSolutionDurationInDays", BindingFlags.Instance | BindingFlags.NonPublic);
            int issueDuration = (int)GetIssueDuration.Invoke(issue, null);
            SubModule.Instance.Log.LogInformation($"{menLeaded} mens leaded for {issueDuration} days.");

            var shouldHaveAll = new Dictionary<SkillObject, int>();
            var shouldHaveOneOfThem = new Dictionary<SkillObject, int>();
            GetCompanionRequiredSkills(issue, out shouldHaveAll, out shouldHaveOneOfThem);

            // Keep track of the original reward as the additional rewards are based on it
            MethodInfo GetCompanionReward = typeof(IssueBase).GetMethod("get_CompanionSkillAndRewardXP", BindingFlags.Instance | BindingFlags.NonPublic);
            ValueTuple<SkillObject, int> originalReward = (ValueTuple<SkillObject, int>)GetCompanionReward.Invoke(issue, null);

            // Distribute additional XP rewards
            Dictionary<SkillObject, int> rewardFromRequiredSkill = XpRewardFromRequiredSkills(companion, originalReward, shouldHaveAll, shouldHaveOneOfThem, issueDuration);
            Dictionary<SkillObject, int> rewardFromLeadingTroops = XpRewardFromLeadingTroops(menLeaded, issueDuration);
            foreach (Dictionary<SkillObject, int> reward in new object [] {  rewardFromRequiredSkill, rewardFromLeadingTroops })
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

        // Base implementation seems to use GetAlternativeSolutionRequiredCompanionSkills() to fetch required skills.
        // However it's not consistent across the various issues (AlternativeSolutionCondition() must be checked).
        // Sometimes shouldHaveAll and shouldHaveOneOfThem dicts are used as out parameters
        // Sometimes only shouldHaveAll or shouldHaveOneOfThem is returned
        // Sometimes another method is used or values are hardcoded somewhere in the code...
        private static void GetCompanionRequiredSkills(IssueBase issue, out Dictionary<SkillObject, int> shouldHaveAll, out Dictionary<SkillObject, int> shouldHaveOneOfThem)
        {
            string issueType = issue.GetType().Name.ToString();

            shouldHaveAll = new Dictionary<SkillObject, int>();
            shouldHaveOneOfThem = new Dictionary<SkillObject, int>();

            switch (issueType)
            {
                case "CapturedByBountyHuntersIssue":
                case "CaravanAmbushIssue":
                case "ExtortionByDesertersIssue":
                case "LandlordTrainingForRetainersIssue":
                case "NearbyBanditBaseIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() uses shouldHaveAll and shouldHaveOneOfThem as out parameters
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                        object[] parameters = new object[] { shouldHaveAll, shouldHaveOneOfThem };
                        GetRequiredSkills.Invoke(issue, parameters);
                        shouldHaveAll = (Dictionary<SkillObject, int>)parameters[0];
                        shouldHaveOneOfThem = (Dictionary<SkillObject, int>)parameters[1];
                        break;
                    }
                case "ArtisanCantSellProductsAtAFairPriceIssue":
                case "ArtisanOverpricedGoodsIssue":
                case "EscortMerchantCaravanIssue":
                case "GangLeaderNeedsWeaponsIssue":
                case "HeadmanNeedsToDeliverAHerdIssue":
                case "LesserNobleRevoltIssue":
                case "LordNeedsGarrisonTroopsIssue":
                case "LordNeedsHorsesIssue":
                case "VillageNeedsToolsIssue":
                case "RuralNotableInnAndOutIssue":
                case "FamilyFeudIssue":
                case "NotableWantsDaughterFoundIssue":
                case "TheSpyPartyIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() returns shouldHaveAll
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                        shouldHaveAll = (Dictionary<SkillObject, int>)GetRequiredSkills.Invoke(issue, null);
                        break;
                    }
                case "LandLordNeedsManualLaborersIssue":
                case "MerchantNeedsHelpWithOutlawsIssue":
                case "RivalGangMovingInIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() returns shouldHaveOneOfThem
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                        shouldHaveOneOfThem = (Dictionary<SkillObject, int>)GetRequiredSkills.Invoke(issue, null);
                        break;
                    }
                case "GandLeaderNeedsRecruitsIssue":
                    // CompanionSkillRequirement returns shouldHaveOneOfThem
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("get_CompanionSkillRequirement", BindingFlags.Instance | BindingFlags.NonPublic);
                        shouldHaveOneOfThem = (Dictionary<SkillObject, int>)GetRequiredSkills.Invoke(issue, null);
                        break;
                    }
                case "LandLordTheArtOfTheTradeIssue":
                    // skill and value hardcoded in CompanionSkillCondition
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("get_CompanionRequiredSkillLevel", BindingFlags.Instance | BindingFlags.NonPublic);
                        int requiredSkillLevel = (int)GetRequiredSkills.Invoke(issue, null);
                        shouldHaveAll.Add(DefaultSkills.Trade, requiredSkillLevel);
                        break;
                    }
                case "MerchantArmyOfPoachersIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() returns shouldHaveAll
                    // GetAlternativeSolutionCompanionSkills() returns shouldHaveOneOfThem
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkill", BindingFlags.Instance | BindingFlags.NonPublic);
                        shouldHaveAll = (Dictionary<SkillObject, int>)GetRequiredSkills.Invoke(issue, null);
                        GetRequiredSkills = issue.GetType().GetMethod("GetAlternativeSolutionCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                        shouldHaveOneOfThem = (Dictionary<SkillObject, int>)GetRequiredSkills.Invoke(issue, null);
                        break;
                    }
                case "HeadmanVillageNeedsDraughtAnimalsIssue":
                    // GetAlternativeSolutionRequiredCompanionSkill() returns shouldHaveAll
                    {
                        MethodInfo GetRequiredSkills = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkill", BindingFlags.Instance | BindingFlags.NonPublic);
                        shouldHaveAll = (Dictionary<SkillObject, int>)GetRequiredSkills.Invoke(issue, null);
                        break;
                    }
                default:
                    // No requirement or unknown issue
                    SubModule.Instance.Log.LogInformation($"Issue {issueType} not supported.");
                    break;
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

        private static Dictionary<SkillObject, int> XpRewardFromRequiredSkills(Hero companion, ValueTuple<SkillObject, int> originalReward, Dictionary<SkillObject, int> shouldHaveAll, Dictionary<SkillObject, int> shouldHaveOneOfThem, int issueDuration)
        {
            var reward = new Dictionary<SkillObject, int>();

            if (shouldHaveAll.Count > 0)
            {
                // In shouldHaveAll, reward each skill
                foreach (KeyValuePair<SkillObject, int> requiredSkill in shouldHaveAll)
                {
                    reward.Add(requiredSkill.Key, (int)Math.Round((float)originalReward.Item2 * (float)issueDuration * 2f));
                }
            }

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
                reward.Add(skillToReward, (int)Math.Round((float)originalReward.Item2 * (float)issueDuration * 2f));
            }

            return reward;
        }
    }
}
