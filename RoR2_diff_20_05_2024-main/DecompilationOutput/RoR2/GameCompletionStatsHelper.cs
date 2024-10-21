using System.Collections.Generic;
using RoR2.Skills;
using RoR2.Stats;

namespace RoR2;

public class GameCompletionStatsHelper
{
	private HashSet<UnlockableDef> unlockablesForGameCompletion = new HashSet<UnlockableDef>();

	private HashSet<AchievementDef> achievementsWithoutUnlockables = new HashSet<AchievementDef>();

	private HashSet<UnlockableDef> unlockablesWithoutAchievements = new HashSet<UnlockableDef>();

	private HashSet<PickupDef> encounterablePickups = new HashSet<PickupDef>();

	private int completableTotal;

	public GameCompletionStatsHelper()
	{
		HashSet<UnlockableDef> unlockablesFromAchievements = new HashSet<UnlockableDef>();
		foreach (SurvivorDef allSurvivorDef in SurvivorCatalog.allSurvivorDefs)
		{
			AddSurvivor(allSurvivorDef);
			void AddSurvivor(SurvivorDef survivorDef)
			{
				AddUnlockable(survivorDef.unlockableDef);
				BodyIndex bodyIndexFromSurvivorIndex = SurvivorCatalog.GetBodyIndexFromSurvivorIndex(survivorDef.survivorIndex);
				GenericSkill[] bodyPrefabSkillSlots = BodyCatalog.GetBodyPrefabSkillSlots(bodyIndexFromSurvivorIndex);
				if (bodyPrefabSkillSlots != null)
				{
					GenericSkill[] array = bodyPrefabSkillSlots;
					for (int i = 0; i < array.Length; i++)
					{
						SkillFamily skillFamily = array[i].skillFamily;
						if ((bool)skillFamily)
						{
							SkillFamily.Variant[] variants = skillFamily.variants;
							for (int j = 0; j < variants.Length; j++)
							{
								SkillFamily.Variant variant = variants[j];
								AddUnlockable(variant.unlockableDef);
							}
						}
					}
				}
				SkinDef[] bodySkins = BodyCatalog.GetBodySkins(bodyIndexFromSurvivorIndex);
				foreach (SkinDef skinDef in bodySkins)
				{
					AddUnlockable(skinDef.unlockableDef);
				}
				bool AddUnlockable(UnlockableDef unlockableDef)
				{
					if (unlockableDef == null)
					{
						return false;
					}
					unlockablesForGameCompletion.Add(unlockableDef);
					return true;
				}
			}
		}
		foreach (CharacterBody allBodyPrefabBodyBodyComponent in BodyCatalog.allBodyPrefabBodyBodyComponents)
		{
			AddMonster(allBodyPrefabBodyBodyComponent);
			void AddMonster(CharacterBody characterBody)
			{
				_003C_002Ector_003Eg__AddUnlockable_007C5_0(characterBody.GetComponent<DeathRewards>()?.logUnlockableDef);
			}
		}
		foreach (AchievementDef allAchievementDef in AchievementManager.allAchievementDefs)
		{
			AddAchievement(allAchievementDef);
		}
		foreach (PickupDef allPickup in PickupCatalog.allPickups)
		{
			AddPickup(allPickup);
			void AddPickup(PickupDef pickupDef)
			{
				ItemDef itemDef = ItemCatalog.GetItemDef(pickupDef.itemIndex);
				EquipmentDef equipmentDef = EquipmentCatalog.GetEquipmentDef(pickupDef.equipmentIndex);
				if (((object)itemDef != null && itemDef.inDroppableTier) || ((object)equipmentDef != null && equipmentDef.canDrop))
				{
					encounterablePickups.Add(pickupDef);
				}
			}
		}
		unlockablesWithoutAchievements.UnionWith(unlockablesForGameCompletion);
		unlockablesWithoutAchievements.ExceptWith(unlockablesFromAchievements);
		completableTotal = 0;
		completableTotal += unlockablesForGameCompletion.Count;
		completableTotal += achievementsWithoutUnlockables.Count;
		completableTotal += encounterablePickups.Count;
		completableTotal += SurvivorCatalog.survivorCount * 2;
		void AddAchievement(AchievementDef achievementDef)
		{
			UnlockableDef unlockableDef2 = UnlockableCatalog.GetUnlockableDef(achievementDef.unlockableRewardIdentifier);
			if (!_003C_002Ector_003Eg__AddUnlockable_007C5_0(unlockableDef2))
			{
				achievementsWithoutUnlockables.Add(achievementDef);
			}
			else
			{
				unlockablesFromAchievements.Add(unlockableDef2);
			}
		}
	}

	public IntFraction GetTotalCompletion(UserProfile userProfile)
	{
		IntFraction result = new IntFraction(0, 0);
		AddResult(GetUnlockableCompletion(userProfile));
		AddResult(GetAchievementWithoutUnlockableCompletion(userProfile));
		AddResult(GetPickupEncounterCompletion(userProfile));
		AddResult(GetSurvivorPickCompletion(userProfile));
		AddResult(GetSurvivorWinCompletion(userProfile));
		return result;
		void AddResult(IntFraction incoming)
		{
			result = new IntFraction(result.numerator + incoming.numerator, result.denominator + incoming.denominator);
		}
	}

	public IntFraction GetUnlockableCompletion(UserProfile userProfile)
	{
		int num = 0;
		foreach (UnlockableDef item in unlockablesForGameCompletion)
		{
			if (userProfile.HasUnlockable(item))
			{
				num++;
			}
		}
		return new IntFraction(num, unlockablesForGameCompletion.Count);
	}

	public IntFraction GetCollectibleCompletion(UserProfile userProfile)
	{
		int num = 0;
		foreach (UnlockableDef unlockablesWithoutAchievement in unlockablesWithoutAchievements)
		{
			if (userProfile.HasUnlockable(unlockablesWithoutAchievement))
			{
				num++;
			}
		}
		return new IntFraction(num, unlockablesWithoutAchievements.Count);
	}

	public IntFraction GetAchievementCompletion(UserProfile userProfile)
	{
		int num = 0;
		foreach (AchievementDef allAchievementDef in AchievementManager.allAchievementDefs)
		{
			if (userProfile.HasAchievement(allAchievementDef.identifier))
			{
				num++;
			}
		}
		return new IntFraction(num, AchievementManager.achievementCount);
	}

	private IntFraction GetAchievementWithoutUnlockableCompletion(UserProfile userProfile)
	{
		int num = 0;
		foreach (AchievementDef achievementsWithoutUnlockable in achievementsWithoutUnlockables)
		{
			if (userProfile.HasAchievement(achievementsWithoutUnlockable.identifier))
			{
				num++;
			}
		}
		return new IntFraction(num, achievementsWithoutUnlockables.Count);
	}

	public IntFraction GetPickupEncounterCompletion(UserProfile userProfile)
	{
		int num = 0;
		foreach (PickupDef allPickup in PickupCatalog.allPickups)
		{
			if (encounterablePickups.Contains(allPickup) && userProfile.HasDiscoveredPickup(allPickup.pickupIndex))
			{
				num++;
			}
		}
		return new IntFraction(num, encounterablePickups.Count);
	}

	public IntFraction GetSurvivorPickCompletion(UserProfile userProfile)
	{
		return GetSurvivorULongStatCompletion(userProfile, PerBodyStatDef.timesPicked, ignoreHidden: true);
	}

	public IntFraction GetSurvivorWinCompletion(UserProfile userProfile)
	{
		return GetSurvivorULongStatCompletion(userProfile, PerBodyStatDef.totalWins, ignoreHidden: true);
	}

	private IntFraction GetSurvivorULongStatCompletion(UserProfile userProfile, PerBodyStatDef perBodyStatDef, bool ignoreHidden)
	{
		int num = 0;
		int num2 = 0;
		foreach (SurvivorDef allSurvivorDef in SurvivorCatalog.allSurvivorDefs)
		{
			if (!ignoreHidden || !allSurvivorDef.hidden)
			{
				num2++;
				string bodyName = BodyCatalog.GetBodyName(SurvivorCatalog.GetBodyIndexFromSurvivorIndex(allSurvivorDef.survivorIndex));
				if (userProfile.statSheet.GetStatValueULong(perBodyStatDef, bodyName) != 0)
				{
					num++;
				}
			}
		}
		return new IntFraction(num, num2);
	}
}
