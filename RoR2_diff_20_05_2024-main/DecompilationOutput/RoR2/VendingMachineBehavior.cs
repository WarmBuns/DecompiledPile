using System;
using System.Collections.Generic;
using RoR2.Networking;
using RoR2.Orbs;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PurchaseInteraction))]
public class VendingMachineBehavior : NetworkBehaviour
{
	[SerializeField]
	private int maxPurchases;

	[SerializeField]
	private float healFraction;

	[SerializeField]
	private GameObject detonateEffect;

	[SerializeField]
	private float vendingRadius;

	[SerializeField]
	private int numBonusOrbs;

	[SerializeField]
	private Transform orbOrigin;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private string animatorTriggerNameVend;

	[SerializeField]
	private string animatorIntNameVendsRemaining;

	private PurchaseInteraction purchaseInteraction;

	private static int kRpcRpcTriggerVendAnimation;

	public int purchaseCount { get; private set; }

	public static event Action<ShrineHealingBehavior, Interactor> onActivated;

	public override int GetNetworkChannel()
	{
		return QosChannelIndex.defaultReliable.intVal;
	}

	private void Awake()
	{
		purchaseInteraction = GetComponent<PurchaseInteraction>();
	}

	[Server]
	public void Vend(Interactor activator)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VendingMachineBehavior::Vend(RoR2.Interactor)' called on client");
			return;
		}
		int num = purchaseCount + 1;
		purchaseCount = num;
		CallRpcTriggerVendAnimation(maxPurchases - purchaseCount);
		CharacterBody component = activator.GetComponent<CharacterBody>();
		if ((bool)component)
		{
			SendOrbToBody(component);
			BullseyeSearch bullseyeSearch = new BullseyeSearch();
			bullseyeSearch.teamMaskFilter = TeamMask.none;
			if ((bool)component.teamComponent)
			{
				bullseyeSearch.teamMaskFilter.AddTeam(component.teamComponent.teamIndex);
			}
			bullseyeSearch.filterByLoS = false;
			bullseyeSearch.maxDistanceFilter = vendingRadius;
			bullseyeSearch.maxAngleFilter = 360f;
			bullseyeSearch.searchOrigin = base.transform.position;
			bullseyeSearch.searchDirection = Vector3.up;
			bullseyeSearch.sortMode = BullseyeSearch.SortMode.None;
			bullseyeSearch.RefreshCandidates();
			bullseyeSearch.FilterOutGameObject(activator.gameObject);
			List<HurtBox> list = new List<HurtBox>(bullseyeSearch.GetResults());
			list.Sort(BonusOrbHurtBoxSort);
			for (int i = 0; i < numBonusOrbs && i < list.Count; i++)
			{
				SendOrbToBody(list[i].healthComponent.body);
			}
		}
	}

	private static int BonusOrbHurtBoxSort(HurtBox lhs, HurtBox rhs)
	{
		return (int)(100f * (lhs.healthComponent.combinedHealthFraction - rhs.healthComponent.combinedHealthFraction));
	}

	private void SendOrbToBody(CharacterBody body)
	{
		HealthComponent component = body.GetComponent<HealthComponent>();
		if ((bool)component)
		{
			VendingMachineOrb vendingMachineOrb = new VendingMachineOrb();
			vendingMachineOrb.origin = orbOrigin.position;
			vendingMachineOrb.target = component.body.mainHurtBox;
			vendingMachineOrb.healFraction = healFraction;
			OrbManager.instance.AddOrb(vendingMachineOrb);
		}
	}

	[Server]
	public void Detonate()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VendingMachineBehavior::Detonate()' called on client");
			return;
		}
		EffectData effectData = new EffectData();
		effectData.origin = base.transform.position;
		EffectManager.SpawnEffect(detonateEffect, effectData, transmit: true);
		UnityEngine.Object.Destroy(base.gameObject);
	}

	[Server]
	public void RefreshPurchaseInteractionAvailability()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VendingMachineBehavior::RefreshPurchaseInteractionAvailability()' called on client");
		}
		else
		{
			purchaseInteraction.Networkavailable = purchaseCount < maxPurchases;
		}
	}

	[ClientRpc]
	public void RpcTriggerVendAnimation(int vendsRemaining)
	{
		if ((bool)animator)
		{
			animator.SetTrigger(animatorTriggerNameVend);
			animator.SetInteger(animatorIntNameVendsRemaining, vendsRemaining);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcTriggerVendAnimation(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcTriggerVendAnimation called on server.");
		}
		else
		{
			((VendingMachineBehavior)obj).RpcTriggerVendAnimation((int)reader.ReadPackedUInt32());
		}
	}

	public void CallRpcTriggerVendAnimation(int vendsRemaining)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcTriggerVendAnimation called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcTriggerVendAnimation);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)vendsRemaining);
		SendRPCInternal(networkWriter, 0, "RpcTriggerVendAnimation");
	}

	static VendingMachineBehavior()
	{
		kRpcRpcTriggerVendAnimation = 2105333433;
		NetworkBehaviour.RegisterRpcDelegate(typeof(VendingMachineBehavior), kRpcRpcTriggerVendAnimation, InvokeRpcRpcTriggerVendAnimation);
		NetworkCRC.RegisterBehaviour("VendingMachineBehavior", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
