using System.Collections.Generic;
using RoR2.Networking;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SceneObjectToggleGroup : NetworkBehaviour
{
	private struct ToggleGroupRange
	{
		public int start;

		public int count;

		public int minEnabled;

		public int maxEnabled;
	}

	public GameObjectToggleGroup[] toggleGroups;

	private const byte enabledObjectsDirtyBit = 1;

	private const byte initialStateMask = 1;

	private static readonly Queue<SceneObjectToggleGroup> activationsQueue;

	private GameObject[] allToggleableObjects;

	private bool[] activations;

	private ToggleGroupRange[] internalToggleGroups;

	static SceneObjectToggleGroup()
	{
		activationsQueue = new Queue<SceneObjectToggleGroup>();
		NetworkManagerSystem.onServerSceneChangedGlobal += OnServerSceneChanged;
	}

	private static void OnServerSceneChanged(string sceneName)
	{
		while (activationsQueue.Count > 0)
		{
			SceneObjectToggleGroup sceneObjectToggleGroup = activationsQueue.Dequeue();
			if ((bool)sceneObjectToggleGroup)
			{
				sceneObjectToggleGroup.ApplyActivations();
			}
		}
	}

	private void Awake()
	{
		activationsQueue.Enqueue(this);
		int num = 0;
		for (int i = 0; i < toggleGroups.Length; i++)
		{
			num += toggleGroups[i].objects.Length;
		}
		allToggleableObjects = new GameObject[num];
		activations = new bool[num];
		internalToggleGroups = new ToggleGroupRange[toggleGroups.Length];
		int start = 0;
		for (int j = 0; j < toggleGroups.Length; j++)
		{
			GameObject[] objects = toggleGroups[j].objects;
			ToggleGroupRange toggleGroupRange = default(ToggleGroupRange);
			toggleGroupRange.start = start;
			toggleGroupRange.count = objects.Length;
			toggleGroupRange.minEnabled = toggleGroups[j].minEnabled;
			toggleGroupRange.maxEnabled = toggleGroups[j].maxEnabled;
			internalToggleGroups[j] = toggleGroupRange;
			GameObject[] array = objects;
			foreach (GameObject gameObject in array)
			{
				allToggleableObjects[start++] = gameObject;
			}
		}
		if (NetworkServer.active)
		{
			Generate();
		}
	}

	public override void OnStartClient()
	{
		base.OnStartClient();
		if (!NetworkServer.active)
		{
			ApplyActivations();
		}
	}

	[Server]
	private void Generate()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void RoR2.SceneObjectToggleGroup::Generate()' called on client");
			return;
		}
		for (int i = 0; i < internalToggleGroups.Length; i++)
		{
			ToggleGroupRange toggleGroupRange = internalToggleGroups[i];
			int num = Run.instance.stageRng.RangeInt(toggleGroupRange.minEnabled, toggleGroupRange.maxEnabled + 1);
			List<int> list = RangeList(toggleGroupRange.start, toggleGroupRange.count);
			Util.ShuffleList(list, Run.instance.stageRng);
			for (int num2 = num - 1; num2 >= 0; num2--)
			{
				activations[list[num2]] = true;
				list.RemoveAt(num2);
			}
			for (int j = 0; j < list.Count; j++)
			{
				activations[list[j]] = false;
			}
		}
		SetDirtyBit(1u);
		static List<int> RangeList(int start, int count)
		{
			List<int> list2 = new List<int>(count);
			int k = start;
			for (int num3 = start + count; k < num3; k++)
			{
				list2.Add(k);
			}
			return list2;
		}
	}

	public override bool OnSerialize(NetworkWriter writer, bool initialState)
	{
		uint num = (initialState ? 1u : base.syncVarDirtyBits);
		writer.Write((byte)num);
		if ((num & (true ? 1u : 0u)) != 0)
		{
			writer.WriteBitArray(activations);
		}
		if (!initialState)
		{
			return num != 0;
		}
		return false;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (((uint)reader.ReadByte() & (true ? 1u : 0u)) != 0)
		{
			reader.ReadBitArray(activations);
		}
		ApplyActivations();
	}

	private void ApplyActivations()
	{
		for (int i = 0; i < allToggleableObjects.Length; i++)
		{
			GameObject gameObject = allToggleableObjects[i];
			if ((bool)gameObject)
			{
				gameObject.SetActive(activations[i]);
			}
		}
	}

	private void UNetVersion()
	{
	}

	public override void PreStartClient()
	{
	}
}
