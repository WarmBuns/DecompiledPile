using System.Linq;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Scorchling;

public class ScorchlingLavaBomb : BaseState
{
	[Header("Timing")]
	[SerializeField]
	public float breachToSpitTime = 1f;

	[SerializeField]
	public float spitToLaunchTime = 0.3f;

	[SerializeField]
	public float spitToBurrowTime = 5f;

	[SerializeField]
	public float burrowToEndOfTime = 1f;

	[SerializeField]
	public float animDurationBreach = 1f;

	[SerializeField]
	public float animDurationSpit = 1f;

	[SerializeField]
	public float animDurationBurrow = 1f;

	[SerializeField]
	public float animDurationPostSpit = 0.75f;

	[SerializeField]
	public float percentageToFireProjectile = 0.75f;

	[Header("FX Objects")]
	[SerializeField]
	public GameObject burrowEffectPrefab;

	[SerializeField]
	public float burrowRadius = 1f;

	[Header("Sound Strings")]
	[SerializeField]
	public string breachSoundString;

	[SerializeField]
	public string spitSoundString;

	[SerializeField]
	public string burrowSoundString;

	[SerializeField]
	public string burrowLoopSoundString;

	[SerializeField]
	public string burrowStopLoopSoundString;

	[Header("Lava Bomb Projectile")]
	public static GameObject mortarProjectilePrefab;

	public static GameObject mortarMuzzleflashEffect;

	public static int mortarCount;

	public static string mortarMuzzleName;

	public static string mortarSoundString;

	public static float mortarDamageCoefficient;

	public static float timeToTarget = 3f;

	public static float projectileVelocity = 55f;

	public static float minimumDistance;

	private bool spitStarted;

	private bool firedProjectile;

	private bool earlyExit;

	private ScorchlingController sController;

	public override void OnEnter()
	{
		base.OnEnter();
		sController = base.characterBody.GetComponent<ScorchlingController>();
		animDurationBreach = (sController.isBurrowed ? animDurationBreach : 0f);
		spitToBurrowTime += animDurationBreach + animDurationSpit;
		burrowToEndOfTime += spitToBurrowTime;
		if (sController.isBurrowed)
		{
			earlyExit = true;
			if (Util.HasEffectiveAuthority(base.characterBody.networkIdentity))
			{
				outer.SetNextState(new ScorchlingBreach
				{
					proceedImmediatelyToLavaBomb = true,
					breachToBurrow = breachToSpitTime
				});
			}
		}
		else
		{
			base.characterBody.SetAimTimer(burrowToEndOfTime);
		}
	}

	public override void FixedUpdate()
	{
		if (earlyExit)
		{
			return;
		}
		base.FixedUpdate();
		if (!spitStarted && base.fixedAge > animDurationBreach)
		{
			spitStarted = true;
			PlayAnimation("FullBody, Override", "Spit", "Spit.playbackRate", animDurationSpit);
		}
		if (spitStarted && !firedProjectile && base.fixedAge > animDurationSpit * percentageToFireProjectile + animDurationBreach)
		{
			firedProjectile = true;
			Util.PlaySound(spitSoundString, base.gameObject);
			EffectManager.SimpleMuzzleFlash(mortarMuzzleflashEffect, base.gameObject, mortarMuzzleName, transmit: false);
			if (base.isAuthority)
			{
				Spit();
			}
		}
		if (firedProjectile && base.fixedAge > animDurationBreach + animDurationSpit + animDurationPostSpit)
		{
			outer.SetNextStateToMain();
		}
	}

	public void Spit()
	{
		Transform transform = base.characterBody.modelLocator.modelTransform.GetComponent<ChildLocator>().FindChild("MuzzleFire");
		Ray ray = new Ray(transform.position, transform.forward);
		Ray ray2 = new Ray(ray.origin, Vector3.up);
		BullseyeSearch bullseyeSearch = new BullseyeSearch();
		bullseyeSearch.searchOrigin = ray.origin;
		bullseyeSearch.searchDirection = ray.direction;
		bullseyeSearch.filterByLoS = false;
		bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			bullseyeSearch.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
		bullseyeSearch.RefreshCandidates();
		HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault();
		bool flag = false;
		Vector3 vector = Vector3.zero;
		RaycastHit hitInfo;
		if ((bool)hurtBox)
		{
			vector = hurtBox.transform.position;
			flag = true;
		}
		else if (Physics.Raycast(ray, out hitInfo, 1000f, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
		{
			vector = hitInfo.point;
			flag = true;
		}
		float magnitude = projectileVelocity;
		if (flag)
		{
			Vector3 vector2 = vector - ray2.origin;
			Vector2 vector3 = new Vector2(vector2.x, vector2.z);
			float magnitude2 = vector3.magnitude;
			Vector2 vector4 = vector3 / magnitude2;
			if (magnitude2 < minimumDistance)
			{
				magnitude2 = minimumDistance;
			}
			float y = Trajectory.CalculateInitialYSpeed(timeToTarget, vector2.y);
			float num = magnitude2 / timeToTarget;
			Vector3 direction = new Vector3(vector4.x * num, y, vector4.y * num);
			magnitude = direction.magnitude;
			ray2.direction = direction;
		}
		Quaternion rotation = Util.QuaternionSafeLookRotation(ray2.direction);
		ProjectileManager.instance.FireProjectile(mortarProjectilePrefab, ray2.origin, rotation, base.gameObject, damageStat * mortarDamageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master), DamageColorIndex.Default, null, magnitude);
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
