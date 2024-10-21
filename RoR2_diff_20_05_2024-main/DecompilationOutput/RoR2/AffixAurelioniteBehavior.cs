using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class AffixAurelioniteBehavior : CharacterBody.ItemBehavior
{
	private float timer;

	private float duration = 20f;

	private float durationModifier = 5f;

	public int goldTransfer = 25;

	private void Start()
	{
		if (NetworkServer.active)
		{
			goldTransfer = Run.instance.GetDifficultyScaledCost(goldTransfer);
		}
		timer = Random.Range(5f, 15f);
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			timer -= Time.deltaTime;
			if (timer <= 0f && stack > 0)
			{
				timer = duration;
				AurelioniteBlessing();
			}
		}
	}

	private void AurelioniteBlessing()
	{
		body.AddTimedBuff(DLC2Content.Buffs.AurelioniteBlessing, 5f);
	}

	private void OnDisable()
	{
		if (body.HasBuff(DLC2Content.Buffs.AurelioniteBlessing))
		{
			body.RemoveBuff(DLC2Content.Buffs.AurelioniteBlessing);
		}
	}
}
