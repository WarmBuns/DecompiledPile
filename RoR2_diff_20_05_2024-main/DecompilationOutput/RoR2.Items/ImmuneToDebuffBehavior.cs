using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RoR2.Items;

public class ImmuneToDebuffBehavior : BaseItemBodyBehavior
{
	public const float barrierFraction = 0.1f;

	public const float cooldownSeconds = 5f;

	private HealthComponent healthComponent;

	private bool isProtected;

	[ItemDefAssociation(useOnServer = true, useOnClient = false)]
	private static ItemDef GetItemDef()
	{
		return DLC1Content.Items.ImmuneToDebuff;
	}

	public static bool OverrideDebuff(BuffIndex buffIndex, CharacterBody body)
	{
		BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
		if ((bool)buffDef)
		{
			return OverrideDebuff(buffDef, body);
		}
		return false;
	}

	public static bool OverrideDebuff(BuffDef buffDef, CharacterBody body)
	{
		if (buffDef.buffIndex != BuffIndex.None && buffDef.isDebuff)
		{
			return TryApplyOverride(body);
		}
		return false;
	}

	public static bool OverrideDot(InflictDotInfo inflictDotInfo)
	{
		CharacterBody characterBody = inflictDotInfo.victimObject?.GetComponent<CharacterBody>();
		if ((bool)characterBody)
		{
			return TryApplyOverride(characterBody);
		}
		return false;
	}

	private static bool TryApplyOverride(CharacterBody body)
	{
		ImmuneToDebuffBehavior component = body.GetComponent<ImmuneToDebuffBehavior>();
		if ((bool)component)
		{
			if (component.isProtected)
			{
				return true;
			}
			if (body.HasBuff(DLC1Content.Buffs.ImmuneToDebuffReady) && (bool)component.healthComponent)
			{
				component.healthComponent.AddBarrier(0.1f * component.healthComponent.fullCombinedHealth);
				body.RemoveBuff(DLC1Content.Buffs.ImmuneToDebuffReady);
				EffectManager.SimpleImpactEffect(Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ImmuneToDebuff/ImmuneToDebuffEffect.prefab").WaitForCompletion(), body.corePosition, Vector3.up, transmit: true);
				if (!body.HasBuff(DLC1Content.Buffs.ImmuneToDebuffReady))
				{
					body.AddTimedBuff(DLC1Content.Buffs.ImmuneToDebuffCooldown, 5f);
				}
				component.isProtected = true;
				return true;
			}
		}
		return false;
	}

	private void OnEnable()
	{
		healthComponent = GetComponent<HealthComponent>();
	}

	private void OnDisable()
	{
		healthComponent = null;
		if ((bool)base.body)
		{
			while (base.body.GetBuffCount(DLC1Content.Buffs.ImmuneToDebuffReady) > 0)
			{
				base.body.RemoveBuff(DLC1Content.Buffs.ImmuneToDebuffReady);
			}
			if (base.body.HasBuff(DLC1Content.Buffs.ImmuneToDebuffCooldown))
			{
				base.body.RemoveBuff(DLC1Content.Buffs.ImmuneToDebuffCooldown);
			}
		}
	}

	private void FixedUpdate()
	{
		isProtected = false;
		bool flag = base.body.HasBuff(DLC1Content.Buffs.ImmuneToDebuffCooldown);
		bool flag2 = base.body.HasBuff(DLC1Content.Buffs.ImmuneToDebuffReady);
		if (!flag && !flag2)
		{
			for (int i = 0; i < stack; i++)
			{
				base.body.AddBuff(DLC1Content.Buffs.ImmuneToDebuffReady);
			}
		}
		if (flag2 && flag)
		{
			base.body.RemoveBuff(DLC1Content.Buffs.ImmuneToDebuffReady);
		}
	}
}
