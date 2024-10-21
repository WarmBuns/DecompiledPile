using System;
using UnityEngine;

namespace RoR2;

public class DebugOverlay : MonoBehaviour
{
	public class MeshDrawer : IDisposable
	{
		private GameObject gameObject;

		private MeshFilter meshFilter;

		private MeshRenderer meshRenderer;

		public bool hasMeshOwnership;

		public Transform transform { get; private set; }

		public bool enabled
		{
			get
			{
				if ((bool)gameObject)
				{
					return gameObject.activeSelf;
				}
				return false;
			}
			set
			{
				if ((bool)gameObject)
				{
					gameObject.SetActive(value);
				}
			}
		}

		public Mesh mesh
		{
			get
			{
				return meshFilter.sharedMesh;
			}
			set
			{
				if (!(meshFilter.sharedMesh == value))
				{
					if ((bool)meshFilter.sharedMesh && hasMeshOwnership)
					{
						UnityEngine.Object.Destroy(meshFilter.sharedMesh);
					}
					meshFilter.sharedMesh = value;
				}
			}
		}

		public Material material
		{
			get
			{
				return meshRenderer.sharedMaterial;
			}
			set
			{
				meshRenderer.sharedMaterial = value;
			}
		}

		public MeshDrawer(Transform parent)
		{
			gameObject = new GameObject("MeshDrawer");
			transform = gameObject.transform;
			transform.SetParent(parent);
			meshFilter = gameObject.AddComponent<MeshFilter>();
			meshRenderer = gameObject.AddComponent<MeshRenderer>();
			material = defaultWireMaterial;
		}

		public void Dispose()
		{
			mesh = null;
			UnityEngine.Object.Destroy(gameObject);
			gameObject = null;
			transform = null;
			meshFilter = null;
			meshRenderer = null;
		}
	}

	private static DebugOverlay instance;

	private new Transform transform;

	public static Material defaultWireMaterial;

	private void Awake()
	{
		transform = base.transform;
	}

	[InitDuringStartup]
	private static void Init()
	{
		LegacyResourcesAPI.LoadAsyncCallback("Materials/UI/matDebugUI", delegate(Material operationResult)
		{
			defaultWireMaterial = operationResult;
		});
		GameObject obj = new GameObject("DebugOverlay");
		instance = obj.AddComponent<DebugOverlay>();
		UnityEngine.Object.DontDestroyOnLoad(obj);
	}

	public static MeshDrawer GetMeshDrawer()
	{
		return new MeshDrawer(instance.transform);
	}
}
