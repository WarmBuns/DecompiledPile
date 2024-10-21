using RoR2;
using UnityEngine;

namespace EntityStates.LunarWisp;

public class ChargeLunarGuns : BaseState
{
	public static string muzzleNameRoot;

	public static string muzzleNameOne;

	public static string muzzleNameTwo;

	public static float baseDuration;

	public static string windUpSound;

	public static GameObject chargeEffectPrefab;

	private GameObject chargeInstance;

	private GameObject chargeInstanceTwo;

	private float duration;

	public static float spinUpDuration;

	public static float chargeEffectDelay;

	private bool chargeEffectSpawned;

	private bool upToSpeed;

	private uint loopedSoundID;

	protected Transform muzzleTransformRoot;

	protected Transform muzzleTransformOne;

	protected Transform muzzleTransformTwo;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = (baseDuration + spinUpDuration) / attackSpeedStat;
		muzzleTransformRoot = FindModelChild(muzzleNameRoot);
		muzzleTransformOne = FindModelChild(muzzleNameOne);
		muzzleTransformTwo = FindModelChild(muzzleNameTwo);
		loopedSoundID = Util.PlaySound(windUpSound, base.gameObject);
		PlayCrossfade("Gesture", "MinigunSpinUp", 0.2f);
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		StartAimMode(0.5f);
		if (base.fixedAge >= chargeEffectDelay && !chargeEffectSpawned)
		{
			chargeEffectSpawned = true;
			if ((bool)muzzleTransformOne && (bool)muzzleTransformTwo && (bool)chargeEffectPrefab)
			{
				chargeInstance = Object.Instantiate(chargeEffectPrefab, muzzleTransformOne.position, muzzleTransformOne.rotation);
				chargeInstance.transform.parent = muzzleTransformOne;
				chargeInstanceTwo = Object.Instantiate(chargeEffectPrefab, muzzleTransformTwo.position, muzzleTransformTwo.rotation);
				chargeInstanceTwo.transform.parent = muzzleTransformTwo;
				ScaleParticleSystemDuration component = chargeInstance.GetComponent<ScaleParticleSystemDuration>();
				if ((bool)component)
				{
					component.newDuration = duration;
				}
			}
		}
		if (base.fixedAge >= spinUpDuration && !upToSpeed)
		{
			upToSpeed = true;
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			FireLunarGuns fireLunarGuns = new FireLunarGuns();
			fireLunarGuns.muzzleTransformOne = muzzleTransformOne;
			fireLunarGuns.muzzleTransformTwo = muzzleTransformTwo;
			fireLunarGuns.muzzleNameOne = muzzleNameOne;
			fireLunarGuns.muzzleNameTwo = muzzleNameTwo;
			outer.SetNextState(fireLunarGuns);
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		AkSoundEngine.StopPlayingID(loopedSoundID);
		if ((bool)chargeInstance)
		{
			EntityState.Destroy(chargeInstance);
		}
		if ((bool)chargeInstanceTwo)
		{
			EntityState.Destroy(chargeInstanceTwo);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
