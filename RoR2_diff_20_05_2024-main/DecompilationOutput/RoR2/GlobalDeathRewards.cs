using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GlobalDeathRewards : MonoBehaviour
{
	[Serializable]
	public struct PickupReward
	{
		[SerializeField]
		public PickupDropTable dropTable;

		[EnumMask(typeof(CharacterBody.BodyFlags))]
		[SerializeField]
		public CharacterBody.BodyFlags requiredBodyFlags;

		[Range(0f, 1f)]
		[SerializeField]
		public float chance;

		[SerializeField]
		public Vector3 velocity;
	}

	[SerializeField]
	private bool requirePlayerAttacker;

	[SerializeField]
	private PickupReward[] pickupRewards;

	private Xoroshiro128Plus rng;

	private void Start()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.runRNG.nextUlong);
		}
	}

	private void OnEnable()
	{
		GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeathGlobal;
	}

	private void OnDisable()
	{
		GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeathGlobal;
	}

	private void OnCharacterDeathGlobal(DamageReport damageReport)
	{
		if (!NetworkServer.active)
		{
			return;
		}
		bool flag = true;
		if (requirePlayerAttacker)
		{
			flag = (bool)damageReport.attackerMaster && (bool)damageReport.attackerMaster.GetComponent<PlayerCharacterMasterController>();
		}
		if (!flag)
		{
			return;
		}
		PickupReward[] array = pickupRewards;
		for (int i = 0; i < array.Length; i++)
		{
			PickupReward pickupReward = array[i];
			if ((damageReport.victimBody.bodyFlags & pickupReward.requiredBodyFlags) == pickupReward.requiredBodyFlags && (bool)pickupReward.dropTable && rng.nextNormalizedFloat <= pickupReward.chance)
			{
				PickupDropletController.CreatePickupDroplet(pickupReward.dropTable.GenerateDrop(rng), damageReport.victim.transform.position, pickupReward.velocity);
			}
		}
	}
}
