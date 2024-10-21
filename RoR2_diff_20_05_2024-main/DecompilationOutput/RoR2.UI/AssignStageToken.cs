using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class AssignStageToken : MonoBehaviour
{
	public TextMeshProUGUI titleText;

	public TextMeshProUGUI subtitleText;

	private void Start()
	{
		SceneDef mostRecentSceneDef = SceneCatalog.mostRecentSceneDef;
		titleText.SetText(Language.GetString(mostRecentSceneDef.nameToken));
		subtitleText.SetText(Language.GetString(mostRecentSceneDef.subtitleToken));
	}
}
