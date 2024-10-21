using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace RoR2.UI;

public class DebugStats : MonoBehaviour
{
	public GameObject statsRoot;

	public TextMeshProUGUI fpsAverageText;

	public float fpsAverageFrequency = 1f;

	public float fpsAverageTime = 60f;

	public float fpsAverageDisplayUpdateFrequency = 1f;

	private float fpsStopwatch;

	private float fpsDisplayStopwatch;

	private static Queue<float> fpsTimestamps;

	private int fpsTimestampCount;

	public TextMeshProUGUI budgetAverageText;

	public float budgetAverageFrequency = 1f;

	public float budgetAverageTime = 60f;

	private const float budgetAverageDisplayUpdateFrequency = 1f;

	private float budgetStopwatch;

	private float budgetDisplayStopwatch;

	private static Queue<float> budgetTimestamps;

	private int budgetTimestampCount;

	private static bool showStats;

	public TextMeshProUGUI teamText;

	private void Awake()
	{
		fpsTimestamps = new Queue<float>();
		fpsTimestampCount = (int)(fpsAverageTime / fpsAverageFrequency);
		budgetTimestamps = new Queue<float>();
		budgetTimestampCount = (int)(budgetAverageTime / budgetAverageFrequency);
	}

	private float GetAverageFPS()
	{
		if (fpsTimestamps.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		foreach (float fpsTimestamp in fpsTimestamps)
		{
			num += fpsTimestamp;
		}
		num /= (float)fpsTimestamps.Count;
		return Mathf.Round(num);
	}

	private float GetAverageParticleCost()
	{
		if (budgetTimestamps.Count == 0)
		{
			return 0f;
		}
		float num = 0f;
		foreach (float budgetTimestamp in budgetTimestamps)
		{
			num += budgetTimestamp;
		}
		num /= (float)budgetTimestamps.Count;
		return Mathf.Round(num);
	}

	private void Update()
	{
		statsRoot.SetActive(showStats);
		if (!showStats)
		{
			return;
		}
		fpsStopwatch += Time.unscaledDeltaTime;
		fpsDisplayStopwatch += Time.unscaledDeltaTime;
		float num = 1f / Time.unscaledDeltaTime;
		if (fpsStopwatch >= 1f / fpsAverageFrequency)
		{
			fpsStopwatch = 0f;
			fpsTimestamps.Enqueue(num);
			if (fpsTimestamps.Count > fpsTimestampCount)
			{
				fpsTimestamps.Dequeue();
			}
		}
		if (fpsDisplayStopwatch > fpsAverageDisplayUpdateFrequency)
		{
			fpsDisplayStopwatch = 0f;
			float averageFPS = GetAverageFPS();
			fpsAverageText.text = "FPS: " + Mathf.Round(num) + " (" + averageFPS + ")";
			TextMeshProUGUI textMeshProUGUI = fpsAverageText;
			textMeshProUGUI.text = textMeshProUGUI.text + "\n ms: " + Mathf.Round(100000f / num) / 100f + "(" + Mathf.Round(100000f / averageFPS) / 100f + ")";
		}
		budgetStopwatch += Time.unscaledDeltaTime;
		budgetDisplayStopwatch += Time.unscaledDeltaTime;
		float num2 = VFXBudget.totalCost;
		if (budgetStopwatch >= 1f / budgetAverageFrequency)
		{
			budgetStopwatch = 0f;
			budgetTimestamps.Enqueue(num2);
			if (budgetTimestamps.Count > budgetTimestampCount)
			{
				budgetTimestamps.Dequeue();
			}
		}
		if (budgetDisplayStopwatch > 1f)
		{
			budgetDisplayStopwatch = 0f;
			float averageParticleCost = GetAverageParticleCost();
			budgetAverageText.text = "Particle Cost: " + Mathf.Round(num2) + " (" + averageParticleCost + ")";
		}
		if (!teamText)
		{
			return;
		}
		string text = "Allies: " + TeamComponent.GetTeamMembers(TeamIndex.Player).Count + "\n";
		string text2 = "Enemies: " + TeamComponent.GetTeamMembers(TeamIndex.Monster).Count + "\n";
		string text3 = "Bosses: " + BossGroup.GetTotalBossCount() + "\n";
		string text4 = "Directors:";
		foreach (CombatDirector instances in CombatDirector.instancesList)
		{
			string text5 = "\n==[" + instances.gameObject.name + "]==";
			if (instances.enabled)
			{
				string text6 = "\n - Credit: " + instances.monsterCredit;
				string text7 = "\n - Target: " + (instances.currentSpawnTarget ? instances.currentSpawnTarget.name : "null");
				string text8 = "\n - Last Spawn Card: ";
				if (instances.lastAttemptedMonsterCard != null && (bool)instances.lastAttemptedMonsterCard.spawnCard)
				{
					text8 += instances.lastAttemptedMonsterCard.spawnCard.name;
				}
				string text9 = "\n - Reroll Timer: " + Mathf.Round(instances.monsterSpawnTimer);
				text5 = text5 + text6 + text7 + text8 + text9;
			}
			else
			{
				text5 += " (Off)";
			}
			text4 = text4 + text5 + "\n \n";
		}
		teamText.text = text + text2 + text3 + text4;
	}
}
