using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class InfiniteTowerEnemyCounter : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The root we're toggling")]
	private GameObject rootObject;

	[SerializeField]
	[Tooltip("The text we're setting")]
	private TextMeshProUGUI counterText;

	[SerializeField]
	[Tooltip("The language token for the text field")]
	private string token;

	private InfiniteTowerWaveController waveController;

	private CombatSquad combatSquad;

	private string counterTextString;

	private bool wasActive;

	private int oldMemberCount = -1;

	private void OnEnable()
	{
		InfiniteTowerRun infiniteTowerRun = Run.instance as InfiniteTowerRun;
		if ((bool)infiniteTowerRun)
		{
			waveController = infiniteTowerRun.waveController;
			if ((bool)waveController)
			{
				combatSquad = waveController.GetComponent<CombatSquad>();
				if ((bool)combatSquad)
				{
					rootObject.SetActive(waveController.HasFullProgress() && combatSquad.memberCount > 0);
				}
				else
				{
					rootObject.SetActive(value: false);
				}
			}
			else
			{
				rootObject.SetActive(value: false);
			}
		}
		counterTextString = Language.GetString(token);
	}

	private void Update()
	{
		if ((bool)combatSquad)
		{
			int memberCount = combatSquad.memberCount;
			bool flag = waveController.HasFullProgress() && combatSquad.memberCount > 0;
			if (flag != wasActive)
			{
				rootObject.SetActive(flag);
				wasActive = flag;
			}
			if (flag && (bool)counterText && memberCount != oldMemberCount)
			{
				oldMemberCount = memberCount;
				string arg = memberCount.ToString();
				counterText.text = string.Format(counterTextString, arg);
			}
		}
	}
}
