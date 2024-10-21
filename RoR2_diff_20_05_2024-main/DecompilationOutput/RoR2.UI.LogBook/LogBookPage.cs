using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI.LogBook;

public class LogBookPage : MonoBehaviour
{
	public RawImage iconImage;

	public ModelPanel modelPanel;

	public TextMeshProUGUI titleText;

	public TextMeshProUGUI categoryText;

	public TextMeshProUGUI pageNumberText;

	public RectTransform contentContainer;

	private PageBuilder pageBuilder;

	[Header("Buttons")]
	public GameObject scroll;

	public LanguageTextMeshController scrollLabel;

	public GameObject back;

	public LanguageTextMeshController backLabel;

	public GameObject navigate;

	public LanguageTextMeshController navigateLabel;

	public GameObject rotate;

	public LanguageTextMeshController rotateLabel;

	public void SetEntry(UserProfile userProfile, Entry entry)
	{
		pageBuilder?.Destroy();
		pageBuilder = new PageBuilder();
		pageBuilder.container = contentContainer;
		pageBuilder.entry = entry;
		pageBuilder.userProfile = userProfile;
		entry.pageBuilderMethod?.Invoke(pageBuilder);
		iconImage.texture = entry.iconTexture;
		titleText.text = entry.GetDisplayName(userProfile);
		categoryText.text = entry.GetCategoryDisplayName(userProfile);
		modelPanel.modelPrefab = entry.modelPrefab;
		modelPanel.transform.parent.parent.gameObject.SetActive(entry.modelPrefab);
		UpdateButtons(entry.category.navButtons);
	}

	private void UpdateButtons(CategoryDef.NavButtons buttons)
	{
		RelabelButton(scroll, scrollLabel, buttons.scroll);
		RelabelButton(back, backLabel, buttons.back);
		RelabelButton(navigate, navigateLabel, buttons.navigate);
		RelabelButton(rotate, rotateLabel, buttons.rotate);
	}

	private void RelabelButton(GameObject button, LanguageTextMeshController label, string text)
	{
		if (text == "")
		{
			button.gameObject.SetActive(value: false);
			return;
		}
		button.gameObject.SetActive(value: true);
		label.token = text;
	}
}
