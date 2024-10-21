using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(EntityLocator))]
public class UnlockPickup : MonoBehaviour
{
	public static string itemPickupSoundString = "Play_UI_item_pickup";

	private bool consumed;

	private float stopWatch;

	public float waitDuration = 0.5f;

	public string displayNameToken;

	[Obsolete("'unlockableName' will be discontinued. Use 'unlockableDef' instead.", false)]
	[Tooltip("'unlockableName' will be discontinued. Use 'unlockableDef' instead.")]
	public string unlockableName;

	public UnlockableDef unlockableDef;

	private void FixedUpdate()
	{
		stopWatch += Time.fixedDeltaTime;
	}

	private void GrantPickup(GameObject activator)
	{
		if ((bool)Run.instance)
		{
			Util.PlaySound(itemPickupSoundString, activator);
			string value = unlockableName;
			if (!unlockableDef && !string.IsNullOrEmpty(value))
			{
				unlockableDef = UnlockableCatalog.GetUnlockableDef(value);
			}
			Run.instance.GrantUnlockToAllParticipatingPlayers(unlockableDef);
			string pickupToken = "???";
			if ((bool)unlockableDef)
			{
				pickupToken = unlockableDef.nameToken;
			}
			Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
			{
				subjectAsCharacterBody = activator.GetComponent<CharacterBody>(),
				baseToken = "PLAYER_PICKUP",
				pickupToken = pickupToken,
				pickupColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unlockable),
				pickupQuantity = 1u
			});
			consumed = true;
			GameObject entity = base.gameObject;
			EntityLocator component = GetComponent<EntityLocator>();
			if ((bool)component.entity)
			{
				entity = component.entity;
			}
			UnityEngine.Object.Destroy(entity);
		}
	}

	private static bool BodyHasPickupPermission(CharacterBody body)
	{
		if ((bool)(body.masterObject ? body.masterObject.GetComponent<PlayerCharacterMasterController>() : null))
		{
			return body.inventory;
		}
		return false;
	}

	private void OnTriggerStay(Collider other)
	{
		if (!NetworkServer.active || !(stopWatch >= waitDuration) || consumed)
		{
			return;
		}
		CharacterBody component = other.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			TeamComponent component2 = component.GetComponent<TeamComponent>();
			if ((bool)component2 && component2.teamIndex == TeamIndex.Player && (bool)component.inventory)
			{
				GrantPickup(component.gameObject);
			}
		}
	}
}
