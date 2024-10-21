using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class AmbientLevelDisplay : MonoBehaviour
{
	public TextMeshProUGUI text;

	private int lastLevel = -1;

	private void Update()
	{
		int num = -1;
		if ((bool)Run.instance)
		{
			num = Run.instance.ambientLevelFloor;
		}
		if (num != lastLevel)
		{
			string text = "-";
			if ((bool)Run.instance)
			{
				text = num.ToString();
			}
			this.text.text = Language.GetStringFormatted("AMBIENT_LEVEL_DISPLAY_FORMAT", text);
			lastLevel = num;
		}
	}
}
