using System;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/SurvivorDef")]
public class SurvivorDef : ScriptableObject
{
	private string _cachedName;

	public GameObject bodyPrefab;

	public GameObject displayPrefab;

	[Obsolete("Use 'unlockableDef' instead.")]
	public string unlockableName = "";

	public UnlockableDef unlockableDef;

	public string displayNameToken;

	public string descriptionToken;

	public string outroFlavorToken;

	public string mainEndingEscapeFailureFlavorToken;

	public Color primaryColor;

	public float desiredSortPosition;

	public bool hidden;

	public SurvivorIndex survivorIndex { get; set; } = SurvivorIndex.None;

	[Obsolete(".name should not be used. Use .cachedName instead. If retrieving the value from the engine is absolutely necessary, cast to ScriptableObject first.", true)]
	public new string name
	{
		get
		{
			throw new NotImplementedException();
		}
		set
		{
			throw new NotImplementedException();
		}
	}

	public string cachedName
	{
		get
		{
			return _cachedName;
		}
		set
		{
			base.name = value;
			_cachedName = value;
		}
	}

	private void Awake()
	{
		_cachedName = base.name;
	}

	private void OnValidate()
	{
		_cachedName = base.name;
	}

	[ContextMenu("Auto Populate Tokens")]
	public void AutoPopulateTokens()
	{
		string arg = base.name.ToUpperInvariant();
		displayNameToken = displayPrefab.GetComponent<CharacterBody>().baseNameToken;
		descriptionToken = $"{arg}_DESCRIPTION";
		outroFlavorToken = $"{arg}_OUTRO_FLAVOR";
	}

	[ContextMenu("Upgrade unlockableName to unlockableDef")]
	public void UpgradeUnlockableNameToUnlockableDef()
	{
		if (!string.IsNullOrEmpty(unlockableName) && !this.unlockableDef)
		{
			UnlockableDef unlockableDef = LegacyResourcesAPI.Load<UnlockableDef>("UnlockableDefs/" + unlockableName);
			if ((bool)unlockableDef)
			{
				this.unlockableDef = unlockableDef;
				unlockableName = null;
			}
		}
		EditorUtil.SetDirty(this);
	}

	public ExpansionDef GetRequiredExpansion()
	{
		ExpansionRequirementComponent component = bodyPrefab.GetComponent<ExpansionRequirementComponent>();
		if (!component)
		{
			return null;
		}
		return component.requiredExpansion;
	}

	public EntitlementDef GetRequiredEntitlement()
	{
		ExpansionDef requiredExpansion = GetRequiredExpansion();
		if (!requiredExpansion)
		{
			return null;
		}
		return requiredExpansion.requiredEntitlement;
	}

	public bool CheckUserHasRequiredEntitlement(NetworkUser networkUser)
	{
		EntitlementDef requiredEntitlement = GetRequiredEntitlement();
		if (!requiredEntitlement)
		{
			return true;
		}
		return EntitlementManager.networkUserEntitlementTracker.UserHasEntitlement(networkUser, requiredEntitlement);
	}

	public bool CheckUserHasRequiredEntitlement(LocalUser localUser)
	{
		EntitlementDef requiredEntitlement = GetRequiredEntitlement();
		if (!requiredEntitlement)
		{
			return true;
		}
		return EntitlementManager.localUserEntitlementTracker.UserHasEntitlement(localUser, requiredEntitlement);
	}

	public bool CheckLocalUserHasRequiredEntitlement()
	{
		foreach (LocalUser readOnlyLocalUsers in LocalUserManager.readOnlyLocalUsersList)
		{
			if (CheckUserHasRequiredEntitlement(readOnlyLocalUsers))
			{
				return true;
			}
		}
		return false;
	}

	public bool CheckRequiredExpansionEnabled(NetworkUser networkUser = null)
	{
		ExpansionDef requiredExpansion = GetRequiredExpansion();
		if (!requiredExpansion)
		{
			return true;
		}
		if (requiredExpansion.enabledChoice == null)
		{
			return false;
		}
		if ((bool)networkUser && !EntitlementManager.networkUserEntitlementTracker.UserHasEntitlement(networkUser, requiredExpansion.requiredEntitlement))
		{
			return false;
		}
		return (PreGameController.instance ? PreGameController.instance.readOnlyRuleBook : (Run.instance ? Run.instance.ruleBook : null))?.IsChoiceActive(requiredExpansion.enabledChoice) ?? true;
	}
}
