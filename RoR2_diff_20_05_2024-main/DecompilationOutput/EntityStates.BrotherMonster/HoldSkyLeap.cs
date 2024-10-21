using RoR2;
using UnityEngine;

namespace EntityStates.BrotherMonster;

public class HoldSkyLeap : BaseState
{
	public static float duration;

	private CharacterModel characterModel;

	private HurtBoxGroup hurtboxGroup;

	private int originalLayer;

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			characterModel = modelTransform.GetComponent<CharacterModel>();
			hurtboxGroup = modelTransform.GetComponent<HurtBoxGroup>();
		}
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount++;
		}
		if ((bool)hurtboxGroup)
		{
			HurtBoxGroup hurtBoxGroup = hurtboxGroup;
			int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter + 1;
			hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		base.characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		Util.PlaySound("Play_moonBrother_phaseJump_land_preWhoosh", base.gameObject);
		originalLayer = base.gameObject.layer;
		base.gameObject.layer = LayerIndex.GetAppropriateFakeLayerForTeam(base.teamComponent.teamIndex).intVal;
		base.characterMotor.Motor.RebuildCollidableLayers();
		if (!SceneInfo.instance)
		{
			return;
		}
		ChildLocator component = SceneInfo.instance.GetComponent<ChildLocator>();
		if ((bool)component)
		{
			Transform transform = component.FindChild("CenterOfArena");
			if ((bool)transform)
			{
				base.characterMotor.Motor.SetPositionAndRotation(transform.position, Quaternion.identity);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge > duration)
		{
			outer.SetNextState(new ExitSkyLeap());
		}
	}

	public override void OnExit()
	{
		if ((bool)characterModel)
		{
			characterModel.invisibilityCount--;
		}
		if ((bool)hurtboxGroup)
		{
			HurtBoxGroup hurtBoxGroup = hurtboxGroup;
			int hurtBoxesDeactivatorCounter = hurtBoxGroup.hurtBoxesDeactivatorCounter - 1;
			hurtBoxGroup.hurtBoxesDeactivatorCounter = hurtBoxesDeactivatorCounter;
		}
		base.characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
		base.gameObject.layer = originalLayer;
		base.characterMotor.Motor.RebuildCollidableLayers();
		base.OnExit();
	}
}
