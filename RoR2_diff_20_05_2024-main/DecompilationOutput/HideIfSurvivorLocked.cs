using System.Linq;
using RoR2;
using RoR2.EntitlementManagement;
using UnityEngine;

public class HideIfSurvivorLocked : MonoBehaviour
{
	[Tooltip("Survivor Def(s) that must be unlocked in order to show this component's gameObject. (ANY)")]
	public SurvivorDef[] requiredSurvivors;

	[Tooltip("Indicator to show that none of the designated survivors are unlocked.")]
	public GameObject lockedIndicator;

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
		if (requiredSurvivors.Length != 0)
		{
			if (requiredSurvivors.Any((SurvivorDef survivorDef) => SurvivorCatalog.SurvivorIsUnlockedOnThisClient(survivorDef.survivorIndex) && survivorDef.CheckLocalUserHasRequiredEntitlement()))
			{
				base.gameObject.SetActive(value: true);
				SetLockIndicator(value: false);
			}
			else
			{
				base.gameObject.SetActive(value: false);
				SetLockIndicator(value: true);
			}
		}
		else
		{
			Debug.LogWarning("No designated requiredSurvivors or lockedIndicator on HideIfSurvivorLocked. Consider updating or removing this component on gameObject:" + base.gameObject.name + ".");
		}
	}

	private void SetLockIndicator(bool value)
	{
		if (lockedIndicator != null)
		{
			lockedIndicator.SetActive(value);
		}
	}
}
