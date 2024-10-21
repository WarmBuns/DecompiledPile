using System;
using System.Collections.Generic;
using HG;
using RoR2.Navigation;
using UnityEngine;
using UnityEngine.Serialization;

namespace RoR2;

public class SceneInfo : MonoBehaviour
{
	private static class NodeGraphOverlay
	{
		private static Dictionary<(MapNodeGroup.GraphType graphType, HullMask hullMask), (DebugOverlay.MeshDrawer drawer, Action updater)> drawers;

		private static void StaticUpdate()
		{
			foreach (KeyValuePair<(MapNodeGroup.GraphType, HullMask), (DebugOverlay.MeshDrawer, Action)> drawer in drawers)
			{
				drawer.Value.Item2();
			}
		}

		private static void SetGraphDrawEnabled(bool shouldDraw, MapNodeGroup.GraphType graphType, HullMask hullMask)
		{
			(MapNodeGroup.GraphType, HullMask) key = (graphType, hullMask);
			DebugOverlay.MeshDrawer drawer;
			GenericMemoizer<(DebugOverlay.MeshDrawer, SceneInfo, MapNodeGroup.GraphType, HullMask), Mesh> getNodeGraphMeshMemoizer;
			if (shouldDraw)
			{
				if (drawers == null)
				{
					drawers = new Dictionary<(MapNodeGroup.GraphType, HullMask), (DebugOverlay.MeshDrawer, Action)>();
					RoR2Application.onUpdate += StaticUpdate;
				}
				if (!drawers.ContainsKey(key))
				{
					drawer = DebugOverlay.GetMeshDrawer();
					drawer.hasMeshOwnership = true;
					drawer.material = DebugOverlay.defaultWireMaterial;
					getNodeGraphMeshMemoizer = new GenericMemoizer<(DebugOverlay.MeshDrawer, SceneInfo, MapNodeGroup.GraphType, HullMask), Mesh>(GetNodeGraphMesh);
					drawers.Add(key, (drawer, Updater));
				}
			}
			else if (drawers != null)
			{
				if (drawers.TryGetValue(key, out (DebugOverlay.MeshDrawer, Action) value))
				{
					value.Item1.Dispose();
					drawers.Remove(key);
				}
				if (drawers.Count == 0)
				{
					drawers = null;
					RoR2Application.onUpdate -= StaticUpdate;
				}
			}
			static Mesh GetNodeGraphMesh(in (DebugOverlay.MeshDrawer, SceneInfo sceneInfo, MapNodeGroup.GraphType graphType, HullMask hullMask) input)
			{
				return input.sceneInfo?.GetNodeGraph(input.graphType).GenerateLinkDebugMesh(input.hullMask);
			}
			void Updater()
			{
				drawer.mesh = getNodeGraphMeshMemoizer.Evaluate((drawer, instance, graphType, hullMask));
			}
		}

		[ConCommand(commandName = "debug_scene_draw_nodegraph", flags = ConVarFlags.Cheat, helpText = "Enables/disables overlay drawing of the specified nodegraph. Format: {shouldDraw} {graphType} {hullClassification, ...}")]
		private static void CCDebugSceneDrawNodegraph(ConCommandArgs args)
		{
			bool argBool = args.GetArgBool(0);
			MapNodeGroup.GraphType argEnum = args.GetArgEnum<MapNodeGroup.GraphType>(1);
			HullMask hullMask = (HullMask)(1 << (int)args.GetArgEnum<HullClassification>(2));
			if (hullMask == HullMask.None)
			{
				throw new ConCommandException("Cannot use HullMask.None.");
			}
			for (int i = 3; i < args.Count; i++)
			{
				HullClassification? hullClassification = args.TryGetArgEnum<HullClassification>(i);
				if (hullClassification.HasValue)
				{
					hullMask = (HullMask)((int)hullMask | (1 << (int)hullClassification.Value));
				}
			}
			SetGraphDrawEnabled(argBool, argEnum, hullMask);
		}
	}

	private static SceneInfo _instance;

	[FormerlySerializedAs("groundNodes")]
	public MapNodeGroup groundNodeGroup;

	[FormerlySerializedAs("airNodes")]
	public MapNodeGroup airNodeGroup;

	[FormerlySerializedAs("railNodes")]
	public MapNodeGroup railNodeGroup;

	public MeshRenderer approximateMapBoundMesh;

	[SerializeField]
	private NodeGraph groundNodesAsset;

	[SerializeField]
	private NodeGraph airNodesAsset;

	public static SceneInfo instance => _instance;

	public NodeGraph groundNodes { get; private set; }

	public NodeGraph airNodes { get; private set; }

	public NodeGraph railNodes { get; private set; }

	public SceneDef sceneDef { get; private set; }

	public bool countsAsStage
	{
		get
		{
			if (!sceneDef)
			{
				return false;
			}
			if (sceneDef.sceneType != SceneType.Stage)
			{
				return sceneDef.sceneType == SceneType.UntimedStage;
			}
			return true;
		}
	}

	private void Awake()
	{
		if ((bool)groundNodesAsset)
		{
			groundNodes = UnityEngine.Object.Instantiate(groundNodesAsset);
		}
		else
		{
			Debug.LogWarning(base.gameObject.scene.name + " has no groundNodesAsset");
		}
		if ((bool)airNodesAsset)
		{
			airNodes = UnityEngine.Object.Instantiate(airNodesAsset);
		}
		else
		{
			Debug.LogWarning(base.gameObject.scene.name + " has no airNodesAsset");
		}
		sceneDef = SceneCatalog.GetSceneDefFromSceneName(base.gameObject.scene.name);
	}

	private void Start()
	{
	}

	private void RemoveCollisionFromParticleSystems()
	{
		ParticleSystem[] array = UnityEngine.Object.FindObjectsOfType<ParticleSystem>();
		int num = 0;
		ParticleSystem[] array2 = array;
		for (int i = 0; i < array2.Length; i++)
		{
			ParticleSystem.CollisionModule collision = array2[i].collision;
			if (collision.enabled)
			{
				num++;
			}
			collision.enabled = false;
		}
	}

	private void OnDestroy()
	{
		UnityEngine.Object.Destroy(groundNodes);
		UnityEngine.Object.Destroy(airNodes);
		UnityEngine.Object.Destroy(railNodes);
	}

	public MapNodeGroup GetNodeGroup(MapNodeGroup.GraphType nodeGraphType)
	{
		return nodeGraphType switch
		{
			MapNodeGroup.GraphType.Ground => groundNodeGroup, 
			MapNodeGroup.GraphType.Air => airNodeGroup, 
			MapNodeGroup.GraphType.Rail => railNodeGroup, 
			_ => null, 
		};
	}

	public NodeGraph GetNodeGraph(MapNodeGroup.GraphType nodeGraphType)
	{
		return nodeGraphType switch
		{
			MapNodeGroup.GraphType.Ground => groundNodes, 
			MapNodeGroup.GraphType.Air => airNodes, 
			MapNodeGroup.GraphType.Rail => railNodes, 
			_ => null, 
		};
	}

	public void SetGateState(string gateName, bool gateEnabled)
	{
		bool flag = false;
		if ((bool)groundNodes)
		{
			flag = groundNodes.TrySetGateState(gateName, gateEnabled) || flag;
		}
		if ((bool)airNodes)
		{
			flag = airNodes.TrySetGateState(gateName, gateEnabled) || flag;
		}
		if ((bool)railNodes)
		{
			flag = railNodes.TrySetGateState(gateName, gateEnabled) || flag;
		}
		if (!flag)
		{
			Debug.LogError($"Unable to set gate state for {base.gameObject.scene.name}: {gateName}={gateEnabled}");
		}
	}

	private void OnEnable()
	{
		if (!_instance)
		{
			_instance = this;
		}
	}

	private void OnDisable()
	{
		if (_instance == this)
		{
			_instance = null;
		}
	}

	private void OnValidate()
	{
		if ((bool)groundNodeGroup)
		{
			groundNodesAsset = groundNodeGroup.nodeGraph;
		}
		if ((bool)airNodeGroup)
		{
			airNodesAsset = airNodeGroup.nodeGraph;
		}
	}
}
