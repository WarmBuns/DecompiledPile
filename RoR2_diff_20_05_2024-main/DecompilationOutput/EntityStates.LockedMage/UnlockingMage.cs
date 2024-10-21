using System;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.LockedMage;

public class UnlockingMage : BaseState
{
	public static GameObject unlockingMageChargeEffectPrefab;

	public static GameObject unlockingMageExplosionEffectPrefab;

	public static float unlockingDuration;

	public static string unlockingChargeSFXString;

	public static float unlockingChargeSFXStringPitch;

	public static string unlockingExplosionSFXString;

	public static float unlockingExplosionSFXStringPitch;

	private bool unlocked;

	public static event Action<Interactor> onOpened;

	public override void OnEnter()
	{
		base.OnEnter();
		EffectManager.SimpleEffect(unlockingMageChargeEffectPrefab, base.transform.position, Util.QuaternionSafeLookRotation(Vector3.up), transmit: false);
		Util.PlayAttackSpeedSound(unlockingChargeSFXString, base.gameObject, unlockingChargeSFXStringPitch);
		GetModelTransform().GetComponent<ChildLocator>().FindChild("Suspension").gameObject.SetActive(value: false);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!(base.fixedAge >= unlockingDuration) || unlocked)
		{
			return;
		}
		base.gameObject.SetActive(value: false);
		EffectManager.SimpleEffect(unlockingMageExplosionEffectPrefab, base.transform.position, Util.QuaternionSafeLookRotation(Vector3.up), transmit: false);
		Util.PlayAttackSpeedSound(unlockingExplosionSFXString, base.gameObject, unlockingExplosionSFXStringPitch);
		unlocked = true;
		if (NetworkServer.active)
		{
			PurchaseInteraction component = GetComponent<PurchaseInteraction>();
			if ((bool)component && (bool)component.lastActivator)
			{
				UnlockingMage.onOpened?.Invoke(component.lastActivator);
			}
		}
	}
}
