using System;
using System.Collections.Generic;
using System.Linq;
using Epic.OnlineServices;
using Epic.OnlineServices.Ecom;
using JetBrains.Annotations;
using RoR2.ContentManagement;
using RoR2.Networking;

namespace RoR2.EntitlementManagement;

public class EgsEntitlementResolver : IUserEntitementsResolverNetworkAndLocal, IUserEntitlementResolver<NetworkUser>, IUserEntitlementResolver<LocalUser>, IPlatformShopLauncher
{
	private const string DevAudienceEntitlementName = "8fc64849a03741faaf51824d6e727cc1";

	private static EcomInterface EOS_Ecom;

	private List<string> ownedEntitlementIDs = new List<string>();

	private static event Action onDlcInstalled;

	public event Action OnPlatformShopClosed;

	event Action IUserEntitlementResolver<LocalUser>.onEntitlementsChanged
	{
		add
		{
			onDlcInstalled += value;
		}
		remove
		{
			onDlcInstalled -= value;
		}
	}

	event Action IUserEntitlementResolver<NetworkUser>.onEntitlementsChanged
	{
		add
		{
			onDlcInstalled += value;
		}
		remove
		{
			onDlcInstalled -= value;
		}
	}

	public EgsEntitlementResolver()
	{
		if (EOS_Ecom == null)
		{
			EOS_Ecom = EOSPlatformManager.GetPlatformInterface().GetEcomInterface();
		}
		RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(GetEGSEntitlements));
	}

	private void GetEGSEntitlements(EpicAccountId accountId)
	{
		GetEGSEntitlements();
	}

	private void GetEGSEntitlements()
	{
		if (EOSLoginManager.loggedInAuthId != null)
		{
			QueryOwnershipOptions options = default(QueryOwnershipOptions);
			options.LocalUserId = EOSLoginManager.loggedInAuthId;
			options.CatalogItemIds = EntitlementCatalog.entitlementDefs.Select((EntitlementDef x) => new Utf8String(x.eosItemId)).ToArray();
			EOS_Ecom.QueryOwnership(ref options, null, HandleQueryOwnershipCallback);
		}
		else
		{
			EOSLoginManager.OnAuthLoggedIn += GetEGSEntitlements;
		}
	}

	private void HandleQueryOwnershipCallback(ref QueryOwnershipCallbackInfo data)
	{
		if (data.GetResultCode() == Result.Success)
		{
			ownedEntitlementIDs.Clear();
			for (int i = 0; i < data.ItemOwnership.Length; i++)
			{
				if (data.ItemOwnership[i].OwnershipStatus == OwnershipStatus.Owned)
				{
					ownedEntitlementIDs.Add(data.ItemOwnership[i].Id);
				}
			}
		}
		EgsEntitlementResolver.onDlcInstalled();
	}

	bool IUserEntitlementResolver<LocalUser>.CheckUserHasEntitlement([NotNull] LocalUser localUser, [NotNull] EntitlementDef entitlementDef)
	{
		return CheckLocalUserHasEntitlement(entitlementDef);
	}

	private bool CheckLocalUserHasEntitlement(EntitlementDef entitlementDef)
	{
		for (int i = 0; i < ownedEntitlementIDs.Count; i++)
		{
			if (ownedEntitlementIDs[i] == entitlementDef.eosItemId)
			{
				return true;
			}
		}
		return false;
	}

	bool IUserEntitlementResolver<NetworkUser>.CheckUserHasEntitlement([NotNull] NetworkUser networkUser, [NotNull] EntitlementDef entitlementDef)
	{
		if (!networkUser)
		{
			return false;
		}
		if (networkUser.isLocalPlayer)
		{
			return CheckLocalUserHasEntitlement(entitlementDef);
		}
		ClientAuthData clientAuthData = ServerAuthManager.FindAuthData(networkUser.connectionToClient);
		if (clientAuthData == null)
		{
			return false;
		}
		PlatformID platformId = clientAuthData.platformId;
		if (!platformId.isValid)
		{
			return false;
		}
		return EntitlementAbstractions.VerifyRemoteUser(clientAuthData, entitlementDef);
	}

	public string[] BuildEntitlements()
	{
		string[] array = new string[ownedEntitlementIDs.Count];
		int num = array.Length;
		for (int i = 0; i < num; i++)
		{
			EntitlementDef[] entitlementDefs = ContentManager.entitlementDefs;
			foreach (EntitlementDef entitlementDef in entitlementDefs)
			{
				if (entitlementDef.eosItemId == ownedEntitlementIDs[i] || "8fc64849a03741faaf51824d6e727cc1" == ownedEntitlementIDs[i])
				{
					array[i] = entitlementDef.name;
					break;
				}
			}
		}
		return array;
	}

	public void LaunchPlatformShop(EntitlementDef entitlementDef)
	{
		throw new NotImplementedException();
	}

	public void LaunchPlatformShopAllProducts()
	{
		throw new NotImplementedException();
	}
}
