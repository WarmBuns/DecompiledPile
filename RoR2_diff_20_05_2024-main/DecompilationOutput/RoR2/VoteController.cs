using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using RoR2.Networking;
using Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace RoR2;

public class VoteController : NetworkBehaviour
{
	public enum TimerStartCondition
	{
		Immediate,
		OnAnyVoteReceived,
		WhileAnyVoteReceived,
		WhileAllVotesReceived,
		Never
	}

	[Tooltip("Custom name for this component to help describe its role.")]
	public string customName;

	[Tooltip("Whether or not users must be participating in the run to be allowed to vote.")]
	public bool onlyAllowParticipatingPlayers = true;

	[Tooltip("Whether or not to add new players to the voting pool when they connect.")]
	public bool addNewPlayers;

	[Tooltip("Whether or not users are allowed to change their choice after submitting it.")]
	public bool canChangeVote;

	[Tooltip("Whether or not users are allowed to revoke their vote entirely after submitting it.")]
	public bool canRevokeVote;

	[Tooltip("If set, the vote cannot be completed early by all users submitting, and the timeout must occur.")]
	public bool mustTimeOut;

	[Tooltip("Whether or not this vote must reset and be unvotable while someone is connecting or disconnecting.")]
	public bool resetOnConnectionsChanged;

	[Tooltip("How long it takes for the vote to forcibly complete once the timer begins.")]
	public float timeoutDuration = 15f;

	[Tooltip("How long it takes for action to be taken after the vote is complete.")]
	public float minimumTimeBeforeProcessing = 3f;

	[Tooltip("What causes the timer to start counting down.")]
	public TimerStartCondition timerStartCondition;

	[Tooltip("An array of functions to be called based on the user vote.")]
	public UnityEvent[] choices;

	[Tooltip("The choice to use when nobody votes or everybody who can vote quits.")]
	public int defaultChoiceIndex;

	[Tooltip("Whether or not to destroy the attached GameObject when the vote completes.")]
	public bool destroyGameObjectOnComplete = true;

	private SyncListUserVote votes = new SyncListUserVote();

	[SyncVar]
	public bool timerIsActive;

	[SyncVar]
	public float timer;

	private static int kListvotes;

	public bool NetworktimerIsActive
	{
		get
		{
			return timerIsActive;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref timerIsActive, 2u);
		}
	}

	public float Networktimer
	{
		get
		{
			return timer;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref timer, 4u);
		}
	}

	[Server]
	private void StartTimer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::StartTimer()' called on client");
		}
		else if (!timerIsActive)
		{
			NetworktimerIsActive = true;
			Networktimer = timeoutDuration;
		}
	}

	[Server]
	private void StopTimer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::StopTimer()' called on client");
			return;
		}
		NetworktimerIsActive = false;
		Networktimer = timeoutDuration;
	}

	[Server]
	private void InitializeVoters()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::InitializeVoters()' called on client");
			return;
		}
		StopTimer();
		if (votes.Count > 0)
		{
			votes.Clear();
		}
		IEnumerable<NetworkUser> source = NetworkUser.readOnlyInstancesList;
		if (onlyAllowParticipatingPlayers)
		{
			source = source.Where((NetworkUser v) => v.isParticipating);
		}
		foreach (GameObject item in source.Select((NetworkUser v) => v.gameObject))
		{
			votes.Add(new UserVote
			{
				networkUserObject = item,
				voteChoiceIndex = -1
			});
		}
	}

	[Server]
	private void AddUserToVoters(NetworkUser networkUser)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::AddUserToVoters(RoR2.NetworkUser)' called on client");
		}
		else if ((!onlyAllowParticipatingPlayers || networkUser.isParticipating) && !votes.Any((UserVote v) => v.networkUserObject == networkUser.gameObject))
		{
			votes.Add(new UserVote
			{
				networkUserObject = networkUser.gameObject,
				voteChoiceIndex = -1
			});
		}
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			if (timerStartCondition == TimerStartCondition.Immediate)
			{
				StartTimer();
			}
			if (addNewPlayers)
			{
				NetworkUser.OnPostNetworkUserStart += AddUserToVoters;
			}
			NetworkManagerSystem.onServerConnectGlobal += OnServerConnectGlobal;
			NetworkManagerSystem.onServerDisconnectGlobal += OnServerDisconnectGlobal;
		}
		votes.InitializeBehaviour(this, kListvotes);
	}

	private void OnServerConnectGlobal(NetworkConnection conn)
	{
		if (resetOnConnectionsChanged)
		{
			InitializeVoters();
		}
	}

	private void OnServerDisconnectGlobal(NetworkConnection conn)
	{
		if (resetOnConnectionsChanged)
		{
			InitializeVoters();
		}
	}

	private void OnDestroy()
	{
		NetworkUser.OnPostNetworkUserStart -= AddUserToVoters;
		NetworkManagerSystem.onServerConnectGlobal -= OnServerConnectGlobal;
		NetworkManagerSystem.onServerDisconnectGlobal -= OnServerDisconnectGlobal;
	}

	public override void OnStartServer()
	{
		base.OnStartServer();
		InitializeVoters();
	}

	[Server]
	public void ReceiveUserVote(NetworkUser networkUser, int voteChoiceIndex)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::ReceiveUserVote(RoR2.NetworkUser,System.Int32)' called on client");
			return;
		}
		if (resetOnConnectionsChanged)
		{
			int connectingClientCount = PlatformSystems.networkManager.GetConnectingClientCount();
			if (connectingClientCount > 0)
			{
				Debug.LogFormat("Vote from user \"{0}\" rejected: {1} clients are currently still in the process of connecting.", networkUser.userName, connectingClientCount);
				return;
			}
		}
		if ((voteChoiceIndex < 0 && !canRevokeVote) || voteChoiceIndex >= choices.Length)
		{
			return;
		}
		GameObject gameObject = networkUser.gameObject;
		for (int i = 0; i < votes.Count; i++)
		{
			if (gameObject == votes[i].networkUserObject)
			{
				if (votes[i].receivedVote && !canChangeVote)
				{
					break;
				}
				votes[i] = new UserVote
				{
					networkUserObject = gameObject,
					voteChoiceIndex = voteChoiceIndex
				};
			}
		}
	}

	private void Update()
	{
		if (NetworkServer.active)
		{
			ServerUpdate();
		}
	}

	[Server]
	private void ServerUpdate()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::ServerUpdate()' called on client");
			return;
		}
		if (timerIsActive)
		{
			Networktimer = timer - Time.deltaTime;
			if (timer < 0f)
			{
				Networktimer = 0f;
			}
		}
		int num = 0;
		for (int num2 = votes.Count - 1; num2 >= 0; num2--)
		{
			if (!votes[num2].networkUserObject)
			{
				votes.RemoveAt(num2);
			}
			else if (votes[num2].receivedVote)
			{
				num++;
			}
		}
		bool flag = num > 0;
		bool num3 = num == votes.Count;
		if (flag)
		{
			if (timerStartCondition == TimerStartCondition.OnAnyVoteReceived || timerStartCondition == TimerStartCondition.WhileAnyVoteReceived)
			{
				StartTimer();
			}
		}
		else if (timerStartCondition == TimerStartCondition.WhileAnyVoteReceived)
		{
			StopTimer();
		}
		if (num3)
		{
			if (timerStartCondition == TimerStartCondition.WhileAllVotesReceived)
			{
				StartTimer();
			}
			else if (RoR2Application.isInSinglePlayer)
			{
				Networktimer = 0f;
			}
			else
			{
				Networktimer = Mathf.Min(timer, minimumTimeBeforeProcessing);
			}
		}
		else if (timerStartCondition == TimerStartCondition.WhileAllVotesReceived)
		{
			StopTimer();
		}
		if ((num3 && !mustTimeOut) || (timerIsActive && timer <= 0f))
		{
			FinishVote();
		}
	}

	[Server]
	private void FinishVote()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.VoteController::FinishVote()' called on client");
			return;
		}
		int num = (from v in votes
			where v.receivedVote
			group v by v.voteChoiceIndex into v
			orderby v.Count() descending
			select v).FirstOrDefault()?.Key ?? defaultChoiceIndex;
		if (num >= choices.Length)
		{
			num = defaultChoiceIndex;
		}
		if (num < choices.Length)
		{
			choices[num].Invoke();
		}
		base.enabled = false;
		NetworktimerIsActive = false;
		Networktimer = 0f;
		if (destroyGameObjectOnComplete)
		{
			Object.Destroy(base.gameObject);
		}
	}

	public int GetVoteCount()
	{
		return votes.Count;
	}

	public UserVote GetVote(int i)
	{
		return votes[i];
	}

	public void SubmitVoteForAllLocalUsers(int choiceIndex)
	{
		foreach (NetworkUser readOnlyLocalPlayers in NetworkUser.readOnlyLocalPlayersList)
		{
			readOnlyLocalPlayers.CallCmdSubmitVote(base.gameObject, choiceIndex);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeSyncListvotes(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("SyncList votes called on server.");
		}
		else
		{
			((VoteController)obj).votes.HandleMsg(reader);
		}
	}

	static VoteController()
	{
		kListvotes = 458257089;
		NetworkBehaviour.RegisterSyncListDelegate(typeof(VoteController), kListvotes, InvokeSyncListvotes);
		NetworkCRC.RegisterBehaviour("VoteController", 0);
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteStructSyncListUserVote_None(writer, votes);
			writer.Write(timerIsActive);
			writer.Write(timer);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			GeneratedNetworkCode._WriteStructSyncListUserVote_None(writer, votes);
		}
		if ((base.syncVarDirtyBits & 2u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(timerIsActive);
		}
		if ((base.syncVarDirtyBits & 4u) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			writer.Write(timer);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			GeneratedNetworkCode._ReadStructSyncListUserVote_None(reader, votes);
			timerIsActive = reader.ReadBoolean();
			timer = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			GeneratedNetworkCode._ReadStructSyncListUserVote_None(reader, votes);
		}
		if (((uint)num & 2u) != 0)
		{
			timerIsActive = reader.ReadBoolean();
		}
		if (((uint)num & 4u) != 0)
		{
			timer = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
	}
}
