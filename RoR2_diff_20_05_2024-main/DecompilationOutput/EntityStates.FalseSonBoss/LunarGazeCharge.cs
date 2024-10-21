using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.FalseSonBoss;

public class LunarGazeCharge : BaseState
{
	public static float baseDuration = 3f;

	public static float laserMaxWidth = 0.2f;

	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public GameObject laserPrefab;

	public static string chargeAttackSoundString;

	public static float lockOnAngle;

	private HurtBox lockedOnHurtBox;

	public float duration;

	private GameObject chargeEffect;

	private GameObject laserEffect;

	private LineRenderer laserLineComponent;

	private Vector3 visualEndPosition;

	private float flashTimer;

	private bool laserOn;

	private BullseyeSearch enemyFinder;

	private const float originalSoundDuration = 2.1f;

	protected EntityState fireState;

	public override void OnEnter()
	{
		base.OnEnter();
		fireState = new LunarGazeFire();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture, Additive", "LaserBlastStart", "LaserBlast.playbackRate", duration);
		PlayAnimation("Gesture, Override", "LaserBlastStart", "LaserBlast.playbackRate", duration);
		Transform modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(chargeAttackSoundString, base.gameObject, 2.1f / duration);
		Ray aimRay = GetAimRay();
		enemyFinder = new BullseyeSearch();
		if ((bool)base.characterBody)
		{
			enemyFinder.viewer = base.characterBody;
		}
		enemyFinder.maxDistanceFilter = 2000f;
		enemyFinder.maxAngleFilter = lockOnAngle;
		enemyFinder.searchOrigin = aimRay.origin;
		enemyFinder.searchDirection = aimRay.direction;
		enemyFinder.filterByLoS = false;
		enemyFinder.sortMode = BullseyeSearch.SortMode.Angle;
		enemyFinder.teamMaskFilter = TeamMask.allButNeutral;
		if ((bool)base.teamComponent)
		{
			enemyFinder.teamMaskFilter.RemoveTeam(base.teamComponent.teamIndex);
		}
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component)
			{
				Transform transform = component.FindChild("MuzzleLaser");
				if ((bool)transform)
				{
					if ((bool)effectPrefab)
					{
						chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
						chargeEffect.transform.parent = transform;
						ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
						if ((bool)component2)
						{
							component2.newDuration = duration;
						}
					}
					if ((bool)laserPrefab)
					{
						laserEffect = Object.Instantiate(laserPrefab, transform.position, transform.rotation);
						laserEffect.transform.parent = transform;
						laserLineComponent = laserEffect.GetComponent<LineRenderer>();
					}
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
		flashTimer = 0f;
		laserOn = true;
	}

	public override void OnExit()
	{
		PlayAnimation("FullBody, Override", "Empty");
		base.OnExit();
		if ((bool)chargeEffect)
		{
			EntityState.Destroy(chargeEffect);
		}
		if ((bool)laserEffect)
		{
			EntityState.Destroy(laserEffect);
		}
	}

	public override void Update()
	{
		base.Update();
		if (!laserEffect || !laserLineComponent)
		{
			return;
		}
		float num = 1000f;
		Ray aimRay = GetAimRay();
		enemyFinder.RefreshCandidates();
		lockedOnHurtBox = enemyFinder.GetResults().FirstOrDefault();
		if ((bool)lockedOnHurtBox)
		{
			aimRay.direction = lockedOnHurtBox.transform.position - aimRay.origin;
		}
		Vector3 position = laserEffect.transform.parent.position;
		Vector3 point = aimRay.GetPoint(num);
		if (Physics.Raycast(aimRay, out var hitInfo, num, (int)LayerIndex.world.mask | (int)LayerIndex.CommonMasks.characterBodiesOrDefault))
		{
			point = hitInfo.point;
		}
		laserLineComponent.SetPosition(0, position);
		laserLineComponent.SetPosition(1, point);
		float num2;
		if (duration - base.age > 0.5f)
		{
			num2 = base.age / duration;
		}
		else
		{
			flashTimer -= Time.deltaTime;
			if (flashTimer <= 0f)
			{
				laserOn = !laserOn;
				flashTimer = 1f / 30f;
			}
			num2 = (laserOn ? 1f : 0f);
		}
		num2 *= laserMaxWidth;
		laserLineComponent.startWidth = num2;
		laserLineComponent.endWidth = num2;
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration && base.isAuthority)
		{
			EntityState nextState = fireState;
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
