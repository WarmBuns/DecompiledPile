using System;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

[Serializable]
public class DirectorCard
{
	public SpawnCard spawnCard;

	public int selectionWeight;

	public DirectorCore.MonsterSpawnDistance spawnDistance;

	public bool preventOverhead;

	public int minimumStageCompletions;

	[Obsolete("'requiredUnlockable' will be discontinued. Use 'requiredUnlockableDef' instead.", false)]
	[Tooltip("'requiredUnlockable' will be discontinued. Use 'requiredUnlockableDef' instead.")]
	public string requiredUnlockable;

	[Obsolete("'forbiddenUnlockable' will be discontinued. Use 'forbiddenUnlockableDef' instead.", false)]
	[Tooltip("'forbiddenUnlockable' will be discontinued. Use 'forbiddenUnlockableDef' instead.")]
	public string forbiddenUnlockable;

	public UnlockableDef requiredUnlockableDef;

	public UnlockableDef forbiddenUnlockableDef;

	public int cost => spawnCard.directorCreditCost;

	[Obsolete("'CardIsValid' is confusingly named. Use 'IsAvailable' instead.", false)]
	public bool CardIsValid()
	{
		return IsAvailable();
	}

	public bool IsAvailable()
	{
		ref string reference = ref requiredUnlockable;
		ref string reference2 = ref forbiddenUnlockable;
		if (!requiredUnlockableDef && !string.IsNullOrEmpty(reference))
		{
			requiredUnlockableDef = UnlockableCatalog.GetUnlockableDef(reference);
			reference = null;
		}
		if (!forbiddenUnlockableDef && !string.IsNullOrEmpty(reference2))
		{
			forbiddenUnlockableDef = UnlockableCatalog.GetUnlockableDef(reference2);
			reference2 = null;
		}
		bool flag = !requiredUnlockableDef || Run.instance.IsUnlockableUnlocked(requiredUnlockableDef);
		bool flag2 = (bool)forbiddenUnlockableDef && Run.instance.DoesEveryoneHaveThisUnlockableUnlocked(forbiddenUnlockableDef);
		if ((bool)Run.instance && Run.instance.stageClearCount >= minimumStageCompletions && flag && !flag2 && (bool)spawnCard && (bool)spawnCard.prefab)
		{
			ExpansionRequirementComponent component = spawnCard.prefab.GetComponent<ExpansionRequirementComponent>();
			if (!component)
			{
				CharacterMaster component2 = spawnCard.prefab.GetComponent<CharacterMaster>();
				if ((bool)component2 && (bool)component2.bodyPrefab)
				{
					component = component2.bodyPrefab.GetComponent<ExpansionRequirementComponent>();
				}
			}
			if ((bool)component)
			{
				return Run.instance.IsExpansionEnabled(component.requiredExpansion);
			}
			return true;
		}
		return false;
	}
}
