using System;
using JetBrains.Annotations;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2;

[CreateAssetMenu(menuName = "RoR2/UnlockableDef")]
public class UnlockableDef : ScriptableObject
{
	private string _cachedName;

	public string nameToken = "???";

	[Obsolete("UnlockableDef.displayModelPath is obsolete. Use .displayModel instead.", false)]
	public string displayModelPath = "Prefabs/NullModel";

	public GameObject displayModelPrefab;

	public bool hidden;

	public Sprite achievementIcon;

	[Tooltip("Set this to override Unlockable Catalog automatic Expansion linking")]
	public ExpansionDef requiredExpansion;

	public UnlockableIndex index { get; set; } = UnlockableIndex.None;

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

	[NotNull]
	public Func<string> getHowToUnlockString { get; set; } = () => "???";

	[NotNull]
	public Func<string> getUnlockedString { get; set; } = () => "???";

	public int sortScore { get; set; }

	private void Awake()
	{
		_cachedName = base.name;
	}

	private void OnValidate()
	{
		_cachedName = base.name;
	}

	[ContextMenu("Update displayModelPath to displayModelPrefab")]
	private void ReplaceDisplayModelPrefabPathWithDirectReference()
	{
		string text = displayModelPath;
		if (!string.IsNullOrEmpty(text))
		{
			GameObject gameObject = LegacyResourcesAPI.Load<GameObject>(text);
			if ((bool)gameObject)
			{
				displayModelPrefab = gameObject;
				displayModelPath = string.Empty;
				EditorUtil.SetDirty(this);
			}
		}
	}

	[ConCommand(commandName = "unlockable_migrate", flags = ConVarFlags.None, helpText = "Generates UnlockableDef assets from the existing catalog entries.")]
	private static void CCUnlockableMigrate(ConCommandArgs args)
	{
		for (UnlockableIndex unlockableIndex = (UnlockableIndex)0; (int)unlockableIndex < UnlockableCatalog.unlockableCount; unlockableIndex++)
		{
			EditorUtil.CopyToScriptableObject<UnlockableDef, UnlockableDef>(UnlockableCatalog.GetUnlockableDef(unlockableIndex), "Assets/RoR2/Resources/UnlockableDefs/");
		}
	}
}
