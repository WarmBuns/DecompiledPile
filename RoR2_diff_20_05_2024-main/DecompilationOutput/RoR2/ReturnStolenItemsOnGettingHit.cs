using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ReturnStolenItemsOnGettingHit : MonoBehaviour, IOnTakeDamageServerReceiver, IOnKilledServerReceiver
{
	public HealthComponent healthComponent;

	[Range(0.01f, 100f)]
	[SerializeField]
	private float minPercentagePerItem;

	[Range(0.01f, 100f)]
	[SerializeField]
	private float maxPercentagePerItem;

	[Range(0f, 100f)]
	[SerializeField]
	private float initialPercentageToFirstItem;

	private List<Inventory> returnOrder;

	private int nextReturnIndex;

	private float damagePerItem;

	private float accumulatedDamage;

	private ItemStealController _itemStealController;

	private bool damageTrackingInitialized;

	public ItemStealController itemStealController
	{
		get
		{
			return _itemStealController;
		}
		set
		{
			if ((object)_itemStealController != null)
			{
				_itemStealController.onLendingFinishServer.RemoveListener(InitializeDamageTracking);
			}
			if ((bool)value)
			{
				value.onLendingFinishServer.AddListener(InitializeDamageTracking);
				_itemStealController = value;
			}
			else
			{
				_itemStealController = null;
			}
		}
	}

	public void OnTakeDamageServer(DamageReport damageReport)
	{
		if ((bool)itemStealController && itemStealController.hasStolen && !damageReport.damageInfo.rejected)
		{
			accumulatedDamage += damageReport.damageDealt;
		}
	}

	private void Awake()
	{
		maxPercentagePerItem = Mathf.Max(minPercentagePerItem, maxPercentagePerItem);
	}

	private void Update()
	{
		if (!NetworkServer.active || !damageTrackingInitialized)
		{
			return;
		}
		if (damagePerItem <= 0f)
		{
			damageTrackingInitialized = false;
			Debug.LogError("ReturnStolenItemsOnGettingHit.damagePerItem is 0!");
			return;
		}
		while (accumulatedDamage > damagePerItem)
		{
			accumulatedDamage -= damagePerItem;
			bool flag = itemStealController.ReclaimItemForInventory(returnOrder[nextReturnIndex]);
			nextReturnIndex = (nextReturnIndex + 1) % returnOrder.Count;
			int num = 0;
			while (!flag && num < returnOrder.Count - 1)
			{
				flag = itemStealController.ReclaimItemForInventory(returnOrder[nextReturnIndex]);
				num++;
				nextReturnIndex = (nextReturnIndex + 1) % returnOrder.Count;
			}
			if (!flag)
			{
				break;
			}
		}
	}

	private void OnDestroy()
	{
		if ((object)_itemStealController != null)
		{
			_itemStealController.onLendingFinishServer.RemoveListener(InitializeDamageTracking);
			_itemStealController = null;
		}
	}

	public void OnKilledServer(DamageReport damageReport)
	{
		_ = (bool)itemStealController;
	}

	private void InitializeDamageTracking()
	{
		returnOrder = new List<Inventory>();
		if ((bool)itemStealController)
		{
			int num = 0;
			List<Inventory> list = new List<Inventory>();
			itemStealController.AddValidStolenInventoriesToList(list);
			foreach (Inventory item in list)
			{
				if (!item.GetComponent<CharacterMaster>().minionOwnership.ownerMaster)
				{
					returnOrder.Add(item);
					num += itemStealController.GetStolenItemCount(item);
				}
			}
			float num2 = Mathf.Clamp(100f / (float)Math.Max(num, 1), minPercentagePerItem, maxPercentagePerItem) / 100f;
			damagePerItem = healthComponent.fullCombinedHealth * num2;
			accumulatedDamage += damagePerItem * initialPercentageToFirstItem / 100f;
			_itemStealController.onLendingFinishServer.RemoveListener(InitializeDamageTracking);
		}
		damageTrackingInitialized = true;
	}
}
