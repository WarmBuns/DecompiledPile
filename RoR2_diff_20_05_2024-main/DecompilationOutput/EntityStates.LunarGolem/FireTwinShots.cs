using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LunarGolem;

public class FireTwinShots : BaseState
{
	public static GameObject projectilePrefab;

	public static GameObject effectPrefab;

	public static GameObject dustEffectPrefab;

	public static GameObject hitEffectPrefab;

	public static GameObject tracerEffectPrefab;

	public static float damageCoefficient;

	public static float blastRadius;

	public static float force;

	public static float baseDuration = 2f;

	public static string attackSoundString;

	public static float aimTime = 2f;

	public static string leftMuzzleTop;

	public static string rightMuzzleTop;

	public static string leftMuzzleBot;

	public static string rightMuzzleBot;

	public static int refireCount = 6;

	public static float baseAimDelay = 0.1f;

	public static float minLeadTime = 2f;

	public static float maxLeadTime = 2f;

	public static float fireSoundPlaybackRate;

	public static bool useSeriesFire = true;

	private int refireIndex;

	private Ray initialAimRay;

	private bool fired;

	private float aimDelay;

	private float duration;

	private static int FireTwinShotStateHash = Animator.StringToHash("FireTwinShot");

	private static int BufferEmptyShotStateHash = Animator.StringToHash("BufferEmpty");

	private static int FireTwinShotParamHash = Animator.StringToHash("FireTwinShot.playbackRate");

	private static int FireRightShotStateHash = Animator.StringToHash("FireRightShot");

	private static int FireLeftShotStateHash = Animator.StringToHash("FireLeftShot");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		aimDelay = baseAimDelay / attackSpeedStat;
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(aimTime);
		}
		if (!useSeriesFire)
		{
			PlayAnimation("Gesture, Additive", FireTwinShotStateHash, FireTwinShotParamHash, duration);
		}
		else
		{
			PlayAnimation("Gesture, Additive", BufferEmptyShotStateHash);
		}
		Util.PlayAttackSpeedSound(attackSoundString, base.gameObject, fireSoundPlaybackRate);
		initialAimRay = GetAimRay();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!fired && aimDelay <= base.fixedAge)
		{
			fired = true;
			if (base.isAuthority)
			{
				Fire();
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			if (refireIndex < refireCount)
			{
				outer.SetNextState(new FireTwinShots
				{
					refireIndex = refireIndex + 1
				});
			}
			else
			{
				outer.SetNextStateToMain();
			}
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}

	public override void OnSerialize(NetworkWriter writer)
	{
		base.OnSerialize(writer);
		writer.WritePackedUInt32((uint)refireIndex);
	}

	public override void OnDeserialize(NetworkReader reader)
	{
		base.OnDeserialize(reader);
		refireIndex = (int)reader.ReadPackedUInt32();
	}

	private void Fire()
	{
		Ray aimRay = GetAimRay();
		Quaternion a = Quaternion.LookRotation(initialAimRay.direction);
		Quaternion b = Quaternion.LookRotation(aimRay.direction);
		float num = Util.Remap(Util.Remap(refireIndex, 0f, refireCount - 1, 0f, 1f), 0f, 1f, minLeadTime, maxLeadTime) / aimDelay;
		Quaternion quaternion = Quaternion.SlerpUnclamped(a, b, 1f + num);
		Ray ray = new Ray(aimRay.origin, quaternion * Vector3.forward);
		if (refireIndex == 0 && (bool)dustEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(dustEffectPrefab, base.gameObject, "Root", transmit: true);
		}
		int num2 = refireIndex;
		if (!useSeriesFire)
		{
			num2 = refireIndex + 3;
		}
		while (refireIndex <= num2)
		{
			string muzzleName = "";
			bool flipProjectile = false;
			switch (refireIndex % 4)
			{
			case 0:
				muzzleName = rightMuzzleTop;
				PlayAnimation("Gesture, Right Additive", FireRightShotStateHash);
				flipProjectile = true;
				break;
			case 1:
				muzzleName = leftMuzzleTop;
				PlayAnimation("Gesture, Left Additive", FireLeftShotStateHash);
				break;
			case 2:
				muzzleName = rightMuzzleBot;
				PlayAnimation("Gesture, Right Additive", FireRightShotStateHash);
				flipProjectile = true;
				break;
			case 3:
				muzzleName = leftMuzzleBot;
				PlayAnimation("Gesture, Left Additive", FireLeftShotStateHash);
				break;
			}
			FireSingle(muzzleName, ray.direction, flipProjectile);
			refireIndex++;
		}
		refireIndex--;
	}

	private void FireSingle(string muzzleName, Vector3 aimDirection, bool flipProjectile)
	{
		ChildLocator modelChildLocator = GetModelChildLocator();
		if (!modelChildLocator)
		{
			return;
		}
		Transform transform = modelChildLocator.FindChild(muzzleName);
		if ((bool)transform)
		{
			if ((bool)effectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, muzzleName, transmit: true);
			}
			if (base.isAuthority)
			{
				ProjectileManager.instance.FireProjectile(projectilePrefab, transform.position, Util.QuaternionSafeLookRotation(aimDirection, (!flipProjectile) ? Vector3.up : Vector3.down), base.gameObject, damageStat * damageCoefficient, force, RollCrit());
			}
		}
	}
}
