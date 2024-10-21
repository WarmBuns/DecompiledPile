using System.Linq;
using RoR2;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Squid.SquidWeapon;

public class FireSpine : BaseState
{
	public static GameObject hitEffectPrefab;

	public static GameObject muzzleflashEffectPrefab;

	public static float damageCoefficient;

	public static float baseDuration = 2f;

	public static float procCoefficient = 1f;

	public static float forceScalar = 1f;

	private bool hasFiredArrow;

	private ChildLocator childLocator;

	private BullseyeSearch enemyFinder;

	private const float maxVisionDistance = float.PositiveInfinity;

	public bool fullVision = true;

	private float duration;

	private static int FireGooStateHash = Animator.StringToHash("FireGoo");

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		GetAimRay();
		PlayAnimation("Gesture", FireGooStateHash);
		if (base.isAuthority)
		{
			FireOrbArrow();
		}
	}

	private void FireOrbArrow()
	{
		if (hasFiredArrow || !NetworkServer.active)
		{
			return;
		}
		Ray aimRay = GetAimRay();
		enemyFinder = new BullseyeSearch();
		enemyFinder.viewer = base.characterBody;
		enemyFinder.maxDistanceFilter = float.PositiveInfinity;
		enemyFinder.searchOrigin = aimRay.origin;
		enemyFinder.searchDirection = aimRay.direction;
		enemyFinder.sortMode = BullseyeSearch.SortMode.Distance;
		enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
		enemyFinder.minDistanceFilter = 0f;
		enemyFinder.maxAngleFilter = (fullVision ? 180f : 90f);
		enemyFinder.filterByLoS = true;
		if ((bool)base.teamComponent)
		{
			enemyFinder.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		enemyFinder.RefreshCandidates();
		HurtBox hurtBox = enemyFinder.GetResults().FirstOrDefault();
		if ((bool)hurtBox)
		{
			Vector3 vector = hurtBox.transform.position - GetAimRay().origin;
			aimRay.origin = GetAimRay().origin;
			aimRay.direction = vector;
			base.inputBank.aimDirection = vector;
			StartAimMode(aimRay);
			hasFiredArrow = true;
			SquidOrb squidOrb = new SquidOrb();
			squidOrb.forceScalar = forceScalar;
			squidOrb.damageValue = base.characterBody.damage * damageCoefficient;
			squidOrb.isCrit = Util.CheckRoll(base.characterBody.crit, base.characterBody.master);
			squidOrb.teamIndex = TeamComponent.GetObjectTeam(base.gameObject);
			squidOrb.attacker = base.gameObject;
			squidOrb.procCoefficient = procCoefficient;
			HurtBox hurtBox2 = hurtBox;
			if ((bool)hurtBox2)
			{
				Transform transform = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("Muzzle");
				EffectManager.SimpleMuzzleFlash(muzzleflashEffectPrefab, base.gameObject, "Muzzle", transmit: true);
				squidOrb.origin = transform.position;
				squidOrb.target = hurtBox2;
				OrbManager.instance.AddOrb(squidOrb);
			}
		}
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
