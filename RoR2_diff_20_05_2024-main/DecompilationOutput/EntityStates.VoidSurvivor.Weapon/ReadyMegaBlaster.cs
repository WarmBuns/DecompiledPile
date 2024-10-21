using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivor.Weapon;

public class ReadyMegaBlaster : BaseSkillState
{
	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string exitSoundString;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	private GameObject chargeEffectInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName);
		Transform transform = FindModelChild(muzzle);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeEffectInstance.transform.parent = transform;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterBody.SetAimTimer(3f);
		if (base.isAuthority && !IsKeyDownAuthority())
		{
			outer.SetNextState(new FireMegaBlasterBig());
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(exitSoundString, base.gameObject);
		if ((bool)chargeEffectInstance)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
