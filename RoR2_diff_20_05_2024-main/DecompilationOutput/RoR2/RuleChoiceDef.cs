using System;
using JetBrains.Annotations;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class RuleChoiceDef
{
	public RuleDef ruleDef;

	public Sprite sprite;

	[NotNull]
	public string tooltipNameToken;

	public Func<RuleChoiceDef, string> getTooltipName = GetNormalTooltipNameFromToken;

	public Color tooltipNameColor = Color.white;

	[NotNull]
	public string tooltipBodyToken;

	public Color tooltipBodyColor = Color.white;

	public string localName;

	public string globalName;

	public int localIndex;

	public int globalIndex;

	public UnlockableDef requiredUnlockable;

	public RuleChoiceDef requiredChoiceDef;

	public EntitlementDef requiredEntitlementDef;

	public ExpansionDef requiredExpansionDef;

	public bool availableInSinglePlayer = true;

	public bool availableInMultiPlayer = true;

	public DifficultyIndex difficultyIndex = DifficultyIndex.Invalid;

	public ArtifactIndex artifactIndex = ArtifactIndex.None;

	public ItemIndex itemIndex = ItemIndex.None;

	public EquipmentIndex equipmentIndex = EquipmentIndex.None;

	public object extraData;

	public bool excludeByDefault;

	public string selectionUISound;

	[CanBeNull]
	public string serverTag;

	public bool onlyShowInGameBrowserIfNonDefault;

	public string spritePath
	{
		set
		{
			AsyncOperationHandle<Sprite> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<Sprite>(value);
			asyncOperationHandle.Completed += delegate(AsyncOperationHandle<Sprite> x)
			{
				sprite = x.Result;
			};
		}
	}

	[Obsolete("Use 'requiredUnlockable' instead.", false)]
	public ref UnlockableDef unlockable => ref requiredUnlockable;

	public bool isDefaultChoice => ruleDef.defaultChoiceIndex == localIndex;

	public static string GetNormalTooltipNameFromToken(RuleChoiceDef ruleChoiceDef)
	{
		return Language.GetString(ruleChoiceDef.tooltipNameToken);
	}

	public static string GetOffTooltipNameFromToken(RuleChoiceDef ruleChoiceDef)
	{
		return Language.GetStringFormatted("RULE_OFF_FORMAT", Language.GetString(ruleChoiceDef.tooltipNameToken));
	}
}
