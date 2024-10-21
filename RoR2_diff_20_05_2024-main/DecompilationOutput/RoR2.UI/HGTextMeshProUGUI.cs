using TMPro;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2.UI;

public class HGTextMeshProUGUI : TextMeshProUGUI
{
	public bool useLanguageDefaultFont = true;

	public static TMP_FontAsset defaultLanguageFont;

	[InitDuringStartup]
	private static void Init()
	{
		Language.onCurrentLanguageChanged += OnCurrentLanguageChanged;
		OnCurrentLanguageChanged();
	}

	private static void OnCurrentLanguageChanged()
	{
		AsyncOperationHandle<TMP_FontAsset> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<TMP_FontAsset>(Language.GetString("DEFAULT_FONT"));
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<TMP_FontAsset> x)
		{
			defaultLanguageFont = x.Result;
		};
	}

	protected override void Awake()
	{
		base.Awake();
		if (useLanguageDefaultFont && defaultLanguageFont != null)
		{
			base.font = defaultLanguageFont;
			UpdateFontAsset();
		}
	}
}
