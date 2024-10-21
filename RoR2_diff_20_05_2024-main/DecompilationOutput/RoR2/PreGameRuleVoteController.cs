using System;
using System.Collections.Generic;
using System.Linq;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class PreGameRuleVoteController : NetworkBehaviour
{
	private static class LocalUserBallotPersistenceManager
	{
		private static readonly Dictionary<LocalUser, Vote[]> votesCache;

		static LocalUserBallotPersistenceManager()
		{
			votesCache = new Dictionary<LocalUser, Vote[]>();
			LocalUserManager.onUserSignIn += OnLocalUserSignIn;
			LocalUserManager.onUserSignOut += OnLocalUserSignOut;
			onVotesUpdated += OnVotesUpdated;
		}

		private static void OnLocalUserSignIn(LocalUser localUser)
		{
			votesCache.Add(localUser, null);
		}

		private static void OnLocalUserSignOut(LocalUser localUser)
		{
			votesCache.Remove(localUser);
		}

		private static void OnVotesUpdated()
		{
			foreach (PreGameRuleVoteController instances in instancesList)
			{
				if (instances.localUser != null)
				{
					votesCache[instances.localUser] = instances.votes;
				}
			}
		}

		public static void ApplyPersistentBallotIfPresent(LocalUser localUser, Vote[] dest)
		{
			if (votesCache.TryGetValue(localUser, out var value) && value != null)
			{
				Debug.LogFormat("Applying persistent ballot of votes for LocalUser {0}.", localUser.userProfile.name);
				Array.Copy(value, dest, value.Length);
			}
		}
	}

	[Serializable]
	private struct Vote
	{
		[SerializeField]
		private byte internalValue;

		public bool hasVoted => internalValue != 0;

		public int choiceValue
		{
			get
			{
				return internalValue - 1;
			}
			set
			{
				internalValue = (byte)(value + 1);
			}
		}

		public static void Serialize(NetworkWriter writer, Vote vote)
		{
			writer.Write(vote.internalValue);
		}

		public static Vote Deserialize(NetworkReader reader)
		{
			Vote result = default(Vote);
			result.internalValue = reader.ReadByte();
			return result;
		}
	}

	private static readonly List<PreGameRuleVoteController> instancesList;

	private const byte networkUserIdentityDirtyBit = 1;

	private const byte votesDirtyBit = 2;

	private const byte allDirtyBits = 3;

	private Vote[] votes = CreateBallot();

	public static int[] votesForEachChoice;

	private bool clientShouldTransmit;

	private NetworkUser networkUser;

	private static bool shouldUpdateGameVotes;

	private readonly RuleMask ruleMaskBuffer = new RuleMask();

	public NetworkIdentity networkUserNetworkIdentity { get; private set; }

	private LocalUser localUser => networkUser?.localUser;

	public static event Action onVotesUpdated;

	public static PreGameRuleVoteController FindForUser(NetworkUser networkUser)
	{
		GameObject gameObject = networkUser.gameObject;
		foreach (PreGameRuleVoteController instances in instancesList)
		{
			if ((bool)instances.networkUserNetworkIdentity && instances.networkUserNetworkIdentity.gameObject == gameObject)
			{
				return instances;
			}
		}
		return null;
	}

	public static void CreateForNetworkUserServer(NetworkUser networkUser)
	{
		GameObject obj = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/PreGameRuleVoteController"));
		PreGameRuleVoteController component = obj.GetComponent<PreGameRuleVoteController>();
		component.networkUserNetworkIdentity = networkUser.GetComponent<NetworkIdentity>();
		component.networkUser = networkUser;
		NetworkServer.Spawn(obj);
	}

	private static Vote[] CreateBallot()
	{
		return new Vote[RuleCatalog.ruleCount];
	}

	[SystemInitializer(new Type[] { typeof(RuleCatalog) })]
	private static void Init()
	{
		votesForEachChoice = new int[RuleCatalog.choiceCount];
	}

	private void Start()
	{
		if (localUser != null)
		{
			LocalUserBallotPersistenceManager.ApplyPersistentBallotIfPresent(localUser, votes);
			ClientTransmitVotesToServer();
		}
		if (NetworkServer.active)
		{
			UpdateGameVotes();
		}
	}

	private void Update()
	{
		if (NetworkServer.active && !networkUserNetworkIdentity)
		{
			UnityEngine.Object.Destroy(base.gameObject);
			return;
		}
		if (clientShouldTransmit)
		{
			clientShouldTransmit = false;
			ClientTransmitVotesToServer();
		}
		if (shouldUpdateGameVotes)
		{
			shouldUpdateGameVotes = false;
			UpdateGameVotes();
		}
	}

	[Client]
	private void ClientTransmitVotesToServer()
	{
		if (!NetworkClient.active)
		{
			Debug.LogWarning("[Client] function 'System.Void RoR2.PreGameRuleVoteController::ClientTransmitVotesToServer()' called on server");
		}
		else if ((bool)networkUserNetworkIdentity)
		{
			NetworkUser component = networkUserNetworkIdentity.GetComponent<NetworkUser>();
			if ((bool)component)
			{
				NetworkWriter networkWriter = new NetworkWriter();
				networkWriter.StartMessage(70);
				networkWriter.Write(base.gameObject);
				WriteVotes(networkWriter);
				networkWriter.FinishMessage();
				component.connectionToServer.SendWriter(networkWriter, QosChannelIndex.defaultReliable.intVal);
			}
		}
	}

	[NetworkMessageHandler(msgType = 70, client = false, server = true)]
	public static void ServerHandleClientVoteUpdate(NetworkMessage netMsg)
	{
		Debug.LogFormat("Received vote from {0}", NetworkUser.readOnlyInstancesList.FirstOrDefault((NetworkUser v) => v.connectionToClient == netMsg.conn)?.userName);
		GameObject gameObject = netMsg.reader.ReadGameObject();
		if (!gameObject)
		{
			return;
		}
		PreGameRuleVoteController component = gameObject.GetComponent<PreGameRuleVoteController>();
		if (!component)
		{
			return;
		}
		NetworkIdentity networkIdentity = component.networkUserNetworkIdentity;
		if (!networkIdentity)
		{
			return;
		}
		NetworkUser component2 = networkIdentity.GetComponent<NetworkUser>();
		if ((bool)component2)
		{
			if (component2.connectionToClient != netMsg.conn)
			{
				Debug.LogFormat("PreGameRuleVoteController.ServerHandleClientVoteUpdate() failed: {0}!={1}", component.connectionToClient, netMsg.conn);
			}
			else
			{
				Debug.LogFormat("Accepting vote from {0}", component2.userName);
				component.ReadVotes(netMsg.reader);
			}
		}
	}

	public void SetVote(int ruleIndex, int choiceValue)
	{
		Vote vote = votes[ruleIndex];
		if (vote.choiceValue != choiceValue)
		{
			votes[ruleIndex].choiceValue = choiceValue;
			if (!NetworkServer.active && (bool)networkUserNetworkIdentity && networkUserNetworkIdentity.isLocalPlayer)
			{
				clientShouldTransmit = true;
			}
			else
			{
				SetDirtyBit(2u);
			}
			shouldUpdateGameVotes = true;
		}
	}

	private static void UpdateGameVotes()
	{
		int i = 0;
		for (int choiceCount = RuleCatalog.choiceCount; i < choiceCount; i++)
		{
			votesForEachChoice[i] = 0;
		}
		int j = 0;
		for (int ruleCount = RuleCatalog.ruleCount; j < ruleCount; j++)
		{
			RuleDef ruleDef = RuleCatalog.GetRuleDef(j);
			int count = ruleDef.choices.Count;
			foreach (PreGameRuleVoteController instances in instancesList)
			{
				Vote vote = instances.votes[j];
				if (vote.hasVoted && vote.choiceValue < count)
				{
					RuleChoiceDef ruleChoiceDef = ruleDef.choices[vote.choiceValue];
					votesForEachChoice[ruleChoiceDef.globalIndex]++;
				}
			}
		}
		if (NetworkServer.active)
		{
			bool flag = false;
			int k = 0;
			for (int ruleCount2 = RuleCatalog.ruleCount; k < ruleCount2; k++)
			{
				RuleDef ruleDef2 = RuleCatalog.GetRuleDef(k);
				int count2 = ruleDef2.choices.Count;
				PreGameController.instance.readOnlyRuleBook.GetRuleChoiceIndex(ruleDef2);
				int ruleChoiceIndex = -1;
				int num = 0;
				bool flag2 = false;
				for (int l = 0; l < count2; l++)
				{
					RuleChoiceDef ruleChoiceDef2 = ruleDef2.choices[l];
					int num2 = votesForEachChoice[ruleChoiceDef2.globalIndex];
					if (num2 == num)
					{
						flag2 = true;
					}
					else if (num2 > num)
					{
						ruleChoiceIndex = ruleChoiceDef2.globalIndex;
						num = num2;
						flag2 = false;
					}
				}
				if (num == 0)
				{
					ruleChoiceIndex = ruleDef2.choices[ruleDef2.defaultChoiceIndex].globalIndex;
				}
				if (!flag2 || num == 0)
				{
					flag = PreGameController.instance.ApplyChoice(ruleChoiceIndex) || flag;
				}
			}
			if (flag)
			{
				PreGameController.instance.RecalculateModifierAvailability();
			}
		}
		PreGameRuleVoteController.onVotesUpdated?.Invoke();
	}

	private void Awake()
	{
		instancesList.Add(this);
	}

	private void OnDestroy()
	{
		shouldUpdateGameVotes = true;
		instancesList.Remove(this);
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = 3u;
		}
		writer.Write((byte)num);
		bool num2 = (num & 1) != 0;
		bool flag = (num & 2) != 0;
		if (num2)
		{
			writer.Write(networkUserNetworkIdentity);
		}
		if (flag)
		{
			WriteVotes(writer);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		byte num = reader.ReadByte();
		bool flag = (num & 1) != 0;
		bool num2 = (num & 2) != 0;
		if (flag)
		{
			networkUserNetworkIdentity = reader.ReadNetworkIdentity();
			networkUser = (networkUserNetworkIdentity ? networkUserNetworkIdentity.GetComponent<NetworkUser>() : null);
		}
		if (num2)
		{
			ReadVotes(reader);
		}
	}

	private RuleChoiceDef GetDefaultChoice(RuleDef ruleDef)
	{
		return ruleDef.choices[PreGameController.instance.readOnlyRuleBook.GetRuleChoiceIndex(ruleDef)];
	}

	private void SetVotesFromRuleBookForSinglePlayer()
	{
		for (int i = 0; i < votes.Length; i++)
		{
			RuleDef ruleDef = RuleCatalog.GetRuleDef(i);
			votes[i].choiceValue = GetDefaultChoice(ruleDef).localIndex;
		}
		SetDirtyBit(2u);
	}

	private void WriteVotes(NetworkWriter writer)
	{
		int i = 0;
		for (int ruleCount = RuleCatalog.ruleCount; i < ruleCount; i++)
		{
			ruleMaskBuffer[i] = votes[i].hasVoted;
		}
		writer.Write(ruleMaskBuffer);
		int j = 0;
		for (int ruleCount2 = RuleCatalog.ruleCount; j < ruleCount2; j++)
		{
			if (votes[j].hasVoted)
			{
				Vote.Serialize(writer, votes[j]);
			}
		}
	}

	private void ReadVotes(NetworkReader reader)
	{
		reader.ReadRuleMask(ruleMaskBuffer);
		bool flag = !networkUserNetworkIdentity || !networkUserNetworkIdentity.isLocalPlayer;
		int i = 0;
		for (int ruleCount = RuleCatalog.ruleCount; i < ruleCount; i++)
		{
			Vote vote = ((!ruleMaskBuffer[i]) ? default(Vote) : Vote.Deserialize(reader));
			if (flag)
			{
				votes[i] = vote;
			}
		}
		shouldUpdateGameVotes |= flag;
		if (NetworkServer.active)
		{
			SetDirtyBit(2u);
		}
	}

	public bool IsChoiceVoted(RuleChoiceDef ruleChoiceDef)
	{
		return votes[ruleChoiceDef.ruleDef.globalIndex].choiceValue == ruleChoiceDef.localIndex;
	}

	static PreGameRuleVoteController()
	{
		instancesList = new List<PreGameRuleVoteController>();
		PreGameController.onServerRecalculatedModifierAvailability += delegate
		{
			UpdateGameVotes();
		};
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
