using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class FPS : MonoBehaviour
{
	public TextMeshProUGUI targetText;

	private float deltaTime;

	private float stopwatch;

	private const float updateTime = 1f;

	private void Update()
	{
		stopwatch += Time.unscaledDeltaTime;
		deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
		if (stopwatch >= 1f)
		{
			stopwatch -= 1f;
			float num = deltaTime * 1000f;
			float num2 = 1f / deltaTime;
			string text = $"{num:0.0} ms \n{num2:0.} fps";
			targetText.text = text;
		}
	}
}
