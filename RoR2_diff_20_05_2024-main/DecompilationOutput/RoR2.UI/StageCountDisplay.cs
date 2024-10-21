using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class StageCountDisplay : MonoBehaviour
{
	public TextMeshProUGUI text;

	private int lastStage = -1;

	private void Update()
	{
		if (!(Run.instance != null))
		{
			return;
		}
		int num = Run.instance.stageClearCount + 1;
		if (num != lastStage)
		{
			string text = "-";
			if ((bool)Run.instance)
			{
				text = num.ToString();
			}
			this.text.text = Language.GetStringFormatted("STAGE_COUNT_FORMAT", text);
			lastStage = num;
		}
	}
}
