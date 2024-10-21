using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.CaptainDefenseMatrixItem;

public class DefenseMatrixOn : BaseBodyAttachmentState
{
	public static float projectileEraserRadius;

	public static float minimumFireFrequency;

	public static float baseRechargeFrequency;

	public static GameObject tracerEffectPrefab;

	private float rechargeTimer;

	private float rechargeFrequency => baseRechargeFrequency * (base.attachedBody ? base.attachedBody.attackSpeed : 1f);

	private float fireFrequency => Mathf.Max(minimumFireFrequency, rechargeFrequency);

	private float timeBetweenFiring => 1f / fireFrequency;

	private bool isReadyToFire => rechargeTimer <= 0f;

	protected int GetItemStack()
	{
		if (!base.attachedBody || !base.attachedBody.inventory)
		{
			return 1;
		}
		return base.attachedBody.inventory.GetItemCount(RoR2Content.Items.CaptainDefenseMatrix);
	}

	public override void OnEnter()
	{
		base.OnEnter();
		if (!base.attachedBody)
		{
			return;
		}
		PlayerCharacterMasterController component = base.attachedBody.master.GetComponent<PlayerCharacterMasterController>();
		if ((bool)component)
		{
			NetworkUser networkUser = component.networkUser;
			if ((bool)networkUser)
			{
				PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.CaptainDefenseMatrix.itemIndex);
				networkUser.localUser?.userProfile.DiscoverPickup(pickupIndex);
			}
		}
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!NetworkServer.active)
		{
			return;
		}
		rechargeTimer -= GetDeltaTime();
		if (base.fixedAge > timeBetweenFiring)
		{
			base.fixedAge -= timeBetweenFiring;
			if (isReadyToFire && DeleteNearbyProjectile())
			{
				rechargeTimer = 1f / rechargeFrequency;
			}
		}
	}

	private bool DeleteNearbyProjectile()
	{
		Vector3 vector = (base.attachedBody ? base.attachedBody.corePosition : Vector3.zero);
		TeamIndex teamIndex = (base.attachedBody ? base.attachedBody.teamComponent.teamIndex : TeamIndex.None);
		float num = projectileEraserRadius * projectileEraserRadius;
		int num2 = 0;
		int itemStack = GetItemStack();
		bool result = false;
		List<ProjectileController> instancesList = InstanceTracker.GetInstancesList<ProjectileController>();
		List<ProjectileController> list = new List<ProjectileController>();
		int i = 0;
		for (int count = instancesList.Count; i < count; i++)
		{
			if (num2 >= itemStack)
			{
				break;
			}
			ProjectileController projectileController = instancesList[i];
			if (!projectileController.cannotBeDeleted && projectileController.teamFilter.teamIndex != teamIndex && (projectileController.transform.position - vector).sqrMagnitude < num)
			{
				list.Add(projectileController);
				num2++;
			}
		}
		int j = 0;
		for (int count2 = list.Count; j < count2; j++)
		{
			ProjectileController projectileController2 = list[j];
			if ((bool)projectileController2)
			{
				result = true;
				Vector3 position = projectileController2.transform.position;
				Vector3 start = vector;
				if ((bool)tracerEffectPrefab)
				{
					EffectData effectData = new EffectData
					{
						origin = position,
						start = start
					};
					EffectManager.SpawnEffect(tracerEffectPrefab, effectData, transmit: true);
				}
				EntityState.Destroy(projectileController2.gameObject);
			}
		}
		return result;
	}
}
