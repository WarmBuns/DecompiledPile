using System;
using System.Collections.Generic;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class ExperienceManager : MonoBehaviour
{
	[Serializable]
	private struct TimedExpAward
	{
		public float awardTime;

		public ulong awardAmount;

		public TeamIndex recipient;
	}

	private class CreateExpEffectMessage : MessageBase
	{
		public Vector3 origin;

		public GameObject targetBody;

		public ulong awardAmount;

		public override void Serialize(NetworkWriter writer)
		{
			writer.Write(origin);
			writer.Write(targetBody);
			writer.WritePackedUInt64(awardAmount);
		}

		public override void Deserialize(NetworkReader reader)
		{
			origin = reader.ReadVector3();
			targetBody = reader.ReadGameObject();
			awardAmount = reader.ReadPackedUInt64();
		}
	}

	private static GameObject experienceOrbPrefab;

	private float localTime;

	private List<TimedExpAward> pendingAwards = new List<TimedExpAward>();

	private float nextAward;

	private bool shouldPool;

	private const float minOrbTravelTime = 0.5f;

	public const float maxOrbTravelTime = 2f;

	public static readonly float[] orbTimeOffsetSequence = new float[16]
	{
		0.841f, 0.394f, 0.783f, 0.799f, 0.912f, 0.197f, 0.335f, 0.768f, 0.278f, 0.554f,
		0.477f, 0.629f, 0.365f, 0.513f, 0.953f, 0.917f
	};

	private static CreateExpEffectMessage currentOutgoingCreateExpEffectMessage = new CreateExpEffectMessage();

	private static CreateExpEffectMessage currentIncomingCreateExpEffectMessage = new CreateExpEffectMessage();

	public static ExperienceManager instance { get; private set; }

	private static float CalcOrbTravelTime(float timeOffset)
	{
		return 0.5f + 1.5f * timeOffset;
	}

	private void OnEnable()
	{
		if ((bool)instance && instance != this)
		{
			Debug.LogError("Only one ExperienceManager can exist at a time.");
			return;
		}
		instance = this;
		if (!experienceOrbPrefab)
		{
			LegacyResourcesAPI.LoadAsyncCallback("Prefabs/ExpOrb", delegate(GameObject x)
			{
				experienceOrbPrefab = x;
				shouldPool = EffectManager.ShouldUsePooledEffect(experienceOrbPrefab);
			});
		}
	}

	private void OnDisable()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	private void Start()
	{
		localTime = 0f;
		nextAward = float.PositiveInfinity;
	}

	private void FixedUpdate()
	{
		localTime += Time.fixedDeltaTime;
		if (pendingAwards.Count <= 0 || !(nextAward <= localTime))
		{
			return;
		}
		nextAward = float.PositiveInfinity;
		for (int num = pendingAwards.Count - 1; num >= 0; num--)
		{
			if (pendingAwards[num].awardTime <= localTime)
			{
				if ((bool)TeamManager.instance)
				{
					TeamManager.instance.GiveTeamExperience(pendingAwards[num].recipient, pendingAwards[num].awardAmount);
				}
				pendingAwards.RemoveAt(num);
			}
			else if (pendingAwards[num].awardTime < nextAward)
			{
				nextAward = pendingAwards[num].awardTime;
			}
		}
	}

	public void AwardExperience(Vector3 origin, CharacterBody body, ulong amount)
	{
		CharacterMaster master = body.master;
		if (!master)
		{
			return;
		}
		TeamIndex teamIndex = master.teamIndex;
		List<ulong> list = CalculateDenominations(amount);
		uint num = 0u;
		for (int i = 0; i < list.Count; i++)
		{
			AddPendingAward(localTime + 0.5f + 1.5f * orbTimeOffsetSequence[num], teamIndex, list[i]);
			num++;
			if (num >= orbTimeOffsetSequence.Length)
			{
				num = 0u;
			}
		}
		currentOutgoingCreateExpEffectMessage.awardAmount = amount;
		currentOutgoingCreateExpEffectMessage.origin = origin;
		currentOutgoingCreateExpEffectMessage.targetBody = body.gameObject;
		NetworkServer.SendToAll(55, currentOutgoingCreateExpEffectMessage);
	}

	private void AddPendingAward(float awardTime, TeamIndex recipient, ulong awardAmount)
	{
		pendingAwards.Add(new TimedExpAward
		{
			awardTime = awardTime,
			recipient = recipient,
			awardAmount = awardAmount
		});
		if (nextAward > awardTime)
		{
			nextAward = awardTime;
		}
	}

	public List<ulong> CalculateDenominations(ulong total)
	{
		List<ulong> list = new List<ulong>();
		while (total != 0)
		{
			ulong num = (ulong)Math.Pow(6.0, Mathf.Floor(Mathf.Log(total, 6f)));
			total = Math.Max(total - num, 0uL);
			list.Add(num);
		}
		return list;
	}

	[NetworkMessageHandler(msgType = 55, client = true)]
	private static void HandleCreateExpEffect(NetworkMessage netMsg)
	{
		if ((bool)instance)
		{
			instance.HandleCreateExpEffectInternal(netMsg);
		}
	}

	private void HandleCreateExpEffectInternal(NetworkMessage netMsg)
	{
		netMsg.ReadMessage(currentIncomingCreateExpEffectMessage);
		if (!SettingsConVars.cvExpAndMoneyEffects.value)
		{
			return;
		}
		GameObject targetBody = currentIncomingCreateExpEffectMessage.targetBody;
		if (!targetBody)
		{
			return;
		}
		HurtBox hurtBox = Util.FindBodyMainHurtBox(targetBody);
		Transform targetTransform = (hurtBox ? hurtBox.transform : targetBody.transform);
		List<ulong> list = CalculateDenominations(currentIncomingCreateExpEffectMessage.awardAmount);
		uint num = 0u;
		ExperienceOrbBehavior experienceOrbBehavior = null;
		for (int i = 0; i < list.Count; i++)
		{
			if (!shouldPool)
			{
				experienceOrbBehavior = UnityEngine.Object.Instantiate(experienceOrbPrefab, currentIncomingCreateExpEffectMessage.origin, Quaternion.identity).GetComponent<ExperienceOrbBehavior>();
			}
			else
			{
				EffectManagerHelper pooledEffect = EffectManager.GetPooledEffect(experienceOrbPrefab, currentIncomingCreateExpEffectMessage.origin, Quaternion.identity);
				experienceOrbBehavior = pooledEffect.GetComponent<ExperienceOrbBehavior>();
				pooledEffect.Reset(inActivate: true);
				pooledEffect.gameObject.SetActive(value: true);
				pooledEffect.ResetPostActivate();
			}
			experienceOrbBehavior.targetTransform = targetTransform;
			experienceOrbBehavior.travelTime = CalcOrbTravelTime(orbTimeOffsetSequence[num]);
			experienceOrbBehavior.exp = list[i];
			experienceOrbBehavior.SetupStartingValues();
			num++;
			if (num >= orbTimeOffsetSequence.Length)
			{
				num = 0u;
			}
		}
	}
}
