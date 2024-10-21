using UnityEngine;

namespace RoR2;

public class LevelUpEffectManager
{
	private static float levelUpEffectInterval = 0.25f;

	private static float levelUpEffectJitter = 0.25f;

	private static int pendingLevelUpEffects = 0;

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		GlobalEventManager.onTeamLevelUp += OnTeamLevelUp;
		GlobalEventManager.onCharacterLevelUp += OnCharacterLevelUp;
		Run.onRunAmbientLevelUp += OnRunAmbientLevelUp;
	}

	private static void OnTeamLevelUp(TeamIndex teamIndex)
	{
		if (TeamComponent.GetTeamMembers(teamIndex).Count > 0)
		{
			Util.PlaySound(TeamCatalog.GetTeamDef(teamIndex)?.levelUpSound, RoR2Application.instance.gameObject);
		}
	}

	private static void OnCharacterLevelUp(CharacterBody characterBody)
	{
		GameObject levelUpEffect = TeamCatalog.GetTeamDef(characterBody.teamComponent.teamIndex)?.levelUpEffect;
		if (!characterBody)
		{
			return;
		}
		Transform transform = (characterBody.mainHurtBox ? characterBody.mainHurtBox.transform : characterBody.transform);
		EffectData effectData = new EffectData
		{
			origin = transform.position
		};
		if ((bool)characterBody.mainHurtBox)
		{
			effectData.SetHurtBoxReference(characterBody.gameObject);
			effectData.scale = characterBody.radius;
		}
		RoR2Application.fixedTimeTimers.CreateTimer((float)pendingLevelUpEffects * levelUpEffectInterval + Random.value * levelUpEffectJitter, delegate
		{
			if ((bool)characterBody)
			{
				EffectManager.SpawnEffect(levelUpEffect, effectData, transmit: false);
			}
			levelUpEffectInterval -= 1f;
		});
		levelUpEffectInterval += 1f;
	}

	private static void OnRunAmbientLevelUp(Run run)
	{
		Util.PlaySound("Play_UI_levelUp_enemy", RoR2Application.instance.gameObject);
	}
}
