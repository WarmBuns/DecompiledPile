using RoR2.EntitlementManagement;
using UnityEngine;

namespace RoR2.ExpansionManagement;

public class ExpansionRequirementComponent : MonoBehaviour
{
	public ExpansionDef requiredExpansion;

	public bool requireEntitlementIfPlayerControlled;

	public static bool ignoreExpansionDisablingForPlayerControlled = true;

	private void Start()
	{
		CharacterBody component = GetComponent<CharacterBody>();
		if ((bool)component && component.isPlayerControlled && !PlayerCanUseBody(component.master.playerCharacterMasterController))
		{
			Object.Destroy(base.gameObject);
		}
	}

	public bool IgnoreExpansionDisabling()
	{
		if (requireEntitlementIfPlayerControlled)
		{
			return ignoreExpansionDisablingForPlayerControlled;
		}
		return false;
	}

	public bool PlayerCanUseBody(PlayerCharacterMasterController playerCharacterMasterController)
	{
		Run instance = Run.instance;
		if (!instance)
		{
			return false;
		}
		if ((bool)requiredExpansion)
		{
			if (!instance.IsExpansionEnabled(requiredExpansion) && !IgnoreExpansionDisabling())
			{
				return false;
			}
			if (requireEntitlementIfPlayerControlled)
			{
				EntitlementDef requiredEntitlement = requiredExpansion.requiredEntitlement;
				if ((bool)requiredEntitlement)
				{
					PlayerCharacterMasterControllerEntitlementTracker component = playerCharacterMasterController.GetComponent<PlayerCharacterMasterControllerEntitlementTracker>();
					if (!component)
					{
						Debug.LogWarning("Rejecting body because the playerCharacterMasterController doesn't have a sibling PlayerCharacterMasterControllerEntitlementTracker");
						return false;
					}
					if (!component.HasEntitlement(requiredEntitlement))
					{
						Debug.LogWarning("Rejecting body because the player doesn't have entitlement " + requiredEntitlement.name);
						return false;
					}
				}
			}
		}
		return true;
	}
}
