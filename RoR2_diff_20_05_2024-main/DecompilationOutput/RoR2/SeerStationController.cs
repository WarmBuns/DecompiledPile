using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SeerStationController : NetworkBehaviour
{
	[SyncVar(hook = "SetTargetSceneDefIndex")]
	private int targetSceneDefIndex = -1;

	public SceneExitController explicitTargetSceneExitController;

	public bool fallBackToFirstActiveExitController;

	public Renderer targetRenderer;

	public int materialIndexToAssign;

	private static List<Material> sharedSharedMaterialsList = new List<Material>();

	public int NetworktargetSceneDefIndex
	{
		get
		{
			return targetSceneDefIndex;
		}
		[param: In]
		set
		{
			if (NetworkServer.localClientActive && !base.syncVarHookGuard)
			{
				base.syncVarHookGuard = true;
				SetTargetSceneDefIndex(value);
				base.syncVarHookGuard = false;
			}
			SetSyncVar(value, ref targetSceneDefIndex, 1u);
		}
	}

	private void SetTargetSceneDefIndex(int newTargetSceneDefIndex)
	{
		NetworktargetSceneDefIndex = newTargetSceneDefIndex;
		OnTargetSceneChanged(SceneCatalog.GetSceneDef((SceneIndex)targetSceneDefIndex));
	}

	[Server]
	public void SetTargetScene(SceneDef sceneDef)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SeerStationController::SetTargetScene(RoR2.SceneDef)' called on client");
		}
		else
		{
			NetworktargetSceneDefIndex = (int)sceneDef.sceneDefIndex;
		}
	}

	public int ReturnTargetSceneDefIndex()
	{
		return targetSceneDefIndex;
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		SceneDef targetScene = null;
		if ((uint)targetSceneDefIndex < SceneCatalog.sceneDefCount)
		{
			targetScene = SceneCatalog.GetSceneDef((SceneIndex)targetSceneDefIndex);
		}
		OnTargetSceneChanged(targetScene);
	}

	private void OnTargetSceneChanged(SceneDef targetScene)
	{
		Material portalMaterial = null;
		if ((bool)targetScene)
		{
			portalMaterial = targetScene.portalMaterial;
		}
		SetPortalMaterial(portalMaterial);
	}

	private void SetPortalMaterial(Material portalMaterial)
	{
		targetRenderer.GetSharedMaterials(sharedSharedMaterialsList);
		sharedSharedMaterialsList[materialIndexToAssign] = portalMaterial;
		targetRenderer.SetSharedMaterials(sharedSharedMaterialsList);
		sharedSharedMaterialsList.Clear();
	}

	[Server]
	public void SetRunNextStageToTarget()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SeerStationController::SetRunNextStageToTarget()' called on client");
			return;
		}
		SceneDef sceneDef = SceneCatalog.GetSceneDef((SceneIndex)targetSceneDefIndex);
		if ((bool)sceneDef)
		{
			SceneExitController sceneExitController = explicitTargetSceneExitController;
			if (!sceneExitController && fallBackToFirstActiveExitController)
			{
				sceneExitController = InstanceTracker.FirstOrNull<SceneExitController>();
			}
			if ((bool)sceneExitController)
			{
				sceneExitController.destinationScene = sceneDef;
				sceneExitController.useRunNextStageScene = false;
				Chat.SendBroadcastChat(new Chat.SimpleChatMessage
				{
					baseToken = sceneDef.portalSelectionMessageString
				});
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.WritePackedUInt32((uint)targetSceneDefIndex);
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
			writer.WritePackedUInt32((uint)targetSceneDefIndex);
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
			targetSceneDefIndex = (int)reader.ReadPackedUInt32();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			SetTargetSceneDefIndex((int)reader.ReadPackedUInt32());
		}
	}

	public override void PreStartClient()
	{
	}
}
