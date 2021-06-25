using System;
using System.Collections.Generic;
using System.Reflection;

using TaleWorlds.Core;
using TaleWorlds.CampaignSystem;

using HarmonyLib;

namespace DeservingCompanions.Patches
{

    // Harmony should not be needed in that case, as there is an OnIssueUpdated event we can listen to.
    // However, some crucial fields are cleared from the IssueBase object before sending the event, such as
    // AlternativeSolutionHero and AlternativeSolutionSentTroops.

    [HarmonyPatch(typeof(IssueBase), "CompleteIssueWithAlternativeSolution")]
    class CompleteIssueWithAlternativeSolutionPatch
    {

        // Prefix patch is required to read AlternativeSolutionHero and AlternativeSolutionSentTroops
        // Both fields are cleared by CompleteIssueWithAlternativeSolution.
        static void Prefix(IssueBase __instance, out ValueTuple<Hero, int> __state)
        {
            __state.Item1 = __instance.AlternativeSolutionHero;
            __state.Item2 = __instance.AlternativeSolutionSentTroops.TotalManCount - 1;
        }

        static void Postfix(IssueBase __instance, ValueTuple<Hero, int> __state)
        {
            Hero companion = __state.Item1;
            int menLeaded = __state.Item2;

            if (companion is not null && companion.CompanionOf == Clan.PlayerClan && menLeaded > 0)
            {
                DistributeAdditionalXpRewards(__instance, companion, menLeaded);
            }
        }

        static void DistributeAdditionalXpRewards(IssueBase issue, Hero companion, int menLeaded)
        {
            MethodInfo GetIssueDuration = typeof(IssueBase).GetMethod("get_AlternativeSolutionDurationInDays", BindingFlags.Instance | BindingFlags.NonPublic);
            int issueDuration = (int)GetIssueDuration.Invoke(issue, null);

            var shouldHaveAll = new Dictionary<SkillObject, int>();
            var shouldHaveOneOfThem = new Dictionary<SkillObject, int>();
            GetCompanionRequiredSkills(issue, out shouldHaveAll, out shouldHaveOneOfThem);

            // Keep track of the original reward as the additional rewards are based on it
            MethodInfo GetCompanionReward = typeof(IssueBase).GetMethod("get_CompanionSkillAndRewardXP", BindingFlags.Instance | BindingFlags.NonPublic);
            ValueTuple<SkillObject, int> originalReward = (ValueTuple<SkillObject, int>)GetCompanionReward.Invoke(issue, null);

            // Additional XP reward from required and optional skills
            Dictionary<SkillObject, int> reward1 = XpRewardFromRequiredSkills(companion, originalReward, shouldHaveAll, shouldHaveOneOfThem, issueDuration);
            if (reward1 is not null)
            {
                foreach (var skillReward in reward1)
                {
                    companion.AddSkillXp(skillReward.Key, AdjustXpReward(originalReward, skillReward.Key, skillReward.Value));
                }
            }

            // Additional XP reward from leading troops
            Dictionary<SkillObject, int> reward2 = XpRewardFromLeadingTroops(menLeaded, issueDuration);
            if (reward2 is not null)
            {
                foreach (var skillReward in reward2)
                {
                    companion.AddSkillXp(skillReward.Key, AdjustXpReward(originalReward, skillReward.Key, skillReward.Value));
                }
            }
        }

        // Base implementation seems to use GetAlternativeSolutionRequiredCompanionSkills() to fetch required skills.
        // However it's not consistent across the various issues (AlternativeSolutionCondition() must be checked).
        // Sometimes shouldHaveAll and shouldHaveOneOfThem dicts are used as out parameters
        // Sometimes only shouldHaveAll or shouldHaveOneOfThem is returned
        // Sometimes another method is used or values are hardcoded somewhere in the code...
        static void GetCompanionRequiredSkills(IssueBase issue, out Dictionary<SkillObject, int> shouldHaveAll, out Dictionary<SkillObject, int> shouldHaveOneOfThem)
        {
            string issueType = issue.GetType().Name.ToString();
            shouldHaveAll = new Dictionary<SkillObject, int>();
            shouldHaveOneOfThem = new Dictionary<SkillObject, int>();

            switch (issueType)
            {
                case "CapturedByBountyHunterIssue":
                case "CaravanAmbushIssue":
                case "ExtertionByDesertersIssue":
                case "LandLordTrainingForRetainersIssue":
                case "NearbyBanditBaseIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() uses shouldHaveAll and shouldHaveOneOfThem as out parameters
                    MethodInfo GetRequiredSkills1 = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                    object[] parameters = new object[] { shouldHaveAll, shouldHaveOneOfThem };
                    GetRequiredSkills1.Invoke(issue, parameters);
                    shouldHaveAll = (Dictionary<SkillObject, int>)parameters[0];
                    shouldHaveOneOfThem = (Dictionary<SkillObject, int>)parameters[1];
                    break;
                case "ArtisanCantSellProductsAtFairPriceIssue":
                case "ArtisanOverpricedGoodsIssue":
                case "EscortMerchantCaravanIssue":
                case "GangLeaderNeedsWeaponsIssue":
                case "HeadmanNeedsToDeliverAHerdIssue":
                case "HeadmanVillageNeedsDraughtAnimalsIssue":
                case "LesserNobleRevoltIssue":
                case "LordNeedsGarrisonTroopsIssue":
                case "LordNeedsHorsesIssue":
                case "VillageNeedsToolsIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() returns shouldHaveAll
                    MethodInfo GetRequiredSkills2 = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                    shouldHaveAll = (Dictionary<SkillObject, int>)GetRequiredSkills2.Invoke(issue, null);
                    break;
                case "LandLordNeedsManualLaborersIssue":
                case "MerchantNeedsHelpWithOutlawsIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() returns shouldHaveOneOfThem
                    MethodInfo GetRequiredSkills3 = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                    shouldHaveOneOfThem = (Dictionary<SkillObject, int>)GetRequiredSkills3.Invoke(issue, null);
                    break;
                case "GandLeaderNeedsRecruitIssue":
                    // CompanionSkillRequirement returns shouldHaveOneOfThem
                    MethodInfo GetRequiredSkills4 = issue.GetType().GetMethod("get_CompanionSkillRequirement", BindingFlags.Instance | BindingFlags.NonPublic);
                    shouldHaveOneOfThem = (Dictionary<SkillObject, int>)GetRequiredSkills4.Invoke(issue, null);
                    break;
                case "LandLordTheArtOfTheTradeIssue":
                    // skill and value hardcoded in CompanionSkillCondition
                    MethodInfo GetRequiredSkills5 = issue.GetType().GetMethod("get_CompanionRequiredSkillLevel", BindingFlags.Instance | BindingFlags.NonPublic);
                    int requiredSkillLevel = (int)GetRequiredSkills5.Invoke(issue, null);
                    shouldHaveAll.Add(DefaultSkills.Trade, requiredSkillLevel);
                    break;
                case "MerchantArmyOfPoacherIssue":
                    // GetAlternativeSolutionRequiredCompanionSkills() returns shouldHaveAll
                    // GetAlternativeSolutionCompanionSkills() returns shouldHaveOneOfThem
                    MethodInfo GetRequiredSkills6 = issue.GetType().GetMethod("GetAlternativeSolutionRequiredCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                    MethodInfo GetRequiredSkills7 = issue.GetType().GetMethod("GetAlternativeSolutionCompanionSkills", BindingFlags.Instance | BindingFlags.NonPublic);
                    shouldHaveAll = (Dictionary<SkillObject, int>)GetRequiredSkills6.Invoke(issue, null);
                    shouldHaveOneOfThem = (Dictionary<SkillObject, int>)GetRequiredSkills7.Invoke(issue, null);
                    break;
                default:
                    // No requirement or unknown issue
                    break;
            }
        }

        static int AdjustXpReward(ValueTuple<SkillObject, int> originalReward, SkillObject skill, int xp)
        {
            // If the rewarded skill has already been rewarded in the Vanilla reward, adjust the new amount
            if (originalReward.Item1 == skill)
            {
                xp = Math.Max(0, xp - originalReward.Item2);
            }

            // Randomize XP amount
            float xpF = (float)xp;
            xpF = (xpF * MBRandom.RandomFloatRanged(0.75f, 1.25f));

            // Apply adjusting factor from settings
            xpF = xpF * Settings.Instance.XpGainsFactor;

            return (int)Math.Round(xpF);
        }

        static Dictionary<SkillObject, int> XpRewardFromLeadingTroops(int menLeaded, int issueDuration)
        {
            float menLeadedF = (float)menLeaded;
            float issueDurationF = (float)issueDuration;
            Dictionary<SkillObject, int> reward = new Dictionary<SkillObject, int>();

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

        static Dictionary<SkillObject, int> XpRewardFromRequiredSkills(Hero companion, ValueTuple<SkillObject, int> originalReward, Dictionary<SkillObject, int> shouldHaveAll, Dictionary<SkillObject, int> shouldHaveOneOfThem, int issueDuration)
        {
            Dictionary<SkillObject, int> reward = new Dictionary<SkillObject, int>();

            if (!shouldHaveAll.IsEmpty<KeyValuePair<SkillObject, int>>())
            {
                foreach (KeyValuePair<SkillObject, int> requiredSkill in shouldHaveAll)
                {
                    reward.Add(requiredSkill.Key, (int)Math.Round((float)originalReward.Item2 * (float)issueDuration * 2f));
                }
            }

            if (!shouldHaveOneOfThem.IsEmpty<KeyValuePair<SkillObject, int>>())
            {
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
