namespace RoR2.ContentManagement;

public struct ContentPackLoadInfo
{
	public int index;

	public string contentPackProviderIdentifier;

	public IContentPackProvider contentPackProvider;

	public ReadOnlyContentPack previousContentPack;

	public int retries;
}
