using System;
using System.Collections.Generic;
using HG;
using JetBrains.Annotations;
using RoR2.UI;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(VoteController))]
public class GameOverController : NetworkBehaviour
{
	[Tooltip("The prefab to use for the end-of-game report panel.")]
	public GameObject gameEndReportPanelPrefab;

	public float appearanceDelay = 1f;

	private VoteController voteController;

	private bool _shouldDisplayGameEndReportPanels;

	private Dictionary<HUD, GameEndReportPanelController> reportPanels = new Dictionary<HUD, GameEndReportPanelController>();

	private const uint runReportDirtyBit = 1u;

	private const uint allDirtyBits = 1u;

	private static int kRpcRpcClientGameOver;

	public static GameOverController instance { get; private set; }

	public bool shouldDisplayGameEndReportPanels
	{
		get
		{
			return _shouldDisplayGameEndReportPanels;
		}
		set
		{
			_shouldDisplayGameEndReportPanels = value;
			if (_shouldDisplayGameEndReportPanels)
			{
				UpdateReportScreens();
			}
			else
			{
				CleanupReportPanels();
			}
		}
	}

	public RunReport runReport { get; private set; }

	[Server]
	public void SetRunReport([NotNull] RunReport newRunReport)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.GameOverController::SetRunReport(RoR2.RunReport)' called on client");
			return;
		}
		SetDirtyBit(1u);
		runReport = newRunReport;
		if ((bool)runReport.gameEnding)
		{
			EntityStateMachine.FindByCustomName(base.gameObject, "Main").initialStateType = runReport.gameEnding.gameOverControllerState;
		}
	}

	private int FindPlayerIndex(LocalUser localUser)
	{
		int i = 0;
		for (int playerInfoCount = runReport.playerInfoCount; i < playerInfoCount; i++)
		{
			if (runReport.GetPlayerInfo(i).localUser == localUser)
			{
				return i;
			}
		}
		return 0;
	}

	private void UpdateReportScreens()
	{
		if (!shouldDisplayGameEndReportPanels)
		{
			return;
		}
		int i = 0;
		for (int count = HUD.readOnlyInstanceList.Count; i < count; i++)
		{
			HUD hUD = HUD.readOnlyInstanceList[i];
			if (!reportPanels.ContainsKey(hUD))
			{
				reportPanels[hUD] = GenerateReportScreen(hUD);
			}
		}
	}

	private void CleanupReportPanels()
	{
		List<HUD> list = CollectionPool<HUD, List<HUD>>.RentCollection();
		foreach (HUD key in reportPanels.Keys)
		{
			if (!key || !shouldDisplayGameEndReportPanels)
			{
				list.Add(key);
			}
		}
		int i = 0;
		for (int count = list.Count; i < count; i++)
		{
			reportPanels.Remove(list[i]);
		}
		CollectionPool<HUD, List<HUD>>.ReturnCollection(list);
	}

	private GameEndReportPanelController GenerateReportScreen(HUD hud)
	{
		LocalUser localUser = hud.localUserViewer;
		GameObject obj = UnityEngine.Object.Instantiate(gameEndReportPanelPrefab, hud.transform);
		obj.transform.parent = hud.transform;
		obj.GetComponent<MPEventSystemProvider>().eventSystem = localUser.eventSystem;
		GameEndReportPanelController component = obj.GetComponent<GameEndReportPanelController>();
		runReport.SortByLocalPlayers();
		GameEndReportPanelController.DisplayData displayData = default(GameEndReportPanelController.DisplayData);
		displayData.runReport = runReport;
		displayData.playerIndex = FindPlayerIndex(localUser);
		GameEndReportPanelController.DisplayData displayData2 = displayData;
		component.SetDisplayData(displayData2);
		component.SetContinueButtonAction(delegate
		{
			if ((bool)localUser.currentNetworkUser)
			{
				localUser.currentNetworkUser.CallCmdSubmitVote(voteController.gameObject, 0);
			}
		});
		GameObject obj2 = UnityEngine.Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/VoteInfoPanel"), (RectTransform)component.continueButton.transform.parent);
		obj2.transform.SetAsFirstSibling();
		obj2.GetComponent<VoteInfoPanelController>().voteController = voteController;
		return component;
	}

	private void Awake()
	{
		runReport = new RunReport();
		voteController = GetComponent<VoteController>();
	}

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		CameraRigController.OnHUDUpdated = (Action)Delegate.Combine(CameraRigController.OnHUDUpdated, new Action(UpdateReportScreens));
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
		CameraRigController.OnHUDUpdated = (Action)Delegate.Remove(CameraRigController.OnHUDUpdated, new Action(UpdateReportScreens));
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = base.syncVarDirtyBits;
		if (initialState)
		{
			num = 1u;
		}
		bool num2 = (num & 1) != 0;
		if (!initialState)
		{
			writer.Write((byte)num);
		}
		if (num2)
		{
			runReport.Write(writer);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (((uint)(initialState ? 1 : reader.ReadByte()) & (true ? 1u : 0u)) != 0)
		{
			runReport.Read(reader);
		}
	}

	[ClientRpc]
	public void RpcClientGameOver()
	{
		if ((bool)Run.instance)
		{
			Run.instance.OnClientGameOver(runReport);
		}
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeRpcRpcClientGameOver(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcClientGameOver called on server.");
		}
		else
		{
			((GameOverController)obj).RpcClientGameOver();
		}
	}

	public void CallRpcClientGameOver()
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("RPC Function RpcClientGameOver called on client.");
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)2);
		networkWriter.WritePackedUInt32((uint)kRpcRpcClientGameOver);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		SendRPCInternal(networkWriter, 0, "RpcClientGameOver");
	}

	static GameOverController()
	{
		kRpcRpcClientGameOver = 1518660169;
		NetworkBehaviour.RegisterRpcDelegate(typeof(GameOverController), kRpcRpcClientGameOver, InvokeRpcRpcClientGameOver);
		NetworkCRC.RegisterBehaviour("GameOverController", 0);
	}

	public override void PreStartClient()
	{
	}
}
