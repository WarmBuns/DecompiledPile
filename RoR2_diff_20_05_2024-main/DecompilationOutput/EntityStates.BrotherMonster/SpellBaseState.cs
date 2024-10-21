using System.Collections.Generic;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class SpellBaseState : BaseState
{
	protected ItemStealController itemStealController;

	protected GameObject hammerRendererObject;

	protected virtual bool DisplayWeapon => false;

	public override void OnEnter()
	{
		base.OnEnter();
		FindItemStealer();
		if (NetworkServer.active && !itemStealController)
		{
			InitItemStealer();
		}
		hammerRendererObject = FindModelChild("HammerRenderer").gameObject;
		if ((bool)hammerRendererObject)
		{
			hammerRendererObject.SetActive(DisplayWeapon);
		}
	}

	public override void OnExit()
	{
		if ((bool)hammerRendererObject)
		{
			hammerRendererObject.SetActive(value: false);
		}
		base.OnExit();
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		FindItemStealer();
	}

	private void FindItemStealer()
	{
		if ((bool)itemStealController)
		{
			return;
		}
		List<NetworkedBodyAttachment> list = new List<NetworkedBodyAttachment>();
		NetworkedBodyAttachment.FindBodyAttachments(base.characterBody, list);
		foreach (NetworkedBodyAttachment item in list)
		{
			itemStealController = item.GetComponent<ItemStealController>();
			if ((bool)itemStealController)
			{
				break;
			}
		}
	}

	private void InitItemStealer()
	{
		if (NetworkServer.active && itemStealController == null)
		{
			GameObject gameObject = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/ItemStealController"), base.transform.position, Quaternion.identity);
			itemStealController = gameObject.GetComponent<ItemStealController>();
			itemStealController.itemLendFilter = ItemStealController.BrotherItemFilter;
			gameObject.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
			base.gameObject.GetComponent<ReturnStolenItemsOnGettingHit>().itemStealController = itemStealController;
			NetworkServer.Spawn(gameObject);
		}
	}
}
