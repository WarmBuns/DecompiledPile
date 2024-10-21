using System;
using System.Collections;
using System.Collections.Generic;
using RoR2.ContentManagement;
using UnityEngine;

namespace RoR2.Modding;

public class LegacyModContentPackProvider : IContentPackProvider
{
	private ContentPack finalizedContentPack;

	public static LegacyModContentPackProvider instance { get; private set; } = new LegacyModContentPackProvider();

	public ContentPack registrationContentPack { get; private set; }

	public bool cutoffReached { get; private set; }

	public string identifier => "RoR2.LegacyModContent";

	private LegacyModContentPackProvider()
	{
		registrationContentPack = new ContentPack();
		finalizedContentPack = new ContentPack();
		ContentManager.collectContentPackProviders += delegate(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
		{
			addContentPackProvider(this);
		};
	}

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		ContentPack.Copy(registrationContentPack, finalizedContentPack);
		yield break;
	}

	public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
	{
		ContentPack.Copy(finalizedContentPack, args.output);
		yield break;
	}

	public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
	{
		ContentPack.Copy(new ContentPack(), registrationContentPack);
		finalizedContentPack = null;
		yield break;
	}

	public void HandleLegacyGetAdditionalEntries<TAsset>(string eventName, Action<List<TAsset>> callback, NamedAssetCollection<TAsset> dest)
	{
		if (cutoffReached)
		{
			throw new InvalidOperationException("Legacy mod ContentPack has been finalized. It is too late to add additional entries via " + eventName + ".");
		}
		List<TAsset> list = new List<TAsset>();
		try
		{
			callback?.Invoke(list);
			dest.Add(list.ToArray());
			Debug.LogWarning("Added content to legacy mod ContentPack via " + eventName + " succeeded. Do not use this code path; it will be removed in a future update. Use IContentPackProvider instead.");
		}
		catch (Exception arg)
		{
			Debug.LogError($"Adding content to legacy mod ContentPack via {eventName} failed: {arg}.");
		}
	}
}
