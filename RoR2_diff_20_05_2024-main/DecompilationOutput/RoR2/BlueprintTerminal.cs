using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class BlueprintTerminal : NetworkBehaviour
{
	[Serializable]
	public struct UnlockableOption
	{
		[Obsolete("'unlockableName' will be discontinued. Use 'unlockableDef' instead.", false)]
		[Tooltip("'unlockableName' will be discontinued. Use 'unlockableDef' instead.")]
		public string unlockableName;

		public UnlockableDef unlockableDef;

		public int cost;

		public float weight;

		public UnlockableDef GetResolvedUnlockableDef()
		{
			string text = unlockableName;
			if (!unlockableDef && !string.IsNullOrEmpty(text))
			{
				unlockableDef = UnlockableCatalog.GetUnlockableDef(text);
			}
			return unlockableDef;
		}
	}

	[SyncVar(hook = "SetHasBeenPurchased")]
	private bool hasBeenPurchased;

	public Transform displayBaseTransform;

	[Tooltip("The unlockables to grant")]
	public UnlockableOption[] unlockableOptions;

	private int unlockableChoice;

	public string unlockSoundString;

	public float idealDisplayVolume = 1.5f;

	public GameObject unlockEffect;

	private GameObject displayInstance;

	public bool NetworkhasBeenPurchased
	{
		get
		{
			return hasBeenPurchased;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetHasBeenPurchased(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref hasBeenPurchased, 1u);
		}
	}

	private void SetHasBeenPurchased(bool newHasBeenPurchased)
	{
		if (hasBeenPurchased != newHasBeenPurchased)
		{
			NetworkhasBeenPurchased = newHasBeenPurchased;
			Rebuild();
		}
	}

	public void Start()
	{
		if (NetworkServer.active)
		{
			RollChoice();
		}
		if (NetworkClient.active)
		{
			Rebuild();
		}
	}

	private void RollChoice()
	{
		WeightedSelection<int> weightedSelection = new WeightedSelection<int>();
		for (int i = 0; i < unlockableOptions.Length; i++)
		{
			weightedSelection.AddChoice(i, unlockableOptions[i].weight);
		}
		unlockableChoice = weightedSelection.Evaluate(UnityEngine.Random.value);
		Rebuild();
	}

	private void Rebuild()
	{
		UnlockableOption unlockableOption = unlockableOptions[unlockableChoice];
		if ((bool)displayInstance)
		{
			UnityEngine.Object.Destroy(displayInstance);
		}
		displayBaseTransform.gameObject.SetActive(!hasBeenPurchased);
		if (!hasBeenPurchased && (bool)displayBaseTransform)
		{
			UnlockableDef resolvedUnlockableDef = unlockableOption.GetResolvedUnlockableDef();
			if ((bool)resolvedUnlockableDef)
			{
				GameObject displayModelPrefab = resolvedUnlockableDef.displayModelPrefab;
				if ((bool)displayModelPrefab)
				{
					displayInstance = UnityEngine.Object.Instantiate(displayModelPrefab, displayBaseTransform.position, displayBaseTransform.transform.rotation, displayBaseTransform);
					Renderer componentInChildren = displayInstance.GetComponentInChildren<Renderer>();
					float num = 1f;
					if ((bool)componentInChildren)
					{
						displayInstance.transform.rotation = Quaternion.identity;
						Vector3 size = componentInChildren.bounds.size;
						float f = size.x * size.y * size.z;
						num *= Mathf.Pow(idealDisplayVolume, 1f / 3f) / Mathf.Pow(f, 1f / 3f);
					}
					displayInstance.transform.localScale = new Vector3(num, num, num);
				}
			}
		}
		PurchaseInteraction component = GetComponent<PurchaseInteraction>();
		if ((bool)component)
		{
			component.Networkcost = unlockableOption.cost;
		}
	}

	[Server]
	public void GrantUnlock(Interactor interactor)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.BlueprintTerminal::GrantUnlock(RoR2.Interactor)' called on client");
			return;
		}
		SetHasBeenPurchased(newHasBeenPurchased: true);
		UnlockableOption unlockableOption = unlockableOptions[unlockableChoice];
		UnlockableDef resolvedUnlockableDef = unlockableOption.GetResolvedUnlockableDef();
		EffectManager.SpawnEffect(unlockEffect, new EffectData
		{
			origin = base.transform.position
		}, transmit: true);
		if ((bool)Run.instance)
		{
			Util.PlaySound(unlockSoundString, interactor.gameObject);
			CharacterBody component = interactor.GetComponent<CharacterBody>();
			Run.instance.GrantUnlockToSinglePlayer(resolvedUnlockableDef, component);
			string pickupToken = "???";
			if (resolvedUnlockableDef != null)
			{
				pickupToken = resolvedUnlockableDef.nameToken;
			}
			Chat.SendBroadcastChat(new Chat.PlayerPickupChatMessage
			{
				subjectAsCharacterBody = component,
				baseToken = "PLAYER_PICKUP",
				pickupToken = pickupToken,
				pickupColor = ColorCatalog.GetColor(ColorCatalog.ColorIndex.Unlockable),
				pickupQuantity = 1u
			});
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(hasBeenPurchased);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(hasBeenPurchased);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			hasBeenPurchased = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetHasBeenPurchased(reader.ReadBoolean());
		}
	}

	public override void PreStartClient()
	{
	}
}
