using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace EntityStates.Huntress;

public class ArrowRain : BaseArrowBarrage
{
	public static float arrowRainRadius = 0f;

	public static float damageCoefficient;

	public static GameObject projectilePrefab;

	public static GameObject areaIndicatorPrefab;

	public static GameObject muzzleFlashEffect;

	private GameObject areaIndicatorInstance;

	private bool shouldFireArrowRain;

	private EffectManagerHelper _emh_areaIndicatorInstance;

	private Vector3 _cachedAreaIndicatorScale = Vector3.one;

	private static int LoopArrowRainStateHash = Animator.StringToHash("LoopArrowRain");

	public override void Reset()
	{
		base.Reset();
		areaIndicatorInstance = null;
		shouldFireArrowRain = false;
		_emh_areaIndicatorInstance = null;
	}

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("FullBody, Override", LoopArrowRainStateHash);
		if ((bool)areaIndicatorPrefab)
		{
			if (!EffectManager.ShouldUsePooledEffect(areaIndicatorPrefab))
			{
				areaIndicatorInstance = Object.Instantiate(areaIndicatorPrefab);
			}
			else
			{
				_emh_areaIndicatorInstance = EffectManager.GetAndActivatePooledEffect(areaIndicatorPrefab, Vector3.zero, Quaternion.identity);
				areaIndicatorInstance = _emh_areaIndicatorInstance.gameObject;
			}
			if (areaIndicatorInstance != null)
			{
				_cachedAreaIndicatorScale.x = arrowRainRadius;
				_cachedAreaIndicatorScale.y = arrowRainRadius;
				_cachedAreaIndicatorScale.z = arrowRainRadius;
				areaIndicatorInstance.transform.localScale = _cachedAreaIndicatorScale;
			}
		}
	}

	private void UpdateAreaIndicator()
	{
		if ((bool)areaIndicatorInstance)
		{
			float maxDistance = 1000f;
			if (Physics.Raycast(GetAimRay(), out var hitInfo, maxDistance, LayerIndex.world.mask))
			{
				areaIndicatorInstance.transform.position = hitInfo.point;
				areaIndicatorInstance.transform.up = hitInfo.normal;
			}
		}
	}

	public override void Update()
	{
		base.Update();
		UpdateAreaIndicator();
	}

	protected override void HandlePrimaryAttack()
	{
		base.HandlePrimaryAttack();
		shouldFireArrowRain = true;
		outer.SetNextStateToMain();
	}

	protected void DoFireArrowRain()
	{
		EffectManager.SimpleMuzzleFlash(muzzleFlashEffect, base.gameObject, "Muzzle", transmit: false);
		if ((bool)areaIndicatorInstance && shouldFireArrowRain)
		{
			ProjectileManager.instance.FireProjectile(projectilePrefab, areaIndicatorInstance.transform.position, areaIndicatorInstance.transform.rotation, base.gameObject, damageStat * damageCoefficient, 0f, Util.CheckRoll(critStat, base.characterBody.master));
		}
	}

	public override void OnExit()
	{
		if (shouldFireArrowRain && !outer.destroying)
		{
			DoFireArrowRain();
		}
		if ((bool)areaIndicatorInstance)
		{
			if (_emh_areaIndicatorInstance != null && _emh_areaIndicatorInstance.OwningPool != null)
			{
				_emh_areaIndicatorInstance.OwningPool.ReturnObject(_emh_areaIndicatorInstance);
			}
			else
			{
				EntityState.Destroy(areaIndicatorInstance.gameObject);
			}
			areaIndicatorInstance = null;
			_emh_areaIndicatorInstance = null;
		}
		base.OnExit();
	}
}
