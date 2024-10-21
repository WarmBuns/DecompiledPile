using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BeetleGuardMonster;

public class DefenseUp : BaseState
{
	public static float baseDuration = 3.5f;

	public static float buffDuration = 8f;

	public static GameObject defenseUpPrefab;

	private Animator modelAnimator;

	private float duration;

	private bool hasCastBuff;

	private EffectManagerHelper _emh_defenseUp;

	public override void Reset()
	{
		base.Reset();
		modelAnimator = null;
		duration = 0f;
		hasCastBuff = false;
		_emh_defenseUp = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayCrossfade("Body", "DefenseUp", "DefenseUp.playbackRate", duration, 0.2f);
		}
	}

	public override void OnExit()
	{
		if (_emh_defenseUp != null && _emh_defenseUp.OwningPool != null)
		{
			_emh_defenseUp.OwningPool.ReturnObject(_emh_defenseUp);
			_emh_defenseUp = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if ((bool)modelAnimator && modelAnimator.GetFloat("DefenseUp.activate") > 0.5f && !hasCastBuff)
		{
			GameObject gameObject = null;
			if (!EffectManager.ShouldUsePooledEffect(defenseUpPrefab))
			{
				gameObject = Object.Instantiate(defenseUpPrefab, base.transform.position, Quaternion.identity, base.transform);
			}
			else
			{
				_emh_defenseUp = EffectManager.GetAndActivatePooledEffect(defenseUpPrefab, base.transform.position, Quaternion.identity, base.transform);
				gameObject = _emh_defenseUp.gameObject;
			}
			ScaleParticleSystemDuration component = gameObject.GetComponent<ScaleParticleSystemDuration>();
			if ((bool)component)
			{
				component.newDuration = buffDuration;
			}
			hasCastBuff = true;
			if (NetworkServer.active)
			{
				base.characterBody.AddTimedBuff(JunkContent.Buffs.EnrageAncientWisp, buffDuration);
			}
		}
		if (base.fixedAge >= duration && base.isAuthority)
		{
			outer.SetNextStateToMain();
		}
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
