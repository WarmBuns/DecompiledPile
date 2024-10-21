using RoR2;
using UnityEngine;

namespace EntityStates.Toolbot;

public class FireBuzzsaw : BaseToolbotPrimarySkillState
{
	public static float damageCoefficientPerSecond;

	public static float procCoefficientPerSecond = 1f;

	public static string fireSoundString;

	public static string impactSoundString;

	public static string spinUpSoundString;

	public static string spinDownSoundString;

	public static float spreadBloomValue = 0.2f;

	public static float baseFireFrequency;

	public static GameObject spinEffectPrefab;

	public static GameObject spinImpactEffectPrefab;

	public static GameObject impactEffectPrefab;

	public static float selfForceMagnitude;

	private OverlapAttack attack;

	private float fireFrequency;

	private float fireAge;

	private GameObject spinEffectInstance;

	private GameObject spinImpactEffectInstance;

	private bool hitOverlapLastTick;

	protected EffectManagerHelper _emh_spinEffectInstance;

	protected EffectManagerHelper _emh_spinImpactEffectInstance;

	private static int SpinBuzzsawStateHash = Animator.StringToHash("SpinBuzzsaw");

	private static int EnterBuzzsawStateHash = Animator.StringToHash("EnterBuzzsaw");

	private static int EmptyStateHash = Animator.StringToHash("Empty");

	private static int ExitBuzzsawStateHash = Animator.StringToHash("ExitBuzzsaw");

	private static int ImpactBuzzsawStateHash = Animator.StringToHash("ImpactBuzzsaw");

	public override string baseMuzzleName => "MuzzleBuzzsaw";

	public override void Reset()
	{
		base.Reset();
		if (attack != null)
		{
			attack.Reset();
		}
		fireFrequency = 0f;
		fireAge = 0f;
		spinEffectInstance = null;
		spinImpactEffectInstance = null;
		base.muzzleTransform = null;
		hitOverlapLastTick = false;
		_emh_spinEffectInstance = null;
		_emh_spinImpactEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		fireFrequency = baseFireFrequency * attackSpeedStat;
		Transform modelTransform = GetModelTransform();
		Util.PlaySound(spinUpSoundString, base.gameObject);
		Util.PlaySound(fireSoundString, base.gameObject);
		if (!base.isInDualWield)
		{
			PlayAnimation("Gesture, Additive Gun", SpinBuzzsawStateHash);
			PlayAnimation("Gesture, Additive", EnterBuzzsawStateHash);
		}
		attack = new OverlapAttack();
		attack.attacker = base.gameObject;
		attack.inflictor = base.gameObject;
		attack.teamIndex = TeamComponent.GetObjectTeam(attack.attacker);
		attack.damage = damageCoefficientPerSecond * damageStat / baseFireFrequency;
		attack.procCoefficient = procCoefficientPerSecond / baseFireFrequency;
		if ((bool)impactEffectPrefab)
		{
			attack.hitEffectPrefab = impactEffectPrefab;
		}
		if ((bool)modelTransform)
		{
			string groupName = "Buzzsaw";
			if (base.isInDualWield)
			{
				if (base.currentHand == -1)
				{
					groupName = "BuzzsawL";
				}
				else if (base.currentHand == 1)
				{
					groupName = "BuzzsawR";
				}
			}
			attack.hitBoxGroup = HitBoxGroup.FindByGroupName(modelTransform.gameObject, groupName);
		}
		if ((bool)base.muzzleTransform)
		{
			if ((bool)spinEffectPrefab)
			{
				if (!EffectManager.ShouldUsePooledEffect(spinEffectPrefab))
				{
					spinEffectInstance = Object.Instantiate(spinEffectPrefab, base.muzzleTransform.position, base.muzzleTransform.rotation);
				}
				else
				{
					_emh_spinEffectInstance = EffectManager.GetAndActivatePooledEffect(spinEffectPrefab, base.muzzleTransform.position, base.muzzleTransform.rotation);
					spinEffectInstance = _emh_spinEffectInstance.gameObject;
				}
				spinEffectInstance.transform.parent = base.muzzleTransform;
				spinEffectInstance.transform.localScale = Vector3.one;
			}
			if ((bool)spinImpactEffectPrefab)
			{
				if (!EffectManager.ShouldUsePooledEffect(spinImpactEffectPrefab))
				{
					spinImpactEffectInstance = Object.Instantiate(spinImpactEffectPrefab, base.muzzleTransform.position, base.muzzleTransform.rotation);
				}
				else
				{
					_emh_spinImpactEffectInstance = EffectManager.GetAndActivatePooledEffect(spinImpactEffectPrefab, base.muzzleTransform.position, base.muzzleTransform.rotation);
					spinImpactEffectInstance = _emh_spinImpactEffectInstance.gameObject;
				}
				spinImpactEffectInstance.transform.parent = base.muzzleTransform;
				spinImpactEffectInstance.transform.localScale = Vector3.one;
				spinImpactEffectInstance.gameObject.SetActive(value: false);
			}
		}
		attack.isCrit = Util.CheckRoll(critStat, base.characterBody.master);
	}

	public override void OnExit()
	{
		base.OnExit();
		Util.PlaySound(spinDownSoundString, base.gameObject);
		if (!base.isInDualWield)
		{
			PlayAnimation("Gesture, Additive Gun", EmptyStateHash);
			PlayAnimation("Gesture, Additive", ExitBuzzsawStateHash);
		}
		if ((bool)spinEffectInstance)
		{
			if (!EffectManager.UsePools)
			{
				EntityState.Destroy(spinEffectInstance);
			}
			else
			{
				if (_emh_spinEffectInstance != null && _emh_spinEffectInstance.OwningPool != null)
				{
					_emh_spinEffectInstance.OwningPool.ReturnObject(_emh_spinEffectInstance);
				}
				else
				{
					EntityState.Destroy(spinEffectInstance);
				}
				_emh_spinEffectInstance = null;
			}
		}
		if ((bool)spinImpactEffectInstance)
		{
			EntityState.Destroy(spinImpactEffectInstance);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		fireAge += GetDeltaTime();
		base.characterBody.SetAimTimer(2f);
		attackSpeedStat = base.characterBody.attackSpeed;
		fireFrequency = baseFireFrequency * attackSpeedStat;
		if (fireAge >= 1f / fireFrequency && base.isAuthority)
		{
			fireAge = 0f;
			attack.ResetIgnoredHealthComponents();
			attack.isCrit = base.characterBody.RollCrit();
			hitOverlapLastTick = attack.Fire();
			if (hitOverlapLastTick)
			{
				Vector3 normalized = (attack.lastFireAverageHitPosition - GetAimRay().origin).normalized;
				if ((bool)base.characterMotor)
				{
					base.characterMotor.ApplyForce(normalized * selfForceMagnitude);
				}
				Util.PlaySound(impactSoundString, base.gameObject);
				if (!base.isInDualWield)
				{
					PlayAnimation("Gesture, Additive", ImpactBuzzsawStateHash);
				}
			}
			base.characterBody.AddSpreadBloom(spreadBloomValue);
			if (!IsKeyDownAuthority() || (object)base.skillDef != base.activatorSkillSlot.skillDef)
			{
				outer.SetNextStateToMain();
			}
		}
		spinImpactEffectInstance.gameObject.SetActive(hitOverlapLastTick);
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
