using RoR2;
using UnityEngine;

namespace EntityStates.Treebot.Weapon;

public class ChargeSonicBoom : BaseState
{
	[SerializeField]
	public string sound;

	[SerializeField]
	public GameObject chargeEffectPrefab;

	public static string muzzleName;

	public static float baseDuration;

	private float duration;

	private GameObject chargeEffect;

	private EffectManagerHelper _emh_chargeEffect;

	public override void Reset()
	{
		base.Reset();
		duration = 0f;
		chargeEffect = null;
		_emh_chargeEffect = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / attackSpeedStat;
		Util.PlaySound(sound, base.gameObject);
		base.characterBody.SetAimTimer(3f);
		if (!chargeEffectPrefab)
		{
			return;
		}
		Transform transform = FindModelChild(muzzleName);
		if ((bool)transform)
		{
			if (!EffectManager.ShouldUsePooledEffect(chargeEffectPrefab))
			{
				chargeEffect = Object.Instantiate(chargeEffectPrefab, transform);
			}
			else
			{
				_emh_chargeEffect = EffectManager.GetAndActivatePooledEffect(chargeEffectPrefab, transform, inResetLocal: true);
				chargeEffect = _emh_chargeEffect.gameObject;
			}
			chargeEffect.transform.localPosition = Vector3.zero;
			chargeEffect.transform.localRotation = Quaternion.identity;
		}
	}

	public override void OnExit()
	{
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
			chargeEffect = null;
			_emh_chargeEffect = null;
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= duration)
		{
			outer.SetNextState(GetNextState());
		}
	}

	protected virtual EntityState GetNextState()
	{
		return new FireSonicBoom();
	}

	public override InterruptPriority GetMinimumInterruptPriority()
	{
		return InterruptPriority.PrioritySkill;
	}
}
