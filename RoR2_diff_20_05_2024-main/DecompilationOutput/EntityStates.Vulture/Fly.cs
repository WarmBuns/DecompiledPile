using RoR2;
using RoR2.Skills;
using UnityEngine;

namespace EntityStates.Vulture;

public class Fly : VultureModeState
{
	public static SkillDef landingSkill;

	public static float launchSpeed;

	public static GameObject jumpEffectPrefab;

	public static string jumpEffectMuzzleString;

	private static int JumpStateHash = Animator.StringToHash("Jump");

	public override void OnEnter()
	{
		base.OnEnter();
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount++;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount++;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
		if ((bool)base.characterMotor)
		{
			base.characterMotor.velocity.y = launchSpeed;
			base.characterMotor.Motor.ForceUnground();
		}
		PlayAnimation("Body", JumpStateHash);
		if ((bool)base.activatorSkillSlot)
		{
			base.activatorSkillSlot.SetSkillOverride(this, landingSkill, GenericSkill.SkillOverridePriority.Contextual);
		}
		if ((bool)jumpEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(jumpEffectPrefab, base.gameObject, jumpEffectMuzzleString, transmit: false);
		}
	}

	public override void OnExit()
	{
		if ((bool)base.activatorSkillSlot)
		{
			base.activatorSkillSlot.UnsetSkillOverride(this, landingSkill, GenericSkill.SkillOverridePriority.Contextual);
		}
		if (characterFlightParameterProvider != null)
		{
			CharacterFlightParameters flightParameters = characterFlightParameterProvider.flightParameters;
			flightParameters.channeledFlightGranterCount--;
			characterFlightParameterProvider.flightParameters = flightParameters;
		}
		if (characterGravityParameterProvider != null)
		{
			CharacterGravityParameters gravityParameters = characterGravityParameterProvider.gravityParameters;
			gravityParameters.channeledAntiGravityGranterCount--;
			characterGravityParameterProvider.gravityParameters = gravityParameters;
		}
		if ((bool)base.modelLocator)
		{
			base.modelLocator.normalizeToFloor = true;
		}
		base.OnExit();
	}
}
