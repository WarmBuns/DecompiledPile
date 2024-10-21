using RoR2;
using UnityEngine;

namespace EntityStates.VoidSurvivor.Weapon;

public class ChargeMegaBlaster : BaseSkillState
{
	[SerializeField]
	public float minimumDuration = 0.1f;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public string chargeSoundString;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	private float duration;

	private uint soundID;

	private GameObject chargeEffectInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		soundID = Util.PlayAttackSpeedSound(chargeSoundString, base.gameObject, attackSpeedStat);
		PlayAnimation(animationLayerName, animationStateName);
		base.characterBody.SetAimTimer(duration + 1f);
		Transform transform = FindModelChild(muzzle);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeEffectInstance.transform.parent = transform;
			ScaleParticleSystemDuration component = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			ObjectScaleCurve component2 = chargeEffectInstance.GetComponent<ObjectScaleCurve>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
			if ((bool)component2)
			{
				component2.timeMax = duration;
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority)
		{
			if (base.fixedAge >= duration)
			{
				outer.SetNextState(new ReadyMegaBlaster
				{
					activatorSkillSlot = base.activatorSkillSlot
				});
			}
			else if (!IsKeyDownAuthority() && base.fixedAge > minimumDuration)
			{
				outer.SetNextState(new FireMegaBlasterSmall());
			}
		}
	}

	public override void OnExit()
	{
		if ((bool)chargeEffectInstance)
		{
			EntityState.Destroy(chargeEffectInstance);
		}
		AkSoundEngine.StopPlayingID(soundID);
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
