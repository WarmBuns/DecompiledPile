using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Image))]
internal class CurrentDifficultyIconController : MonoBehaviour
{
	private void Start()
	{
		if ((bool)Run.instance)
		{
			DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(Run.instance.selectedDifficulty);
			GetComponent<Image>().sprite = difficultyDef.GetIconSprite();
		}
	}
}
