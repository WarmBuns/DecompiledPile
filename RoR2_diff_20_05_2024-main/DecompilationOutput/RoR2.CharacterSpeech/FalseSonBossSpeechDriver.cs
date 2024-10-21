using System;
using System.Collections.ObjectModel;
using EntityStates.FalseSonBoss;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2.CharacterSpeech;

[RequireComponent(typeof(CharacterSpeechController))]
[RequireComponent(typeof(SimpleCombatSpeechDriver))]
public class FalseSonBossSpeechDriver : BaseCharacterSpeechDriver
{
	public CharacterSpeechController.SpeechInfo[] seeHereticResponses;

	public CharacterSpeechController.SpeechInfo[] seeTitanGoldResponses;

	public CharacterSpeechController.SpeechInfo[] seeHereticAndTitanGoldResponses;

	public CharacterSpeechController.SpeechInfo[] killMechanicalResponses;

	public CharacterSpeechController.SpeechInfo[] killHereticResponses;

	public CharacterSpeechController.SpeechInfo[] killTitanGoldReponses;

	public CharacterSpeechController.SpeechInfo[] killPlayerWithLightningResponses;

	public CharacterSpeechController.SpeechInfo[] purifyFalseSonResponses;

	public UnityEvent onTitanGoldSighted;

	private SimpleCombatSpeechDriver simpleCombatSpeechDriver;

	private static BodyIndex hereticBodyIndex = BodyIndex.None;

	private static BodyIndex titanGoldBodyIndex = BodyIndex.None;

	[SystemInitializer(new Type[] { typeof(BodyCatalog) })]
	private static void Init()
	{
		hereticBodyIndex = BodyCatalog.FindBodyIndex("HereticBody");
		titanGoldBodyIndex = BodyCatalog.FindBodyIndex("TitanGoldBody");
	}

	protected new void Awake()
	{
		base.Awake();
		if (NetworkServer.active)
		{
			simpleCombatSpeechDriver = GetComponent<SimpleCombatSpeechDriver>();
			simpleCombatSpeechDriver.onBodyKill.AddListener(OnBodyKill);
			simpleCombatSpeechDriver.onBossAllyDeath.AddListener(OnBossAllyDeath);
			CharacterBody.onBodyStartGlobal += OnCharacterBodyStartGlobal;
			SkyJumpDeathState.falseSonUnlockEvent += FalseSonPurifiedResponse;
		}
	}

	protected new void OnDestroy()
	{
		CharacterBody.onBodyStartGlobal -= OnCharacterBodyStartGlobal;
		SkyJumpDeathState.falseSonUnlockEvent -= FalseSonPurifiedResponse;
		base.OnDestroy();
	}

	private void OnCharacterBodyStartGlobal(CharacterBody characterBody)
	{
		if (characterBody.bodyIndex == titanGoldBodyIndex)
		{
			onTitanGoldSighted?.Invoke();
		}
	}

	public void DoInitialSightResponse()
	{
		bool flag = false;
		bool flag2 = false;
		ReadOnlyCollection<CharacterBody> readOnlyInstancesList = CharacterBody.readOnlyInstancesList;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			BodyIndex bodyIndex = readOnlyInstancesList[i].bodyIndex;
			flag |= bodyIndex == hereticBodyIndex;
			flag2 |= bodyIndex == titanGoldBodyIndex;
		}
		CharacterSpeechController.SpeechInfo[] responsePool = Array.Empty<CharacterSpeechController.SpeechInfo>();
		if (flag && flag2)
		{
			TrySetResponsePool(seeHereticAndTitanGoldResponses);
		}
		if (flag)
		{
			TrySetResponsePool(seeHereticResponses);
		}
		if (flag2)
		{
			TrySetResponsePool(seeTitanGoldResponses);
		}
		SendReponseFromPool(responsePool);
		void TrySetResponsePool(CharacterSpeechController.SpeechInfo[] newResponsePool)
		{
			if (responsePool.Length == 0)
			{
				responsePool = newResponsePool;
			}
		}
	}

	private void OnBodyKill(DamageReport damageReport)
	{
		if (!damageReport.victimBody)
		{
			return;
		}
		CharacterSpeechController.SpeechInfo[] responsePool = Array.Empty<CharacterSpeechController.SpeechInfo>();
		if (damageReport.victimBodyIndex == hereticBodyIndex)
		{
			TrySetResponsePool(killHereticResponses);
		}
		else if ((damageReport.victimBody.bodyFlags &= CharacterBody.BodyFlags.Mechanical) == CharacterBody.BodyFlags.Mechanical)
		{
			if (killMechanicalResponses.Length != 0)
			{
				TrySetResponsePool(killMechanicalResponses);
			}
		}
		else if ((ulong)(damageReport.damageInfo.damageType & DamageType.LunarRuin) != 0 && damageReport.attacker == null)
		{
			TrySetResponsePool(killPlayerWithLightningResponses);
		}
		SendReponseFromPool(responsePool);
		void TrySetResponsePool(CharacterSpeechController.SpeechInfo[] newResponsePool)
		{
			if (responsePool.Length == 0)
			{
				responsePool = newResponsePool;
			}
		}
	}

	private void OnBossAllyDeath(DamageReport damageReport)
	{
		CharacterSpeechController.SpeechInfo[] responsePool;
		if ((bool)damageReport.victimBody)
		{
			responsePool = Array.Empty<CharacterSpeechController.SpeechInfo>();
			if (damageReport.victimBodyIndex == titanGoldBodyIndex)
			{
				TrySetResponsePool(killTitanGoldReponses);
			}
			SendReponseFromPool(responsePool);
		}
		void TrySetResponsePool(CharacterSpeechController.SpeechInfo[] newResponsePool)
		{
			if (responsePool.Length == 0)
			{
				responsePool = newResponsePool;
			}
		}
	}

	private void FalseSonPurifiedResponse()
	{
		CharacterSpeechController.SpeechInfo[] responsePool = Array.Empty<CharacterSpeechController.SpeechInfo>();
		TrySetResponsePool(purifyFalseSonResponses);
		SendReponseFromPool(responsePool);
		void TrySetResponsePool(CharacterSpeechController.SpeechInfo[] newResponsePool)
		{
			if (responsePool.Length == 0)
			{
				responsePool = newResponsePool;
			}
		}
	}

	private void SendReponseFromPool(CharacterSpeechController.SpeechInfo[] responsePool)
	{
		if (responsePool.Length != 0)
		{
			base.characterSpeechController.EnqueueSpeech(in responsePool[UnityEngine.Random.Range(0, responsePool.Length - 1)]);
		}
	}
}
