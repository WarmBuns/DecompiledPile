using RoR2;
using UnityEngine;

namespace EntityStates.VoidRaidCrab.Leg;

public abstract class BaseStompState : BaseLegState
{
	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public string animName;

	public static GameObject warningIndicatorPrefab;

	public GameObject target;

	protected float duration;

	private bool lifetimeExpiredAuthority;

	private RayAttackIndicator warningIndicatorInstance;

	protected virtual bool shouldUseWarningIndicator => false;

	protected virtual bool shouldUpdateLegStompTargetPosition => false;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration;
		string stompPlaybackRateParam = base.legController.stompPlaybackRateParam;
		if (!string.IsNullOrEmpty(stompPlaybackRateParam))
		{
			EntityState.PlayAnimationOnAnimator(base.legController.animator, base.legController.primaryLayerName, animName, stompPlaybackRateParam, duration);
		}
		else
		{
			EntityState.PlayAnimationOnAnimator(base.legController.animator, base.legController.primaryLayerName, animName);
			int layerIndex = base.legController.animator.GetLayerIndex(base.legController.primaryLayerName);
			duration = base.legController.animator.GetCurrentAnimatorStateInfo(layerIndex).length;
		}
		SetWarningIndicatorActive(shouldUseWarningIndicator);
	}

	public override void OnExit()
	{
		SetWarningIndicatorActive(newWarningIndicatorActive: false);
		base.OnExit();
	}

	public override void ModifyNextState(EntityState nextState)
	{
		base.ModifyNextState(nextState);
		if (nextState is BaseStompState baseStompState)
		{
			baseStompState.warningIndicatorInstance = warningIndicatorInstance;
			warningIndicatorInstance = null;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.legController.mainBody.hasEffectiveAuthority && base.fixedAge >= duration && !lifetimeExpiredAuthority)
		{
			lifetimeExpiredAuthority = true;
			OnLifetimeExpiredAuthority();
		}
		if (shouldUpdateLegStompTargetPosition && (bool)target)
		{
			base.legController.SetStompTargetWorldPosition(target.transform.position);
		}
		UpdateWarningIndicatorInstance();
	}

	protected abstract void OnLifetimeExpiredAuthority();

	protected void SetWarningIndicatorActive(bool newWarningIndicatorActive)
	{
		if ((bool)warningIndicatorInstance == newWarningIndicatorActive)
		{
			return;
		}
		if (newWarningIndicatorActive)
		{
			GameObject gameObject = Object.Instantiate(warningIndicatorPrefab);
			warningIndicatorInstance = gameObject.GetComponent<RayAttackIndicator>();
			UpdateWarningIndicatorInstance();
			return;
		}
		if ((bool)warningIndicatorInstance)
		{
			EntityState.Destroy(warningIndicatorInstance.gameObject);
		}
		warningIndicatorInstance = null;
	}

	private void UpdateWarningIndicatorInstance()
	{
		if ((bool)warningIndicatorInstance)
		{
			Vector3 position = base.legController.toeTipTransform.position;
			Vector3 vector = (base.legController.mainBody ? base.legController.mainBody.transform.position : position);
			warningIndicatorInstance.attackRay = new Ray(position, Vector3.down);
			warningIndicatorInstance.attackRange = position.y - vector.y;
			warningIndicatorInstance.attackRadius = Stomp.blastRadius;
			warningIndicatorInstance.layerMask = LayerIndex.world.mask;
		}
	}
}
