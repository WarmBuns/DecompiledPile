using RoR2;
using UnityEngine;

namespace EntityStates.EngiTurret.EngiTurretWeapon;

public class FireBeam : BaseState
{
	[SerializeField]
	public GameObject effectPrefab;

	[SerializeField]
	public GameObject hitEffectPrefab;

	[SerializeField]
	public GameObject laserPrefab;

	[SerializeField]
	public string muzzleString;

	[SerializeField]
	public string attackSoundString;

	[SerializeField]
	public float damageCoefficient;

	[SerializeField]
	public float procCoefficient;

	[SerializeField]
	public float force;

	[SerializeField]
	public float minSpread;

	[SerializeField]
	public float maxSpread;

	[SerializeField]
	public int bulletCount;

	[SerializeField]
	public float fireFrequency;

	[SerializeField]
	public float maxDistance;

	private float fireTimer;

	private Ray laserRay;

	private Transform modelTransform;

	private GameObject laserEffectInstance;

	private Transform laserEffectInstanceEndTransform;

	private Vector3 laserEndPoint;

	private float prevLaserDistance;

	private bool forceCast;

	private BulletAttack bulletAttack;

	private EffectManagerHelper _emh_laserEffect;

	public override void Reset()
	{
		base.Reset();
		fireTimer = 0f;
		laserEndPoint = Vector3.zero;
		modelTransform = null;
		laserEffectInstance = null;
		laserEffectInstanceEndTransform = null;
		forceCast = true;
		if (bulletAttack != null)
		{
			bulletAttack.Reset();
		}
		_emh_laserEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		Util.PlaySound(attackSoundString, base.gameObject);
		fireTimer = 0f;
		forceCast = true;
		modelTransform = GetModelTransform();
		if (!modelTransform)
		{
			return;
		}
		ChildLocator component = modelTransform.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild(muzzleString);
		if ((bool)transform && (bool)laserPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(laserPrefab))
			{
				laserEffectInstance = Object.Instantiate(laserPrefab, transform.position, transform.rotation);
			}
			else
			{
				_emh_laserEffect = EffectManager.GetAndActivatePooledEffect(laserPrefab, transform.position, transform.rotation);
				laserEffectInstance = _emh_laserEffect.gameObject;
			}
			if ((bool)laserEffectInstance)
			{
				ChildLocator component2 = laserEffectInstance.GetComponent<ChildLocator>();
				laserEffectInstanceEndTransform = component2.FindChild("LaserEnd");
			}
			laserEffectInstance.transform.parent = transform;
		}
	}

	public override void OnExit()
	{
		if ((bool)laserEffectInstance)
		{
			if (_emh_laserEffect != null && _emh_laserEffect.OwningPool != null)
			{
				_emh_laserEffect.OwningPool.ReturnObject(_emh_laserEffect);
			}
			else
			{
				EntityState.Destroy(laserEffectInstance);
			}
			laserEffectInstance = null;
			_emh_laserEffect = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!(Time.deltaTime > 0f))
		{
			return;
		}
		laserRay = GetLaserRay();
		fireTimer += Time.deltaTime;
		float num = fireFrequency * base.characterBody.attackSpeed;
		float num2 = 1f / num;
		if (fireTimer > num2)
		{
			if (base.isAuthority)
			{
				laserEndPoint = FireBullet(laserRay, muzzleString);
				prevLaserDistance = (laserEndPoint - laserRay.origin).magnitude;
			}
			else
			{
				forceCast = true;
			}
			fireTimer = 0f;
		}
		else if (!forceCast && prevLaserDistance > float.Epsilon)
		{
			laserEndPoint = laserRay.origin + laserRay.direction * prevLaserDistance;
		}
		if (forceCast)
		{
			forceCast = false;
			if ((bool)laserEffectInstance && (bool)laserEffectInstanceEndTransform)
			{
				float distance = maxDistance;
				_ = laserEffectInstance.transform.parent.position;
				laserEndPoint = laserRay.GetPoint(distance);
				if (Util.CharacterRaycast(base.gameObject, laserRay, out var hitInfo, distance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
				{
					laserEndPoint = hitInfo.point;
					prevLaserDistance = (laserEndPoint - laserRay.origin).magnitude;
				}
			}
		}
		if ((bool)laserEffectInstance && (bool)laserEffectInstanceEndTransform)
		{
			laserEffectInstanceEndTransform.position = laserEndPoint;
		}
		if (base.isAuthority && !ShouldFireLaser())
		{
			outer.SetNextStateToMain();
		}
	}

	protected Vector3 GetBeamEndPoint()
	{
		Vector3 point = laserRay.GetPoint(maxDistance);
		if (Util.CharacterRaycast(base.gameObject, laserRay, out var hitInfo, maxDistance, (int)LayerIndex.world.mask | (int)LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
		{
			point = hitInfo.point;
		}
		return point;
	}

	protected virtual EntityState GetNextState()
	{
		return EntityStateCatalog.InstantiateState(ref outer.mainStateType);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}

	public virtual void ModifyBullet(BulletAttack bulletAttack)
	{
		bulletAttack.damageType |= (DamageTypeCombo)DamageType.SlowOnHit;
	}

	public virtual bool ShouldFireLaser()
	{
		if ((bool)base.inputBank)
		{
			return base.inputBank.skill1.down;
		}
		return false;
	}

	public virtual Ray GetLaserRay()
	{
		return GetAimRay();
	}

	private Vector3 FireBullet(Ray laserRay, string targetMuzzle)
	{
		if ((bool)effectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(effectPrefab, base.gameObject, targetMuzzle, transmit: false);
		}
		if (base.isAuthority)
		{
			if (bulletAttack == null)
			{
				bulletAttack = new BulletAttack();
			}
			bulletAttack.owner = base.gameObject;
			bulletAttack.weapon = base.gameObject;
			bulletAttack.origin = laserRay.origin;
			bulletAttack.aimVector = laserRay.direction;
			bulletAttack.minSpread = minSpread;
			bulletAttack.maxSpread = maxSpread;
			bulletAttack.bulletCount = 1u;
			bulletAttack.damage = damageCoefficient * damageStat / fireFrequency;
			bulletAttack.procCoefficient = procCoefficient / fireFrequency;
			bulletAttack.force = force;
			bulletAttack.muzzleName = targetMuzzle;
			bulletAttack.hitEffectPrefab = hitEffectPrefab;
			bulletAttack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
			bulletAttack.HitEffectNormal = false;
			bulletAttack.radius = 0f;
			bulletAttack.maxDistance = maxDistance;
			ModifyBullet(bulletAttack);
			return bulletAttack.Fire_ReturnHit();
		}
		return Vector3.zero;
	}
}
