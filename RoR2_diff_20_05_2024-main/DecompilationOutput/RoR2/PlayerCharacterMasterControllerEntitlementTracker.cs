using System;
using HG;
using JetBrains.Annotations;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(PlayerCharacterMasterController))]
public class PlayerCharacterMasterControllerEntitlementTracker : NetworkBehaviour
{
	private PlayerCharacterMasterController playerCharacterMasterController;

	private static readonly uint entitlementsSetDirtyBit = 1u;

	private static readonly uint allDirtyBits = entitlementsSetDirtyBit;

	private bool[] entitlementsSet;

	private void Awake()
	{
		playerCharacterMasterController = GetComponent<PlayerCharacterMasterController>();
		entitlementsSet = new bool[EntitlementCatalog.entitlementDefs.Length];
		playerCharacterMasterController.onLinkedToNetworkUserServer += OnLinkedToNetworkUserServer;
	}

	private void OnLinkedToNetworkUserServer()
	{
		UpdateEntitlementsServer();
		GameObject bodyPrefab = playerCharacterMasterController.master.bodyPrefab;
		if ((bool)bodyPrefab)
		{
			ExpansionRequirementComponent component = bodyPrefab.GetComponent<ExpansionRequirementComponent>();
			if ((bool)component && !component.PlayerCanUseBody(playerCharacterMasterController))
			{
				Debug.LogWarning("Player can't use body " + bodyPrefab.name + "; defaulting to default survivor.");
				playerCharacterMasterController.master.bodyPrefab = SurvivorCatalog.defaultSurvivor.bodyPrefab;
			}
		}
	}

	[Server]
	private void UpdateEntitlementsServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.PlayerCharacterMasterControllerEntitlementTracker::UpdateEntitlementsServer()' called on client");
			return;
		}
		NetworkUser networkUser = playerCharacterMasterController.networkUser;
		if (!networkUser)
		{
			return;
		}
		bool flag = false;
		NetworkUserServerEntitlementTracker networkUserEntitlementTracker = EntitlementManager.networkUserEntitlementTracker;
		ReadOnlyArray<EntitlementDef> entitlementDefs = EntitlementCatalog.entitlementDefs;
		for (int i = 0; i < entitlementDefs.Length; i++)
		{
			EntitlementDef entitlementDef = entitlementDefs[i];
			if (!entitlementsSet[i] && networkUserEntitlementTracker.UserHasEntitlement(networkUser, entitlementDef))
			{
				entitlementsSet[i] = true;
				flag = true;
			}
		}
		if (flag)
		{
			SetDirtyBit(entitlementsSetDirtyBit);
		}
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = (initialState ? allDirtyBits : base.syncVarDirtyBits);
		writer.WritePackedUInt32(num);
		if ((num & entitlementsSetDirtyBit) != 0)
		{
			writer.WriteBitArray(entitlementsSet);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if ((reader.ReadPackedUInt32() & entitlementsSetDirtyBit) != 0)
		{
			reader.ReadBitArray(entitlementsSet);
		}
	}

	public bool HasEntitlement([NotNull] EntitlementDef entitlementDef)
	{
		if ((object)entitlementDef == null)
		{
			throw new ArgumentNullException("entitlementDef");
		}
		return ArrayUtils.GetSafe(entitlementsSet, (int)entitlementDef.entitlementIndex, false);
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
