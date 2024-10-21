using RoR2;
using RoR2.CharacterAI;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.Scorchling;

public class ScorchlingBreach : BaseState
{
	[Header("Timing")]
	[SerializeField]
	public float crackToBreachTime = 1f;

	[SerializeField]
	public float breachToBurrow = 5f;

	[SerializeField]
	public float burrowToEndOfTime = 1f;

	[SerializeField]
	public float animDuration = 1f;

	[Header("Explosion Attr")]
	[SerializeField]
	public float blastProcCoefficient;

	[SerializeField]
	public float blastDamageCoefficient;

	[SerializeField]
	public float blastForce;

	[SerializeField]
	public Vector3 blastBonusForce;

	[SerializeField]
	public float knockbackForce;

	[Header("FX Objects")]
	[SerializeField]
	public GameObject crackEffectPrefab;

	[SerializeField]
	public float crackRadius = 1f;

	[SerializeField]
	public GameObject blastEffectPrefab;

	[SerializeField]
	public GameObject blastImpactEffectPrefab;

	[SerializeField]
	public float blastRadius = 1f;

	[SerializeField]
	public GameObject burrowEffectPrefab;

	[SerializeField]
	public float burrowRadius = 1f;

	[Header("Sound Strings")]
	[SerializeField]
	public string preBreachSoundString;

	[SerializeField]
	public string breachSoundString;

	[SerializeField]
	public string burrowSoundString;

	[SerializeField]
	public string burrowLoopSoundString;

	[SerializeField]
	public string burrowStopLoopSoundString;

	public bool proceedImmediatelyToLavaBomb;

	private bool breached;

	private bool burrowed;

	private bool amServer;

	private Vector3 breachPosition;

	private ScorchlingController scorchlingController;

	private CharacterBody enemyCBody;

	public override void OnEnter()
	{
		base.OnEnter();
		amServer = NetworkServer.active;
		scorchlingController = base.characterBody.GetComponent<ScorchlingController>();
		Util.PlaySound(preBreachSoundString, base.gameObject);
		if (amServer)
		{
			enemyCBody = base.characterBody.master.GetComponent<BaseAI>().currentEnemy?.characterBody;
			if (proceedImmediatelyToLavaBomb)
			{
				breachToBurrow = 1f;
			}
			breachToBurrow += crackToBreachTime;
			burrowToEndOfTime += breachToBurrow;
			breachPosition = base.characterBody.footPosition;
			if (!proceedImmediatelyToLavaBomb && (bool)enemyCBody)
			{
				breachPosition = enemyCBody.footPosition;
			}
			if ((bool)base.characterMotor)
			{
				base.characterMotor.walkSpeedPenaltyCoefficient = 0f;
			}
			base.characterBody.SetAimTimer(breachToBurrow);
			TeleportHelper.TeleportBody(base.characterBody, breachPosition);
			EffectManager.SpawnEffect(crackEffectPrefab, new EffectData
			{
				origin = breachPosition,
				scale = crackRadius
			}, transmit: true);
			scorchlingController.SetTeleportPermission(b: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (amServer && base.fixedAge < crackToBreachTime && enemyCBody != null)
		{
			Vector3 normalized = (enemyCBody.corePosition - base.characterBody.corePosition).normalized;
			base.characterBody.characterDirection.forward = normalized;
		}
		if (!breached && base.fixedAge > crackToBreachTime)
		{
			breached = true;
			scorchlingController.Breach();
			PlayAnimation("FullBody, Override", "Breach", "Breach.playbackRate", animDuration);
			Util.PlaySound(breachSoundString, base.gameObject);
			Util.PlaySound(burrowStopLoopSoundString, base.gameObject);
			if (amServer)
			{
				DetonateAuthority();
			}
		}
		if (base.fixedAge > burrowToEndOfTime)
		{
			DoExit();
		}
	}

	private void DoExit()
	{
		if (proceedImmediatelyToLavaBomb)
		{
			outer.SetNextState(new ScorchlingLavaBomb());
		}
		else
		{
			outer.SetNextStateToMain();
		}
	}

	protected BlastAttack.Result DetonateAuthority()
	{
		EffectManager.SpawnEffect(blastEffectPrefab, new EffectData
		{
			origin = breachPosition,
			scale = blastRadius
		}, transmit: true);
		return new BlastAttack
		{
			attacker = base.gameObject,
			baseDamage = damageStat * blastDamageCoefficient,
			baseForce = blastForce,
			bonusForce = blastBonusForce,
			crit = RollCrit(),
			damageType = DamageType.Stun1s,
			falloffModel = BlastAttack.FalloffModel.None,
			procCoefficient = blastProcCoefficient,
			radius = blastRadius,
			position = breachPosition,
			attackerFiltering = AttackerFiltering.NeverHitSelf,
			impactEffect = EffectCatalog.FindEffectIndexFromPrefab(blastImpactEffectPrefab),
			teamIndex = base.teamComponent.teamIndex
		}.Fire();
	}

	public override void OnExit()
	{
		base.OnExit();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		if (proceedImmediatelyToLavaBomb)
		{
			return InterruptPriority.Frozen;
		}
		return InterruptPriority.PrioritySkill;
	}
}
