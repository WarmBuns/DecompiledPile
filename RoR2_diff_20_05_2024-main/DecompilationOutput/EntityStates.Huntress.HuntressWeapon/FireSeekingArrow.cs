using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Huntress.HuntressWeapon;

public class FireSeekingArrow : BaseState
{
	[SerializeField]
	public float orbDamageCoefficient;

	[SerializeField]
	public float orbProcCoefficient;

	[SerializeField]
	public string muzzleString;

	[SerializeField]
	public GameObject muzzleflashEffectPrefab;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public float baseDuration;

	[SerializeField]
	public int maxArrowCount;

	[SerializeField]
	public float baseArrowReloadDuration;

	private float duration;

	protected float arrowReloadDuration;

	private float arrowReloadTimer;

	protected bool isCrit;

	private int firedArrowCount;

	private HurtBox initialOrbTarget;

	private ChildLocator childLocator;

	private HuntressTracker huntressTracker;

	private Animator animator;

	private static int fireSeekingShotStateHash = Animator.StringToHash("FireSeekingShot");

	private static int fireSeekingShotParamHash = Animator.StringToHash("FireSeekingShot.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		Transform modelTransform = GetModelTransform();
		huntressTracker = GetComponent<HuntressTracker>();
		if ((bool)modelTransform)
		{
			childLocator = modelTransform.GetComponent<ChildLocator>();
			animator = modelTransform.GetComponent<Animator>();
		}
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, attackSpeedStat);
		if ((bool)huntressTracker && base.isAuthority)
		{
			initialOrbTarget = huntressTracker.GetTrackingTarget();
		}
		duration = baseDuration / attackSpeedStat;
		arrowReloadDuration = baseArrowReloadDuration / attackSpeedStat;
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration + 1f);
		}
		PlayCrossfade("Gesture, Override", fireSeekingShotStateHash, fireSeekingShotParamHash, duration, duration * 0.2f / attackSpeedStat);
		PlayCrossfade("Gesture, Additive", fireSeekingShotStateHash, fireSeekingShotParamHash, duration, duration * 0.2f / attackSpeedStat);
		isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
	}

	public override void OnExit()
	{
		base.OnExit();
		FireOrbArrow();
	}

	protected virtual GenericDamageOrb CreateArrowOrb()
	{
		return new HuntressArrowOrb();
	}

	private void FireOrbArrow()
	{
		if (firedArrowCount < maxArrowCount && !(arrowReloadTimer > 0f) && NetworkServer.active)
		{
			firedArrowCount++;
			arrowReloadTimer = arrowReloadDuration;
			GenericDamageOrb genericDamageOrb = CreateArrowOrb();
			genericDamageOrb.damageValue = base.characterBody.damage * orbDamageCoefficient;
			genericDamageOrb.isCrit = isCrit;
			genericDamageOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			genericDamageOrb.attacker = base.gameObject;
			genericDamageOrb.procCoefficient = orbProcCoefficient;
			HurtBox hurtBox = initialOrbTarget;
			if ((bool)hurtBox)
			{
				Transform transform = childLocator.FindChild(muzzleString);
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, muzzleString, transmit: true);
				genericDamageOrb.origin = transform.position;
				genericDamageOrb.target = hurtBox;
				OrbManager.instance.AddOrb(genericDamageOrb);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		arrowReloadTimer -= GetDeltaTime();
		if (animator.GetFloat("FireSeekingShot.fire") > 0f)
		{
			FireOrbArrow();
		}
		if (base.fixedAge > duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		writer.Write(HurtBoxReference.FromHurtBox(initialOrbTarget));
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		initialOrbTarget = reader.ReadHurtBoxReference().ResolveHurtBox();
	}
}
