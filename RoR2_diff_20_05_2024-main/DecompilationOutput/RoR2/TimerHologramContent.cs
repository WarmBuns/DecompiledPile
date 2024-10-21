using TMPro;
using UnityEngine;

namespace RoR2;

public class TimerHologramContent : MonoBehaviour
{
	public float displayValue;

	public TextMeshPro targetTextMesh;

	private void FixedUpdate()
	{
		if ((bool)targetTextMesh)
		{
			int num = Mathf.FloorToInt(displayValue);
			int num2 = Mathf.FloorToInt((displayValue - (float)num) * 100f);
			targetTextMesh.text = $"{num:D}.{num2:D2}";
		}
	}
}
