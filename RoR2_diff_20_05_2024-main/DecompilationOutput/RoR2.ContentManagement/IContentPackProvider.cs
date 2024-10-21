using System.Collections;

namespace RoR2.ContentManagement;

public interface IContentPackProvider
{
	string identifier { get; }

	IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args);

	IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args);

	IEnumerator FinalizeAsync(FinalizeAsyncArgs args);
}
