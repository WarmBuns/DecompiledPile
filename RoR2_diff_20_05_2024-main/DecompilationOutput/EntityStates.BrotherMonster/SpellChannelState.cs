using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace EntityStates.BrotherMonster;

public class SpellChannelState : SpellBaseState
{
	public static float stealInterval;

	public static float delayBeforeBeginningSteal;

	public static float maxDuration;

	public static GameObject channelEffectPrefab;

	private bool hasBegunSteal;

	private GameObject channelEffectInstance;

	private Transform spellChannelChildTransform;

	private bool hasSubscribedToStealFinish;

	private static int SpellChannelStateHash = Animator.StringToHash("SpellChannel");

	protected override bool DisplayWeapon => false;

	public override void OnEnter()
	{
		base.OnEnter();
		PlayAnimation("Body", SpellChannelStateHash);
		Util.PlaySound("Play_moonBrother_phase4_itemSuck_start", base.gameObject);
		spellChannelChildTransform = FindModelChild("SpellChannel");
		if ((bool)spellChannelChildTransform)
		{
			channelEffectInstance = Object.Instantiate(channelEffectPrefab, spellChannelChildTransform.position, Quaternion.identity, spellChannelChildTransform);
		}
		base.characterBody.AddBuff(RoR2Content.Buffs.Immune);
	}

	public override void FixedUpdate()
	{
		base.FixedUpdate();
		if (!itemStealController)
		{
			return;
		}
		if (!hasSubscribedToStealFinish && base.isAuthority)
		{
			hasSubscribedToStealFinish = true;
			if (NetworkServer.active)
			{
				itemStealController.onStealFinishServer.AddListener(OnStealEndAuthority);
			}
			else
			{
				itemStealController.onStealFinishClient += OnStealEndAuthority;
			}
		}
		if (NetworkServer.active && base.fixedAge > delayBeforeBeginningSteal && !hasBegunSteal)
		{
			hasBegunSteal = true;
			itemStealController.stealInterval = stealInterval;
			TeamIndex teamIndex = GetTeam();
			itemStealController.StartSteal((CharacterMaster characterMaster) => characterMaster.teamIndex != teamIndex && characterMaster.hasBody);
		}
		if (base.isAuthority && base.fixedAge > delayBeforeBeginningSteal + maxDuration)
		{
			outer.SetNextState(new SpellChannelExitState());
		}
		if ((bool)spellChannelChildTransform)
		{
			itemStealController.transform.position = spellChannelChildTransform.position;
		}
	}

	public override void OnExit()
	{
		if ((bool)itemStealController && hasSubscribedToStealFinish)
		{
			itemStealController.onStealFinishServer.RemoveListener(OnStealEndAuthority);
			itemStealController.onStealFinishClient -= OnStealEndAuthority;
		}
		if ((bool)channelEffectInstance)
		{
			EntityState.Destroy(channelEffectInstance);
		}
		Util.PlaySound("Play_moonBrother_phase4_itemSuck_end", base.gameObject);
		base.characterBody.RemoveBuff(RoR2Content.Buffs.Immune);
		base.OnExit();
	}

	private void OnStealEndAuthority()
	{
		outer.SetNextState(new SpellChannelExitState());
	}
}
