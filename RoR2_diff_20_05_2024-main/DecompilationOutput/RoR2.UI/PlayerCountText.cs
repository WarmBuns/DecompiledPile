using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace RoR2.UI;

public class PlayerCountText : MonoBehaviour
{
	public Text targetText;

	private void Update()
	{
		if ((bool)targetText)
		{
			targetText.text = $"{NetworkUser.readOnlyInstancesList.Count}/{NetworkManager.singleton.maxConnections}";
		}
	}
}
