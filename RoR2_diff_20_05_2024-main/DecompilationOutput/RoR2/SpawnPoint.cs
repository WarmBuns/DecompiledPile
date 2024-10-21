using System.Collections.Generic;
using System.Collections.ObjectModel;
using HG;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace RoR2;

public class SpawnPoint : MonoBehaviour
{
	private static List<SpawnPoint> instancesList = new List<SpawnPoint>();

	private static ReadOnlyCollection<SpawnPoint> _readOnlyInstancesList = new ReadOnlyCollection<SpawnPoint>(instancesList);

	[Tooltip("Flagged when a player spawns on this position, to stop overlapping spawn positions")]
	public bool consumed;

	private static GameObject prefab;

	private static Mesh commandoMesh;

	private static bool attemptedCommandoMeshLoad = false;

	public static ReadOnlyCollection<SpawnPoint> readOnlyInstancesList => _readOnlyInstancesList;

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	public static SpawnPoint ConsumeSpawnPoint()
	{
		if (instancesList.Count == 0)
		{
			return null;
		}
		SpawnPoint spawnPoint = null;
		for (int i = 0; i < readOnlyInstancesList.Count; i++)
		{
			if (!readOnlyInstancesList[i].consumed)
			{
				spawnPoint = readOnlyInstancesList[i];
				readOnlyInstancesList[i].consumed = true;
				break;
			}
		}
		if (!spawnPoint)
		{
			for (int j = 0; j < readOnlyInstancesList.Count; j++)
			{
				readOnlyInstancesList[j].consumed = false;
			}
			spawnPoint = readOnlyInstancesList[0];
		}
		return spawnPoint;
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private static Mesh GetCommandoMesh()
	{
		if (!commandoMesh)
		{
			LoadCommandoMesh();
		}
		return commandoMesh;
	}

	private static void LoadCommandoMesh()
	{
		if (attemptedCommandoMeshLoad)
		{
			return;
		}
		attemptedCommandoMeshLoad = true;
		GameObject gameObject = null;
		gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody");
		if (!gameObject)
		{
			return;
		}
		CharacterModel component = gameObject.GetComponent<ModelLocator>().modelTransform.GetComponent<CharacterModel>();
		SkinnedMeshRenderer skinnedMeshRenderer = null;
		for (int i = 0; i < component.baseRendererInfos.Length; i++)
		{
			skinnedMeshRenderer = component.baseRendererInfos[i].renderer as SkinnedMeshRenderer;
			if ((bool)skinnedMeshRenderer)
			{
				break;
			}
		}
		commandoMesh = skinnedMeshRenderer.sharedMesh;
	}

	[InitDuringStartup]
	private static void Init()
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LegacyResourcesAPI.LoadAsync<GameObject>("Prefabs/SpawnPoint");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> x)
		{
			prefab = x.Result;
		};
	}

	public static void AddSpawnPoint(NodeGraph nodeGraph, NodeGraph.NodeIndex nodeIndex, Xoroshiro128Plus rng)
	{
		nodeGraph.GetNodePosition(nodeIndex, out var position);
		List<NodeGraph.LinkIndex> list = CollectionPool<NodeGraph.LinkIndex, List<NodeGraph.LinkIndex>>.RentCollection();
		nodeGraph.GetActiveNodeLinks(nodeIndex, list);
		Quaternion rotation;
		if (list.Count > 0)
		{
			NodeGraph.LinkIndex linkIndex = rng.NextElementUniform(list);
			nodeGraph.GetNodePosition(nodeGraph.GetLinkEndNode(linkIndex), out var position2);
			rotation = Util.QuaternionSafeLookRotation(position2 - position);
		}
		else
		{
			rotation = Quaternion.Euler(0f, rng.nextNormalizedFloat * 360f, 0f);
		}
		AddSpawnPoint(position, rotation);
		list = CollectionPool<NodeGraph.LinkIndex, List<NodeGraph.LinkIndex>>.ReturnCollection(list);
	}

	public static void AddSpawnPoint(Vector3 position, Quaternion rotation)
	{
		Object.Instantiate(prefab, position, rotation);
	}

	public void OnDrawGizmos()
	{
		Gizmos.matrix = base.transform.localToWorldMatrix;
		Gizmos.color = Color.green;
		Gizmos.DrawWireMesh(GetCommandoMesh());
	}
}
