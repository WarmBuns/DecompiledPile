using Facepunch.Steamworks;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class SteamBuildIdLabel : MonoBehaviour
{
	private void Start()
	{
		string text = "ver. " + RoR2Application.GetBuildId();
		if (!string.IsNullOrEmpty("290"))
		{
			text += "#290";
		}
		if (Client.Instance != null)
		{
			string betaName = Client.Instance.BetaName;
			if (!string.IsNullOrEmpty(betaName))
			{
				text = text + "[" + betaName + "]";
			}
			GetComponent<TextMeshProUGUI>().text = text;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
	}
}
