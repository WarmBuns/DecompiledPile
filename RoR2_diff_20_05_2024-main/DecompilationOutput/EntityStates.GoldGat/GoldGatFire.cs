using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.GoldGat;

public class GoldGatFire : BaseGoldGatState
{
	public static float minFireFrequency;

	public static float maxFireFrequency;

	public static float minSpread;

	public static float maxSpread;

	public static float windUpDuration;

	public static float force;

	public static float damageCoefficient;

	public static float procCoefficient;

	public static string attackSoundString;

	public static GameObject tracerEffectPrefab;

	public static GameObject impactEffectPrefab;

	public static GameObject muzzleFlashEffectPrefab;

	public static int baseMoneyCostPerBullet;

	public static string windUpSoundString;

	public static string windUpRTPC;

	public float totalStopwatch;

	private float stopwatch;

	private float fireFrequency;

	private uint loopSoundID;

	private static int CrankplaybackRateParamHash = Animator.StringToHash("Crank.playbackRate");

	public override void OnEnter()
	{
		base.OnEnter();
		loopSoundID = Util.PlaySound(windUpSoundString, base.gameObject);
		FireBullet();
	}

	private void FireBullet()
	{
		body.SetAimTimer(2f);
		float t = Mathf.Clamp01(totalStopwatch / windUpDuration);
		fireFrequency = Mathf.Lerp(minFireFrequency, maxFireFrequency, t);
		float num = Mathf.Lerp(minSpread, maxSpread, t);
		Util.PlaySound(attackSoundString, base.gameObject);
		int num2 = (int)((float)baseMoneyCostPerBullet * (1f + ((float)TeamManager.instance.GetTeamLevel(bodyMaster.teamIndex) - 1f) * 0.25f));
		if (base.isAuthority)
		{
			Transform aimOriginTransform = body.aimOriginTransform;
			if ((bool)gunChildLocator)
			{
				gunChildLocator.FindChild("Muzzle");
			}
			if ((bool)aimOriginTransform)
			{
				BulletAttack bulletAttack = new BulletAttack();
				bulletAttack.owner = networkedBodyAttachment.attachedBodyObject;
				bulletAttack.aimVector = bodyInputBank.aimDirection;
				bulletAttack.origin = bodyInputBank.aimOrigin;
				bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
				bulletAttack.force = force;
				bulletAttack.damage = body.damage * damageCoefficient;
				bulletAttack.damageColorIndex = DamageColorIndex.Item;
				bulletAttack.bulletCount = 1u;
				bulletAttack.minSpread = 0f;
				bulletAttack.maxSpread = num;
				bulletAttack.tracerEffectPrefab = tracerEffectPrefab;
				bulletAttack.isCrit = Util.CheckRoll(body.crit, bodyMaster);
				bulletAttack.procCoefficient = procCoefficient;
				bulletAttack.muzzleName = "Muzzle";
				bulletAttack.weapon = base.gameObject;
				bulletAttack.Fire();
				gunAnimator?.Play("Fire");
			}
		}
		if (NetworkServer.active)
		{
			bodyMaster.money = (uint)Mathf.Max(0f, bodyMaster.money - num2);
		}
		gunAnimator?.SetFloat(CrankplaybackRateParamHash, fireFrequency);
		EffectManager.SimpleMuzzleFlash(muzzleFlashEffectPrefab, base.gameObject, "Muzzle", transmit: false);
	}

	public override void Update()
	{
		base.Update();
		float deltaTime = Time.deltaTime;
		totalStopwatch += deltaTime;
		stopwatch += deltaTime;
		AkSoundEngine.SetRTPCValueByPlayingID(windUpRTPC, Mathf.InverseLerp(minFireFrequency, maxFireFrequency, fireFrequency) * 100f, loopSoundID);
		if (!CheckReturnToIdle() && stopwatch > 1f / fireFrequency)
		{
			stopwatch = 0f;
			FireBullet();
		}
	}

	public override void OnExit()
	{
		AkSoundEngine.StopPlayingID(loopSoundID);
		base.OnExit();
	}
}
