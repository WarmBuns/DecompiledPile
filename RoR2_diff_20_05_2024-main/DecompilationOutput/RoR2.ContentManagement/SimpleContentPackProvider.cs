using System.Collections;

namespace RoR2.ContentManagement;

public class SimpleContentPackProvider : IContentPackProvider
{
	public delegate IEnumerator LoadStaticContentAsyncDelegate(LoadStaticContentAsyncArgs args);

	public delegate IEnumerator GenerateContentPackAsyncDelegate(GetContentPackAsyncArgs args);

	public delegate IEnumerator FinalizeAsyncDelegate(FinalizeAsyncArgs args);

	public string identifier { get; set; }

	public LoadStaticContentAsyncDelegate loadStaticContentImplementation { get; set; }

	public GenerateContentPackAsyncDelegate generateContentPackAsyncImplementation { get; set; }

	public FinalizeAsyncDelegate finalizeAsyncImplementation { get; set; }

	public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
	{
		return loadStaticContentImplementation?.Invoke(args);
	}

	public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
	{
		return generateContentPackAsyncImplementation?.Invoke(args);
	}

	public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
	{
		return finalizeAsyncImplementation?.Invoke(args);
	}
}
