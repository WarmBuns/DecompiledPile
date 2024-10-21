using TMPro;
using UnityEngine;

namespace RoR2;

public class IncreaseDamageOnMultiKillItemDisplayUpdater : MonoBehaviour
{
	private CharacterBody body;

	[SerializeField]
	private TextMeshPro textMesh;

	private void Start()
	{
		body = GetComponentInParent<CharacterModel>().body;
		body.increaseDamageOnMultiKillItemDisplayUpdater = this;
	}

	public void UpdateKillCounterText(int killCounter)
	{
		string text = killCounter.ToString();
		string text2 = "00";
		if (killCounter > 9)
		{
			text2 = "0";
		}
		else if (killCounter > 99)
		{
			text2 = "";
		}
		else if (killCounter > 999)
		{
			text2 = "G0D";
			text = "";
		}
		text = text2 + text;
		textMesh.text = text;
	}
}
