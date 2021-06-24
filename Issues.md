# Issues

d = `IssueDifficultyMultiplier`

| Issue | Duration | Troops | Required | Reward |
| ----- | -------- | ------ | -------- | ------ |
| `ArtisanCantSellProductsAtFairPrice` | 7 + 7d | 6 + 8d | TRD 150d | TRD 400 + 1700d |
| `ArtisanOverpricedGoods` | 7 + 7d | 6 + 8d | TRD 50 | TRD 400 + 1700d |
| `CapturedByBountyHunter` | 15 | 10 | SCT 30 MELEE 30 | MELEE 750 + 1000d |
| `CaravanAmbush` | 12 + 16d | 22 + 30d | TAC 150d MELEE 150d | SCT 600 + 800d |
| `EscortMerchantCaravan` | 2 + 6d | 9 + 9d | SCT 150d LDR 150d | SCT 800 + 1000d |
| `ExtortionByDeserters` | 12 + 16d | 22 + 30d | TAC 150d MELEE 150d | MELEE/RID 800 + 1000d |
| `GandLeaderNeedsRecruit` | 6 + 10f | 8 + 12f | LDR 150d | LDR 500 + 700d |
| `GandLeaderNeedsWeapons` | 22 + 30d | 10 + 20d | ROG 150d TRD 150d | TRD/ROG 800 + 900d |
| `HeadmanNeedsGrain` | 10 + 7d | 5 + 3d | TRD 150d | TRD 500 + 700d |
| `HeadmanNeedsToDeliverAHerd` | 5 + 15d | 5 + 15d | RID 150d | RID 500 + 700d |
| `HeadmanNeedsToDeliver` | 7 + 7d | 6 + 8d | TRD 150d | TRD 500 + 700d |
| `LandlordNeedsAccessToVillageCommons` | 8 + 17d | 6 + 19d | Nothing? | MELEE 700 + 900d |
| `LandLordNeedsManualLaborers` | 8 + 17d | 6 + 19d | MELEE 150d | MELEE 500 + 700d |
| `LandLordTheArtOfTheTrade` | 15 | 3 + 2d | TRD 100d | TRD 900 + 800d |
| `LandlordTrainingForRetainers` | 16 + 22d | 7 + 13d | TAC & LDR & MELEE 150d | MELEE \| TAC 500 + 700d |
| `LesserNobleRevolt` | 7 + 13d | 5 + 10d | LDR & SCT 150d | LDR \| SCT 800 + 1000d |
| `LordNeedsGarrisonTroops` | 7 + 13d | 5 + 10d | LDR & SCT 150d | SCT \| LDR 800 + 900d |
| `LordNeedsHorses` | 7 + 13d | 5 + 10d | TRD & RID 150d | TRD \| RID 500 + 700d |
| `MerchantArmyOfPoacher` | 10 + 14d | 17 + 23d | TAC & MELEE 70 | MELEE 800 + 1000d |
| `MerchantNeedsHelpWithOutlaws` | 5 + 10d | 7 + 10d | MELEE 150d | RID 600 + 800d |
| `NearbyBanditBase` | 20 | 10 | TAC 30 MELEE 50 | MELEE 1000 + 1250d |
| `VillageNeedsTools` | 7 + 7d | 6 + 8d | TRD 150d | TRD 500 + 700d |

## Code analysis

Deserving Companions relies on two methods to fetch required and rewarded skills: `GetAlternativeSolutionRequiredCompanionSkills()` and `CompanionSkillAndRewardXP`. The problem is some issues does not use those methods -- relying instead on alternative methods or hardcoded values...

* `GangLeaderNeedsRecruits` : `GetAlternativeSolutionRequiredCompanionSkills()` is not implemented. Uses `CompanionSkillRequirement` instead.
* `LandlordNeedsAccessToVillageCommons` : No `GetAlternativeSolutionRequiredCompanionSkills()`, because there isn't any skill requirement.
* `LandlordNeedsManualLaborers` : Required skills returned by `GetAlternativeSolutionRequiredCompanionSkills()` are all `ShouldHaveOneOfThem`.
* `LandLordTheArtOfTheTrade` : No `GetAlternativeSolutionRequiredCompanionSkills()`. Requirements are hardcoded in `CompanionSkillCondition()`.
*  `MerchantArmyOfPoachers` : `ShouldHaveOneOfThem` skills are in `GetAlternativeSolutionCompanionSkills()`.
* `MerchantNeedsHelpWithOutlaws` : required skills are `ShouldHaveOneOfThem`.

There is an issue with `GetAlternativeSolutionCompanionSkills()`. Sometimes it returns a single dict which can be of type `ShouldHaveAll` or `ShouldHaveOneOfThem`. Sometimes it just updates `ShouldHaveAll` and `ShouldHaveOneOfThem` dictionary parameters.

In the end there is three types of issues:
* issues that updates `ShouldHaveAll` and `ShouldHaveOneOfThem` dictionnaries (seems to be the default implementation)
* issues that return a single dict that can be of type `ShoudHaveAll` or `ShouldHaveOneOfThem`. We have to check the `AlternativeSolutionEndConsequence()` method to know.
* issues that does not implement `GetAlternativeSolutionRequiredCompanionSkills`, relying instead on another method or in hardcoded values.

## Implementation

| Issue | `GetAlternativeSolutionRequiredCompanionSkills()` |
| ----- | -------- |
| `ArtisanCantSellProductsAtFairPrice` | Return `ShouldHaveAll` (single skill) |
| `ArtisanOverpricedGoods` | Return `ShouldHaveAll` (single skill) |
| `CapturedByBountyHunter` | Update `ShouldHaveAll` & `ShouldHaveOneOfThem` |
| `CaravanAmbush` | Update `ShouldHaveAll` & `ShouldHaveOneOfThem` |
| `EscortMerchantCaravan` | Return `ShouldHaveAll` |
| `ExtortionByDeserters` | Update `ShouldHaveAll` & `ShouldHaveOneOfThem` |
| `GandLeaderNeedsRecruit` | `ShouldHaveOneOfThem` returned by `CompanionSkillRequirement` |
| `GangLeaderNeedsWeapons` | Return `ShouldHaveAll` |
| `HeadmanNeedsGrain` | Return `ShouldHaveAll` |
| `HeadmanNeedsToDeliverAHerd` | Return `ShouldHaveAll` |
| `HeadmanVillageNeedsDraughtAnimals` | Return `ShouldHaveAll` |
| `LandlordNeedsAccessToVillageCommons` | No requirement |
| `LandLordNeedsManualLaborers` | Return `ShouldHaveOneOfThem` |
| `LandLordTheArtOfTheTrade` | Hardcoded in `CompanionSkillCondition` |
| `LandlordTrainingForRetainers` | Update `ShouldHaveAll` & `ShouldHaveOneOfThem` |
| `LesserNobleRevolt` | Return `ShouldHaveAll` |
| `LordNeedsGarrisonTroops` | Return `ShouldHaveAll` |
| `LordNeedsHorses` | Return `ShouldHaveAll` |
| `MerchantArmyOfPoacher` | Return `ShouldHaveAll`. `GetAlternativeSolutionCompanionSkills()` returns `ShouldHaveOneOfThem` |
| `MerchantNeedsHelpWithOutlaws` | Return `ShouldHaveOneOfThem` |
| `NearbyBanditBase` | Update `ShouldHaveAll` & `ShouldHaveOneOfThem` |
| `VillageNeedsTools` | Return `ShouldHaveAll` |