using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class InfiniteTowerWaveCounter : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The text we're setting")]
	private TextMeshProUGUI counterText;

	[SerializeField]
	[Tooltip("The language token for the text field")]
	private string token;

	private InfiniteTowerRun runInstance;

	private string counterTextString;

	private int oldWaveIndex = -1;

	private void OnEnable()
	{
		runInstance = Run.instance as InfiniteTowerRun;
		counterTextString = Language.GetString(token);
	}

	private void Update()
	{
		if ((bool)runInstance && (bool)counterText && runInstance.waveIndex != oldWaveIndex)
		{
			oldWaveIndex = runInstance.waveIndex;
			counterText.text = string.Format(counterTextString, runInstance.waveIndex);
		}
	}
}
