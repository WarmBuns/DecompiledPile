using UnityEngine;

namespace RoR2.UI;

public class ButtonSelectionController : MonoBehaviour
{
	public MPButton[] buttons;

	public void SelectThisButton(MPButton selectedButton)
	{
		for (int i = 0; i < buttons.Length; i++)
		{
			_ = buttons[i] == selectedButton;
		}
	}
}
