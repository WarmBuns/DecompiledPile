using System;
using System.Collections.Generic;
using RoR2.Projectile;
using UnityEngine;

namespace RoR2;

public class LunarSunBehavior : CharacterBody.ItemBehavior
{
	private const float secondsPerTransform = 60f;

	private const float secondsPerProjectile = 3f;

	private const string projectilePath = "Prefabs/Projectiles/LunarSunProjectile";

	private const int baseMaxProjectiles = 2;

	private const int maxProjectilesPerStack = 1;

	private const float baseOrbitDegreesPerSecond = 180f;

	private const float orbitDegreesPerSecondFalloff = 0.9f;

	private const float baseOrbitRadius = 2f;

	private const float orbitRadiusPerStack = 0.25f;

	private const float maxInclinationDegrees = 0f;

	private const float baseDamageCoefficient = 3.6f;

	private float projectileTimer;

	private float transformTimer;

	private GameObject projectilePrefab;

	private Xoroshiro128Plus transformRng;

	public event Action<LunarSunBehavior> onDisabled;

	public static int GetMaxProjectiles(Inventory inventory)
	{
		return 2 + inventory.GetItemCount(DLC1Content.Items.LunarSun);
	}

	public void InitializeOrbiter(ProjectileOwnerOrbiter orbiter, LunarSunProjectileController controller)
	{
		float num = body.radius + 2f + UnityEngine.Random.Range(0.25f, 0.25f * (float)stack);
		float num2 = num / 2f;
		num2 *= num2;
		float degreesPerSecond = 180f * Mathf.Pow(0.9f, num2);
		Quaternion quaternion = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up);
		Quaternion quaternion2 = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 0f), Vector3.forward);
		Vector3 planeNormal = quaternion * quaternion2 * Vector3.up;
		float initialDegreesFromOwnerForward = UnityEngine.Random.Range(0f, 360f);
		orbiter.Initialize(planeNormal, num, degreesPerSecond, initialDegreesFromOwnerForward);
		onDisabled += DestroyOrbiter;
		void DestroyOrbiter(LunarSunBehavior lunarSunBehavior)
		{
			if ((bool)controller)
			{
				controller.Detonate();
			}
		}
	}

	private void Awake()
	{
		base.enabled = false;
		projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/LunarSunProjectile");
		ulong seed = Run.instance.seed ^ (ulong)Run.instance.stageClearCount;
		transformRng = new Xoroshiro128Plus(seed);
	}

	private void OnEnable()
	{
	}

	private void OnDisable()
	{
		this.onDisabled?.Invoke(this);
		this.onDisabled = null;
	}

	private void FixedUpdate()
	{
		projectileTimer += Time.fixedDeltaTime;
		if (!body.master.IsDeployableLimited(DeployableSlot.LunarSunBomb) && projectileTimer > 3f / (float)stack)
		{
			projectileTimer = 0f;
			FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
			fireProjectileInfo.projectilePrefab = projectilePrefab;
			fireProjectileInfo.crit = body.RollCrit();
			fireProjectileInfo.damage = body.damage * 3.6f;
			fireProjectileInfo.damageColorIndex = DamageColorIndex.Item;
			fireProjectileInfo.force = 0f;
			fireProjectileInfo.owner = base.gameObject;
			fireProjectileInfo.position = body.transform.position;
			fireProjectileInfo.rotation = Quaternion.identity;
			FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
			ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		}
		transformTimer += Time.fixedDeltaTime;
		if (!(transformTimer > 60f))
		{
			return;
		}
		transformTimer = 0f;
		if (!body.master || !body.inventory)
		{
			return;
		}
		List<ItemIndex> list = new List<ItemIndex>(body.inventory.itemAcquisitionOrder);
		ItemIndex itemIndex = ItemIndex.None;
		Util.ShuffleList(list, transformRng);
		foreach (ItemIndex item in list)
		{
			if (item != DLC1Content.Items.LunarSun.itemIndex)
			{
				ItemDef itemDef = ItemCatalog.GetItemDef(item);
				if ((bool)itemDef && itemDef.tier != ItemTier.NoTier)
				{
					itemIndex = item;
					break;
				}
			}
		}
		if (itemIndex != ItemIndex.None)
		{
			body.inventory.RemoveItem(itemIndex);
			body.inventory.GiveItem(DLC1Content.Items.LunarSun);
			CharacterMasterNotificationQueue.SendTransformNotification(body.master, itemIndex, DLC1Content.Items.LunarSun.itemIndex, CharacterMasterNotificationQueue.TransformationType.LunarSun);
		}
	}
}
