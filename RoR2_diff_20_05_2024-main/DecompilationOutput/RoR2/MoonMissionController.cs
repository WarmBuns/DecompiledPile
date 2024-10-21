using System;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MoonMissionController : NetworkBehaviour
{
	private Xoroshiro128Plus rng;

	public static MoonMissionController instance { get; private set; }

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
		SceneDirector.onPreGeneratePlayerSpawnPointsServer += OnPreGeneratePlayerSpawnPointsServer;
	}

	private void OnDisable()
	{
		SceneDirector.onPreGeneratePlayerSpawnPointsServer -= OnPreGeneratePlayerSpawnPointsServer;
		instance = SingletonHelper.Unassign(instance, this);
	}

	[Server]
	public override void OnStartServer()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.MoonMissionController::OnStartServer()' called on client");
			return;
		}
		base.OnStartServer();
		rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUint);
	}

	private void OnPreGeneratePlayerSpawnPointsServer(SceneDirector sceneDirector, ref Action generationMethod)
	{
		generationMethod = GeneratePlayerSpawnPointsServer;
	}

	private void GeneratePlayerSpawnPointsServer()
	{
		if (!SceneInfo.instance)
		{
			return;
		}
		ChildLocator component = SceneInfo.instance.GetComponent<ChildLocator>();
		if (!component)
		{
			return;
		}
		Transform transform = component.FindChild("PlayerSpawnOrigin");
		Vector3 position = transform.position;
		NodeGraph groundNodes = SceneInfo.instance.groundNodes;
		if (!groundNodes)
		{
			Debug.LogError("MoonMissionController.GeneratePlayerSpawnPointsServer: No ground nodegraph found to place spawn points.", this);
			return;
		}
		NodeGraphSpider nodeGraphSpider = new NodeGraphSpider(groundNodes, HullMask.Human);
		nodeGraphSpider.AddNodeForNextStep(groundNodes.FindClosestNode(position, HullClassification.Human));
		for (int i = 0; i < 4; i++)
		{
			nodeGraphSpider.PerformStep();
			if (nodeGraphSpider.collectedSteps.Count > 16)
			{
				break;
			}
		}
		for (int j = 0; j < nodeGraphSpider.collectedSteps.Count; j++)
		{
			NodeGraphSpider.StepInfo stepInfo = nodeGraphSpider.collectedSteps[j];
			groundNodes.GetNodePosition(stepInfo.node, out var position2);
			Quaternion rotation = transform.rotation;
			SpawnPoint.AddSpawnPoint(position2, rotation);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
