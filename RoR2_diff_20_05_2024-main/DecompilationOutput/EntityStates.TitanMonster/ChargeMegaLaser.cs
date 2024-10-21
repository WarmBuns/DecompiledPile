using System.Linq;
using RoR2;
using UnityEngine;

namespace EntityStates.TitanMonster;

public class ChargeMegaLaser : BaseState
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

	private EffectManagerHelper _emh_chargeEffect;

	private EffectManagerHelper _emh_laserEffect;

	public override void Reset()
	{
		base.Reset();
		lockedOnHurtBox = null;
		duration = 0f;
		chargeEffect = null;
		laserEffect = null;
		laserLineComponent = null;
		visualEndPosition = Vector3.zero;
		flashTimer = 0f;
		laserOn = false;
		if (enemyFinder != null)
		{
			enemyFinder.Reset();
		}
		_emh_chargeEffect = null;
		_emh_laserEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		Util.PlayAttackSpeedSound(chargeAttackSoundString, base.gameObject, 2.1f / duration);
		Ray aimRay = GetAimRay();
		enemyFinder = new BullseyeSearch();
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
						if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
						{
							chargeEffect = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
						}
						else
						{
							_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform.position, transform.rotation);
							chargeEffect = _emh_chargeEffect.gameObject;
						}
						chargeEffect.transform.parent = transform;
						ScaleParticleSystemDuration component2 = chargeEffect.GetComponent<ScaleParticleSystemDuration>();
						if ((bool)component2)
						{
							component2.newDuration = duration;
						}
					}
					if ((bool)laserPrefab)
					{
						if (!EffectManager.ShouldUsePooledEffect(laserPrefab))
						{
							laserEffect = Object.Instantiate(laserPrefab, transform.position, transform.rotation);
						}
						else
						{
							_emh_laserEffect = EffectManager.GetAndActivatePooledEffect(laserPrefab, transform.position, transform.rotation);
							laserEffect = _emh_laserEffect.gameObject;
						}
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
		base.OnExit();
		if ((bool)chargeEffect)
		{
			if (_emh_chargeEffect != null && _emh_chargeEffect.OwningPool != null)
			{
				_emh_chargeEffect.OwningPool.ReturnObject(_emh_chargeEffect);
			}
			else
			{
				EntityState.Destroy(chargeEffect);
			}
			_emh_chargeEffect = null;
			chargeEffect = null;
		}
		if ((bool)laserEffect)
		{
			if (_emh_laserEffect != null && _emh_laserEffect.OwningPool != null)
			{
				_emh_laserEffect.OwningPool.ReturnObject(_emh_laserEffect);
			}
			else
			{
				EntityState.Destroy(laserEffect);
			}
			_emh_laserEffect = null;
			laserEffect = null;
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
			FireMegaLaser nextState = new FireMegaLaser();
			outer.SetNextState(nextState);
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
