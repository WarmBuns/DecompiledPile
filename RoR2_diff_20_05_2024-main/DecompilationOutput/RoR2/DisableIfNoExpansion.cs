using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

public class DisableIfNoExpansion : MonoBehaviour
{
	[SerializeField]
	private ExpansionDef expansionDef;

	private void Awake()
	{
		EntitlementManager.onEntitlementsUpdated += Reset;
		Reset();
	}

	private void OnDestroy()
	{
		EntitlementManager.onEntitlementsUpdated -= Reset;
	}

	private void Reset()
	{
		if ((bool)expansionDef && !EntitlementManager.localUserEntitlementTracker.AnyUserHasEntitlement(expansionDef.requiredEntitlement))
		{
			base.gameObject.SetActive(value: false);
		}
		else
		{
			base.gameObject.SetActive(value: true);
		}
	}
}
