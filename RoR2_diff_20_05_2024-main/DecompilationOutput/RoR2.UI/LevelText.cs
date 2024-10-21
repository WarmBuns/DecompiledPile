using System.Text;
using HG;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class LevelText : MonoBehaviour
{
	public CharacterBody source;

	public TextMeshProUGUI targetText;

	private uint displayData;

	private static readonly StringBuilder sharedStringBuilder = new StringBuilder();

	private void SetDisplayData(uint newDisplayData)
	{
		if (displayData != newDisplayData)
		{
			displayData = newDisplayData;
			uint value = displayData;
			sharedStringBuilder.Clear();
			sharedStringBuilder.AppendUint(value);
			targetText.SetText(sharedStringBuilder);
		}
	}

	private void Update()
	{
		if ((bool)source)
		{
			SetDisplayData(Convert.FloorToUIntClamped(source.level));
		}
	}

	private void OnValidate()
	{
		if (!targetText)
		{
			Debug.LogError("targetText must be assigned.");
		}
	}
}
