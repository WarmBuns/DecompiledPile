using EntityStates.VagrantMonster;
using RoR2;
using UnityEngine;

namespace EntityStates.VagrantNovaItem;

public class ChargeState : BaseVagrantNovaItemState
{
	public static float baseDuration = 3f;

	public static string chargeSound;

	private float duration;

	private GameObject chargeVfxInstance;

	private GameObject areaIndicatorVfxInstance;

	public override void OnEnter()
	{
		base.OnEnter();
		duration = baseDuration / (base.attachedBody ? base.attachedBody.attackSpeed : 1f);
		SetChargeSparkEmissionRateMultiplier(1f);
		if ((bool)base.attachedBody)
		{
			Vector3 position = base.transform.position;
			Quaternion rotation = base.transform.rotation;
			chargeVfxInstance = Object.Instantiate(ChargeMegaNova.chargingEffectPrefab, position, rotation);
			chargeVfxInstance.transform.localScale = Vector3.one * 0.25f;
			Util.PlaySound(chargeSound, base.gameObject);
			areaIndicatorVfxInstance = Object.Instantiate(ChargeMegaNova.areaIndicatorPrefab, position, rotation);
			ObjectScaleCurve component = areaIndicatorVfxInstance.GetComponent<ObjectScaleCurve>();
			component.timeMax = duration;
			component.baseScale = Vector3.one * DetonateState.blastRadius * 2f;
			areaIndicatorVfxInstance.GetComponent<AnimateShaderAlpha>().timeMax = duration;
		}
		RoR2Application.onLateUpdate += OnLateUpdate;
	}

	public override void OnExit()
	{
		RoR2Application.onLateUpdate -= OnLateUpdate;
		if ((object)chargeVfxInstance != null)
		{
			EntityState.Destroy(chargeVfxInstance);
			chargeVfxInstance = null;
		}
		if ((object)areaIndicatorVfxInstance != null)
		{
			EntityState.Destroy(areaIndicatorVfxInstance);
			areaIndicatorVfxInstance = null;
		}
		base.OnExit();
	}

	private void OnLateUpdate()
	{
		if ((bool)chargeVfxInstance)
		{
			chargeVfxInstance.transform.position = base.transform.position;
		}
		if ((bool)areaIndicatorVfxInstance)
		{
			areaIndicatorVfxInstance.transform.position = base.transform.position;
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.isAuthority && base.fixedAge >= duration)
		{
			outer.SetNextState(new DetonateState());
		}
	}
}
