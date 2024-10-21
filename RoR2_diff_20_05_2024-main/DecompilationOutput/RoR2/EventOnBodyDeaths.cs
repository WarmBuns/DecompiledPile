using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class EventOnBodyDeaths : MonoBehaviour
{
	public string[] bodyNames;

	private int currentDeathCount;

	public int targetDeathCount;

	public UnityEvent onAchieved;

	private void OnEnable()
	{
		GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath;
	}

	private void OnDisable()
	{
		GlobalEventManager.onCharacterDeathGlobal -= OnCharacterDeath;
	}

	private void OnCharacterDeath(DamageReport damageReport)
	{
		if ((bool)damageReport.victimBody)
		{
			for (int i = 0; i < bodyNames.Length; i++)
			{
				if (damageReport.victimBody.name.Contains(bodyNames[i]))
				{
					currentDeathCount++;
					break;
				}
			}
		}
		if (currentDeathCount >= targetDeathCount)
		{
			onAchieved?.Invoke();
		}
	}
}
