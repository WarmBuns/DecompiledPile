using System;
using System.Collections.Generic;
using System.Text;
using HG;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

internal class ServerManagerBase<T> : TagManager where T : ServerManagerBase<T>, IDisposable, new()
{
	protected List<string> tags = new List<string>();

	protected RuleBook currentRuleBook = new RuleBook();

	protected KeyValueSplitter ruleBookKvHelper;

	protected KeyValueSplitter modListKvHelper;

	protected bool disposed;

	protected float playerUpdateTimer;

	protected float playerUpdateInterval = 5f;

	private static string versionGameDataString;

	protected const int k_cbMaxGameServerGameData = 2048;

	public static T instance { get; private set; }

	public event Action<T, List<string>> collectAdditionalTags;

	public static string GetVersionGameDataString()
	{
		return versionGameDataString ?? (versionGameDataString = "gameData=" + RoR2Application.GetBuildId());
	}

	protected void OnPreGameControllerSetRuleBookServerGlobal(PreGameController preGameController, RuleBook ruleBook)
	{
		UpdateServerRuleBook();
	}

	protected void OnServerRunSetRuleBookGlobal(Run run, RuleBook ruleBook)
	{
		UpdateServerRuleBook();
	}

	protected void UpdateServerRuleBook()
	{
		if ((bool)Run.instance)
		{
			SetServerRuleBook(Run.instance.ruleBook);
		}
		else if ((bool)PreGameController.instance)
		{
			SetServerRuleBook(PreGameController.instance.readOnlyRuleBook);
		}
		UpdateTags();
	}

	protected void SetServerRuleBook(RuleBook ruleBook)
	{
		currentRuleBook.Copy(ruleBook);
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		RuleBook.WriteBase64ToStringBuilder(ruleBook, stringBuilder);
		ruleBookKvHelper.SetValue(stringBuilder.ToString());
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}

	protected void UpdateTags()
	{
		tags.Clear();
		if (RoR2Application.isModded)
		{
			tags.Add("mod");
		}
		tags.Add(PreGameController.cvSvAllowRuleVoting.value ? "rv1" : "rv0");
		tags.Add(PreGameController.cvSvAllowMultiplayerPause.value ? "rmp1" : "rmp0");
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		stringBuilder.Append("a=");
		foreach (RuleDef child in RuleCatalog.artifactRuleCategory.children)
		{
			RuleChoiceDef ruleChoice = currentRuleBook.GetRuleChoice(child);
			if (ruleChoice.localName == "On")
			{
				int artifactIndex = (int)((ArtifactDef)ruleChoice.extraData).artifactIndex;
				char value = (char)(48 + artifactIndex);
				stringBuilder.Append(value);
			}
		}
		tags.Add(stringBuilder.ToString());
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
		foreach (RuleChoiceDef choice in currentRuleBook.choices)
		{
			if (choice.serverTag != null)
			{
				tags.Add(choice.serverTag);
			}
		}
		this.collectAdditionalTags?.Invoke(instance, tags);
		tags.Add(NetworkManagerSystem.cvSvCustomTags.value);
		string value2 = string.Join(",", tags);
		if (!base.tagsString.Equals(value2, StringComparison.Ordinal))
		{
			base.tagsString = value2;
			TagsStringUpdated();
			onTagsStringUpdated?.Invoke(base.tagsString);
		}
	}

	protected virtual void TagsStringUpdated()
	{
	}

	public virtual void Dispose()
	{
		if (!disposed)
		{
			disposed = true;
			RoR2Application.onUpdate -= Update;
			Run.onServerRunSetRuleBookGlobal -= OnServerRunSetRuleBookGlobal;
			PreGameController.onPreGameControllerSetRuleBookServerGlobal -= OnPreGameControllerSetRuleBookServerGlobal;
		}
	}

	protected virtual void Update()
	{
		playerUpdateTimer -= Time.unscaledDeltaTime;
		if (playerUpdateTimer <= 0f)
		{
			playerUpdateTimer = playerUpdateInterval;
		}
	}

	public static void StartServer()
	{
		instance?.Dispose();
		instance = null;
		if (!NetworkServer.dontListen)
		{
			instance = new T();
			if (instance.disposed)
			{
				instance = null;
			}
		}
	}

	public static void StopServer()
	{
		instance?.Dispose();
		instance = null;
	}
}
