namespace RoR2;

internal class OutOfCombatArmorBehavior : CharacterBody.ItemBehavior
{
	private bool providingBuff;

	private void SetProvidingBuff(bool shouldProvideBuff)
	{
		if (shouldProvideBuff != providingBuff)
		{
			providingBuff = shouldProvideBuff;
			if (providingBuff)
			{
				body.AddBuff(DLC1Content.Buffs.OutOfCombatArmorBuff);
			}
			else
			{
				body.RemoveBuff(DLC1Content.Buffs.OutOfCombatArmorBuff);
			}
		}
	}

	private void OnDisable()
	{
		SetProvidingBuff(shouldProvideBuff: false);
	}

	private void FixedUpdate()
	{
		SetProvidingBuff(body.outOfDanger);
	}
}
