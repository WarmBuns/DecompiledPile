using System.Collections;
using RoR2.ContentManagement;

namespace RoR2;

public class CU8Content : IContentPackProvider
{
	public static class Artifacts
	{
		public static ArtifactDef Devotion;

		public static ArtifactDef Delusion;
	}

	public static class Items
	{
		public static ItemDef LemurianHarness;
	}

	public static class ItemRelationshipTypes
	{
	}

	public static class Equipment
	{
	}

	public static class Buffs
	{
	}

	public static class Elites
	{
	}

	public static class GameEndings
	{
	}

	public static class Survivors
	{
	}

	public static class MiscPickups
	{
	}

	private ContentPack contentPack = new ContentPack();

	private static readonly string addressablesLabel = "ContentPack:RoR2.CU8";

	public string identifier => "RoR2.CU8";

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		contentPack.identifier = identifier;
		AddressablesLoadHelper loadHelper = AddressablesLoadHelper.CreateUsingDefaultResourceLocator(addressablesLabel);
		yield return loadHelper.AddContentPackLoadOperationWithYields(contentPack);
		loadHelper.AddGenericOperation(delegate
		{
			ContentLoadHelper.PopulateTypeFields(typeof(Artifacts), contentPack.artifactDefs);
			ContentLoadHelper.PopulateTypeFields(typeof(Items), contentPack.itemDefs);
		}, 0.05f);
		while (loadHelper.coroutine.MoveNext())
		{
			args.ReportProgress(loadHelper.progress.value);
			yield return loadHelper.coroutine.Current;
		}
	}

	public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
	{
		ContentPack.Copy(contentPack, args.output);
		yield break;
	}

	public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
	{
		yield break;
	}
}
