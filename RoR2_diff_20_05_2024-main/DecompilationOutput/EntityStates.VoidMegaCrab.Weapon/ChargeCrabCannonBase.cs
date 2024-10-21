using RoR2;
using UnityEngine;

namespace EntityStates.VoidMegaCrab.Weapon;

public abstract class ChargeCrabCannonBase : BaseState
{
	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public float baseDuration = 2f;

	[SerializeField]
	public string soundName;

	[SerializeField]
	public string chargeMuzzleName;

	[SerializeField]
	public string animationLayerName = "Gesture, Additive";

	[SerializeField]
	public string animationStateName = "FireCrabCannon";

	[SerializeField]
	public string animationStateNamePreCharged = "FireCrabCannon";

	[SerializeField]
	public string animationPlaybackRateParam = "FireCrabCannon.playbackRate";

	[SerializeField]
	public float animationCrossfadeDuration = 0.2f;

	private GameObject chargeInstance;

	private float duration;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			int layerIndex = modelAnimator.GetLayerIndex(animationLayerName);
			if (modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).IsName("Empty"))
			{
				PlayCrossfade(animationLayerName, animationStateName, animationPlaybackRateParam, duration, animationCrossfadeDuration);
			}
			else
			{
				PlayCrossfade(animationLayerName, animationStateNamePreCharged, animationPlaybackRateParam, duration, animationCrossfadeDuration);
			}
		}
		Util.PlaySound(soundName, base.gameObject);
		Transform transform = FindModelChild(chargeMuzzleName);
		if ((bool)chargeEffectPrefab && (bool)transform)
		{
			chargeInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeInstance.transform.parent = transform;
			ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = duration;
			}
		}
		base.characterBody.SetAimTimer(duration + 2f);
	}

	public override void OnExit()
	{
		base.OnExit();
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(GetNextState());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	protected abstract FireCrabCannonBase GetNextState();
}
