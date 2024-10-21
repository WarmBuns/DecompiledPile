using RoR2;
using UnityEngine;

namespace EntityStates.Duplicator;

public class Duplicating : BaseState
{
	public static float initialDelayDuration = 1f;

	public static float timeBetweenStartAndDropDroplet = 3f;

	public static string muzzleString;

	public static GameObject bakeEffectPrefab;

	public static GameObject releaseEffectPrefab;

	private GameObject bakeEffectInstance;

	private bool hasStartedCooking;

	private bool hasDroppedDroplet;

	private Transform muzzleTransform;

	private EffectManagerHelper _emh_bakeEffectInstance;

	private static int CookStateHash = Animator.StringToHash("Cook");

	public override void Reset()
	{
		base.Reset();
		bakeEffectInstance = null;
		hasStartedCooking = false;
		hasDroppedDroplet = false;
		muzzleTransform = null;
		_emh_bakeEffectInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		ChildLocator component = GetModelTransform().GetComponent<ChildLocator>();
		if ((bool)component)
		{
			muzzleTransform = component.FindChild(muzzleString);
		}
	}

	private void BeginCooking()
	{
		if (hasStartedCooking)
		{
			return;
		}
		hasStartedCooking = true;
		PlayAnimation("Body", CookStateHash);
		if ((bool)base.sfxLocator)
		{
			Util.PlaySound(base.sfxLocator.openSound, base.gameObject);
		}
		if ((bool)muzzleTransform)
		{
			if (!EffectManager.ShouldUsePooledEffect(bakeEffectPrefab))
			{
				bakeEffectInstance = Object.Instantiate(bakeEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
				return;
			}
			_emh_bakeEffectInstance = EffectManager.GetAndActivatePooledEffect(bakeEffectPrefab, muzzleTransform.position, muzzleTransform.rotation);
			bakeEffectInstance = _emh_bakeEffectInstance.gameObject;
		}
	}

	private void DropDroplet()
	{
		if (hasDroppedDroplet)
		{
			return;
		}
		hasDroppedDroplet = true;
		GetComponent<ShopTerminalBehavior>().DropPickup();
		if (!muzzleTransform)
		{
			return;
		}
		if ((bool)bakeEffectInstance)
		{
			if (_emh_bakeEffectInstance != null && _emh_bakeEffectInstance.OwningPool != null)
			{
				_emh_bakeEffectInstance.OwningPool.ReturnObject(_emh_bakeEffectInstance);
			}
			else
			{
				EntityState.Destroy(bakeEffectInstance);
			}
			_emh_bakeEffectInstance = null;
			bakeEffectInstance = null;
		}
		if ((bool)releaseEffectPrefab)
		{
			EffectManager.SimpleMuzzleFlash(releaseEffectPrefab, base.gameObject, muzzleString, transmit: false);
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (base.fixedAge >= initialDelayDuration)
		{
			BeginCooking();
		}
		if (base.fixedAge >= initialDelayDuration + timeBetweenStartAndDropDroplet)
		{
			DropDroplet();
		}
	}
}
