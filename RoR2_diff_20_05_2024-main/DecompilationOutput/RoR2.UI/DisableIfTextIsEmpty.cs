using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class DisableIfTextIsEmpty : MonoBehaviour
{
	public GameObject[] gameObjects;

	public TextMeshProUGUI tmpUGUI;

	private bool isActive;

	private void Update()
	{
		bool flag = !string.IsNullOrEmpty(tmpUGUI.text);
		if (flag != isActive)
		{
			isActive = flag;
			GameObject[] array = gameObjects;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(isActive);
			}
		}
	}
}
