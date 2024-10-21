using UnityEngine.Networking;

namespace RoR2;

public class AffixVoidBehavior : CharacterBody.ItemBehavior
{
	private const string sdStateMachineName = "Weapon";

	private bool wasVoidBody;

	private EntityStateMachine sdStateMachine;

	private HealthComponent healthComponent;

	private bool hasEffectiveAuthority;

	private bool hasBegunSelfDestruct;

	private void Awake()
	{
		base.enabled = false;
		sdStateMachine = EntityStateMachine.FindByCustomName(base.gameObject, "Weapon");
		healthComponent = GetComponent<HealthComponent>();
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
		hasBegunSelfDestruct = false;
	}

	private void OnEnable()
	{
		if ((bool)body)
		{
			wasVoidBody = (body.bodyFlags & CharacterBody.BodyFlags.Void) != 0;
			body.bodyFlags |= CharacterBody.BodyFlags.Void;
		}
	}

	private void OnDisable()
	{
		if ((bool)body)
		{
			if (body.HasBuff(DLC1Content.Buffs.BearVoidReady))
			{
				body.RemoveBuff(DLC1Content.Buffs.BearVoidReady);
			}
			if (body.HasBuff(DLC1Content.Buffs.BearVoidCooldown))
			{
				body.RemoveBuff(DLC1Content.Buffs.BearVoidCooldown);
			}
			if (!wasVoidBody)
			{
				body.bodyFlags &= ~CharacterBody.BodyFlags.Void;
			}
		}
	}

	private void FixedUpdate()
	{
		if (NetworkServer.active && (bool)body && !body.HasBuff(DLC1Content.Buffs.BearVoidReady) && !body.HasBuff(DLC1Content.Buffs.BearVoidCooldown))
		{
			body.AddBuff(DLC1Content.Buffs.BearVoidReady);
		}
	}
}
