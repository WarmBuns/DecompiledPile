using System;
using RoR2.EntitlementManagement;
using UnityEngine;

namespace RoR2;

public class PlatformManager
{
	public static Guid DefaultControllerGuid => PlatformSystems.platformManager.GetDefaultControllerGuid();

	public static string DefaultControllerName => PlatformSystems.platformManager.GetDefaultControllerName();

	public static event Action initializeFinished;

	public PlatformManager()
	{
		RoR2Application.onLoad = (Action)Delegate.Combine(RoR2Application.onLoad, new Action(InitializePlatformManager));
	}

	public virtual void InitializePlatformManager()
	{
		RoR2Application.onUpdate += UpdatePlatformManager;
	}

	protected virtual void UpdatePlatformManager()
	{
	}

	internal static void OnInitialized()
	{
		PlatformManager.initializeFinished?.Invoke();
	}

	public virtual Vector3 GetGyroAimDeltaVector()
	{
		return Vector3.zero;
	}

	public virtual bool IsNetworkAvailable()
	{
		return true;
	}

	[ConCommand(commandName = "PlatformStore_ShowItem", flags = ConVarFlags.None, helpText = "Shows the platform store page for a single Entitlement/DLC")]
	public static void CCPlatformStoreShowItem(ConCommandArgs args)
	{
		int argInt = args.GetArgInt(0);
		if (argInt < EntitlementCatalog.entitlementDefs.Length)
		{
			PlatformSystems.entitlementsSystem.LaunchPlatformShop(EntitlementCatalog.entitlementDefs[argInt]);
		}
	}

	[ConCommand(commandName = "PlatformStore_ShowAll", flags = ConVarFlags.None, helpText = "Shows the platform store page for all purchasable items")]
	public static void CCPlatformStoreShowAll(ConCommandArgs args)
	{
		PlatformSystems.entitlementsSystem.LaunchPlatformShopAllProducts();
	}

	internal virtual Guid GetDefaultControllerGuid()
	{
		return DefaultControllerMaps.gamepadTemplateGuid;
	}

	internal virtual string GetDefaultControllerName()
	{
		return "Default Controller";
	}
}
