using RoR2;
using RoR2.UI;
using UnityEngine;

namespace EntityStates.Railgunner.Weapon;

public abstract class BaseWindupSnipe : BaseState, IBaseWeaponState
{
	[SerializeField]
	public float duration;

	[SerializeField]
	public GameObject crosshairOverridePrefab;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	[SerializeField]
	public string animationPlaybackRateParam;

	[SerializeField]
	public string enterSoundString;

	[SerializeField]
	public GameObject windupEffectPrefab;

	[SerializeField]
	public string windupEffectMuzzle;

	private CrosshairUtils.OverrideRequest crosshairOverrideRequest;

	private GameObject windupEffectInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(enterSoundString, base.gameObject);
		PlayAnimation(animationLayerName, animationStateName, animationPlaybackRateParam, duration);
		if ((bool)crosshairOverridePrefab)
		{
			crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
		}
		if ((bool)windupEffectPrefab)
		{
			Transform transform = FindModelChild(windupEffectMuzzle);
			if ((bool)transform)
			{
				windupEffectInstance = Object.Instantiate(windupEffectPrefab, transform.position, transform.rotation);
				windupEffectInstance.transform.parent = transform;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		base.characterBody.SetSpreadBloom(base.age / duration, canOnlyIncreaseBloom: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(InstantiateNextState());
		}
	}

	public override void OnExit()
	{
		crosshairOverrideRequest?.Dispose();
		if ((bool)windupEffectInstance)
		{
			EntityState.Destroy(windupEffectInstance);
		}
		base.OnExit();
	}

	protected abstract EntityState InstantiateNextState();

	public bool CanScope()
	{
		return true;
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Frozen;
	}
}
