using RoR2;
using UnityEngine;

namespace EntityStates.AncientWispMonster;

public class ChargeBomb : BaseState
{
	public static float baseDuration = 3f;

	public static GameObject effectPrefab;

	public static GameObject delayPrefab;

	public static float radius = 10f;

	public static float damageCoefficient = 1f;

	private float duration;

	private GameObject chargeEffectLeft;

	private GameObject chargeEffectRight;

	private Vector3 startLine = Vector3.zero;

	private Vector3 endLine = Vector3.zero;

	private bool hasFired;

	private EffectManagerHelper _emh_chargeEffectLeft;

	private EffectManagerHelper _emh_chargeEffectRight;

	private static int chargeBombAnimStateHash = Animator.StringToHash("ChargeBomb");

	private static int chargeBombParamHas = Animator.StringToHash("ChargeBomb.playbackRate");

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffectLeft = null;
		chargeEffectRight = null;
		startLine = Vector3.zero;
		endLine = Vector3.zero;
		hasFired = false;
		_emh_chargeEffectLeft = null;
		_emh_chargeEffectRight = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		PlayAnimation("Gesture", chargeBombAnimStateHash, chargeBombParamHas, duration);
		Transform modelTransform = GetModelTransform();
		if ((bool)modelTransform)
		{
			ChildLocator component = modelTransform.GetComponent<ChildLocator>();
			if ((bool)component && (bool)effectPrefab)
			{
				Transform transform = component.FindChild("MuzzleLeft");
				Transform transform2 = component.FindChild("MuzzleRight");
				if ((bool)transform)
				{
					if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
					{
						chargeEffectLeft = Object.Instantiate(effectPrefab, transform.position, transform.rotation);
					}
					else
					{
						_emh_chargeEffectLeft = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform.position, transform.rotation);
						chargeEffectLeft = _emh_chargeEffectLeft.gameObject;
					}
					chargeEffectLeft.transform.parent = transform;
				}
				if ((bool)transform2)
				{
					if (!EffectManager.ShouldUsePooledEffect(effectPrefab))
					{
						chargeEffectRight = Object.Instantiate(effectPrefab, transform2.position, transform2.rotation);
					}
					else
					{
						_emh_chargeEffectRight = EffectManager.GetAndActivatePooledEffect(effectPrefab, transform2.position, transform2.rotation);
						chargeEffectRight = _emh_chargeEffectRight.gameObject;
					}
					chargeEffectRight.transform.parent = transform2;
				}
			}
		}
		if ((bool)base.characterBody)
		{
			base.characterBody.SetAimTimer(duration);
		}
		if (Physics.Raycast(GetAimRay(), out var hitInfo, (int)LayerIndex.world.mask))
		{
			startLine = hitInfo.point;
		}
	}

	public override void OnExit()
	{
		base.OnExit();
		if (_emh_chargeEffectLeft != null && _emh_chargeEffectLeft.OwningPool != null)
		{
			_emh_chargeEffectLeft.OwningPool.ReturnObject(_emh_chargeEffectLeft);
		}
		else
		{
			EntityState.Destroy(chargeEffectLeft);
		}
		chargeEffectLeft = null;
		_emh_chargeEffectLeft = null;
		if (_emh_chargeEffectRight != null && _emh_chargeEffectRight.OwningPool != null)
		{
			_emh_chargeEffectRight.OwningPool.ReturnObject(_emh_chargeEffectRight);
		}
		else
		{
			EntityState.Destroy(chargeEffectRight);
		}
		chargeEffectRight = null;
		_emh_chargeEffectRight = null;
	}

	public override void Update()
	{
		base.Update();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		float num = 0f;
		if (base.fixedAge >= num && !hasFired)
		{
			hasFired = true;
			Ray aimRay = GetAimRay();
			if (Physics.Raycast(aimRay, out var hitInfo, (int)LayerIndex.world.mask))
			{
				endLine = hitInfo.point;
			}
			Vector3 normalized = (endLine - startLine).normalized;
			normalized.y = 0f;
			normalized.Normalize();
			for (int i = 0; i < 1; i++)
			{
				Vector3 vector = endLine;
				Ray ray = default(Ray);
				ray.origin = aimRay.origin;
				ray.direction = vector - aimRay.origin;
				Debug.DrawLine(ray.origin, vector, Color.red, 5f);
				if (Physics.Raycast(ray, out hitInfo, 500f, LayerIndex.world.mask))
				{
					Vector3 point = hitInfo.point;
					Quaternion rotation = Util.QuaternionSafeLookRotation(hitInfo.normal);
					GameObject obj = Object.Instantiate(delayPrefab, point, rotation);
					DelayBlast component = obj.GetComponent<DelayBlast>();
					component.position = point;
					component.baseDamage = base.characterBody.damage * damageCoefficient;
					component.baseForce = 2000f;
					component.bonusForce = Vector3.up * 1000f;
					component.radius = radius;
					component.attacker = base.gameObject;
					component.inflictor = null;
					component.crit = Util.CheckRoll(critStat, base.characterBody.master);
					component.maxTimer = duration;
					obj.GetComponent<TeamFilter>().teamIndex = TeamComponent.GetObjectTeam(component.attacker);
					obj.transform.localScale = new Vector3(radius, radius, 1f);
					ScaleParticleSystemDuration component2 = obj.GetComponent<ScaleParticleSystemDuration>();
					if ((bool)component2)
					{
						component2.newDuration = duration;
					}
				}
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextState(new FireBomb());
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.Skill;
	}
}
