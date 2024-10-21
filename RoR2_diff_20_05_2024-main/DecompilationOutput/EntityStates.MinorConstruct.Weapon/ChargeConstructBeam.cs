using RoR2;
using UnityEngine;

namespace EntityStates.MinorConstruct.Weapon;

public class ChargeConstructBeam : BaseState
{
	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public string exitSoundString;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string chargeEffectMuzzle;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public float baseDuration;

	private float duration;

	private GameObject chargeInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		Util.PlaySound(enterSoundString, base.gameObject);
		Transform transform = FindModelChild(chargeEffectMuzzle);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeInstance.transform.parent = base.gameObject.transform;
			ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		if ((bool)chargeInstance)
		{
			Ray aimRay = GetAimRay();
			chargeInstance.transform.forward = aimRay.direction;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge > duration)
		{
			outer.SetNextState(new FireConstructBeam());
		}
	}

	public override void OnExit()
	{
		Util.PlaySound(exitSoundString, base.gameObject);
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
