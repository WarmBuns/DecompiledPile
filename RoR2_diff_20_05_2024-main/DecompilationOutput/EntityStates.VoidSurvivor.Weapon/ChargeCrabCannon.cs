using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.VoidSurvivor.Weapon;

public class ChargeCrabCannon : BaseSkillState
{
	[SerializeField]
	public float baseDurationPerGrenade;

	[SerializeField]
	public float minimumDuration;

	[SerializeField]
	public string muzzle;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	[SerializeField]
	public string chargeStockSoundString;

	[SerializeField]
	public string chargeLoopStartSoundString;

	[SerializeField]
	public string chargeLoopStopSoundString;

	[SerializeField]
	public float bloomPerGrenade;

	[SerializeField]
	public float corruptionPerGrenade;

	[SerializeField]
	public string animationLayerName;

	[SerializeField]
	public string animationStateName;

	private VoidSurvivorController voidSurvivorController;

	private GameObject chargeEffectInstance;

	private int grenadeCount;

	private int lastGrenadeCount;

	private float durationPerGrenade;

	private float nextGrenadeStopwatch;

	private static int BufferEmptyStateHash = Animator.StringToHash("BufferEmpty");

	public override void OnEnter()
	{
		base.OnEnter();
		voidSurvivorController = GetComponent<VoidSurvivorController>();
		PlayAnimation(animationLayerName, animationStateName);
		durationPerGrenade = baseDurationPerGrenade / attackSpeedStat;
		Util.PlaySound(chargeLoopStartSoundString, base.gameObject);
		AddGrenade();
		Transform modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild(muzzle);
		if ((bool)transform && (bool)chargeEffectPrefab)
		{
			chargeEffectInstance = Object.Instantiate(chargeEffectPrefab, transform.position, transform.rotation);
			chargeEffectInstance.transform.parent = transform;
			ScaleParticleSystemDuration component2 = chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component2)
			{
				component2.newDuration = durationPerGrenade;
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		Util.PlaySound(chargeLoopStopSoundString, base.gameObject);
		PlayAnimation(animationLayerName, BufferEmptyStateHash);
		EntityState.Destroy(chargeEffectInstance);
	}

	private void AddGrenade()
	{
		grenadeCount++;
		if ((bool)voidSurvivorController && NetworkServer.active)
		{
			voidSurvivorController.AddCorruption(corruptionPerGrenade);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		base.characterBody.SetAimTimer(3f);
		nextGrenadeStopwatch += GetDeltaTime();
		if (nextGrenadeStopwatch > durationPerGrenade && base.activatorSkillSlot.stock > 0)
		{
			AddGrenade();
			nextGrenadeStopwatch -= durationPerGrenade;
			base.activatorSkillSlot.DeductStock(1);
		}
		float value = bloomPerGrenade * (float)lastGrenadeCount;
		base.characterBody.SetSpreadBloom(value);
		if (lastGrenadeCount < grenadeCount)
		{
			Util.PlaySound(chargeStockSoundString, base.gameObject);
		}
		if (!IsKeyDownAuthority() && base.fixedAge > minimumDuration / attackSpeedStat && base.isAuthority)
		{
			FireCrabCannon fireCrabCannon = new FireCrabCannon();
			fireCrabCannon.grenadeCountMax = grenadeCount;
			outer.SetNextState(fireCrabCannon);
		}
		else
		{
			lastGrenadeCount = grenadeCount;
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
