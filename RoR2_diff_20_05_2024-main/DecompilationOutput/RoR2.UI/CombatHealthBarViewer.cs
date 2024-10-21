using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class CombatHealthBarViewer : MonoBehaviour, ILayoutGroup, ILayoutController
{
	[Serializable]
	public class HealthBarInfo
	{
		public HealthComponent sourceHealthComponent;

		public Transform sourceTransform;

		public GameObject healthBarRootObject;

		public Transform healthBarRootObjectTransform;

		public HealthBar healthBar;

		public BuffDisplay buffdisplay;

		public float verticalOffset;

		public float endTime = float.NegativeInfinity;

		public int poolIndex;

		public bool frameDelay;

		private static Vector3 hiddenPosition = new Vector3(-9999f, -9999f, -9999f);

		private bool active = true;

		public bool SetInactive()
		{
			if (active)
			{
				healthBarRootObjectTransform.position = hiddenPosition;
				healthBar.enabled = false;
				active = false;
				return true;
			}
			return false;
		}

		public void SetActive()
		{
			if (!active)
			{
				frameDelay = true;
				healthBar.enabled = true;
				healthBar.gameObject.SetActive(value: true);
				buffdisplay.UpdateLayout();
				active = true;
			}
		}
	}

	private static readonly List<CombatHealthBarViewer> instancesList;

	public RectTransform container;

	public GameObject healthBarPrefab;

	public float healthBarDuration;

	private const float hoverHealthBarDuration = 1f;

	private RectTransform rectTransform;

	private Canvas canvas;

	private UICamera uiCamera;

	private List<HealthComponent> trackedVictims = new List<HealthComponent>();

	private Dictionary<HealthComponent, HealthBarInfo> victimToHealthBarInfo = new Dictionary<HealthComponent, HealthBarInfo>();

	public float zPosition;

	private const float overheadOffset = 1f;

	public static bool doingUIRebuild;

	public HealthComponent crosshairTarget { get; set; }

	public GameObject viewerBodyObject { get; set; }

	public CharacterBody viewerBody { get; set; }

	public TeamIndex viewerTeamIndex { get; set; }

	private HealthBarInfo GetHealthBarInfo()
	{
		HealthBarInfo obj = new HealthBarInfo
		{
			healthBarRootObject = UnityEngine.Object.Instantiate(healthBarPrefab, container)
		};
		obj.healthBarRootObjectTransform = obj.healthBarRootObject.transform;
		obj.healthBar = obj.healthBarRootObject.GetComponent<HealthBar>();
		return obj;
	}

	private void InitializeHealthBarInfos()
	{
	}

	static CombatHealthBarViewer()
	{
		instancesList = new List<CombatHealthBarViewer>();
		doingUIRebuild = false;
		GlobalEventManager.onClientDamageNotified += delegate(DamageDealtMessage msg)
		{
			if ((bool)msg.victim && !msg.isSilent)
			{
				HealthComponent component = msg.victim.GetComponent<HealthComponent>();
				if ((bool)component && !component.dontShowHealthbar)
				{
					TeamIndex objectTeam = TeamComponent.GetObjectTeam(component.gameObject);
					foreach (CombatHealthBarViewer instances in instancesList)
					{
						if (msg.attacker == instances.viewerBodyObject && (bool)instances.viewerBodyObject)
						{
							instances.HandleDamage(component, objectTeam);
						}
					}
				}
			}
		};
	}

	private void Update()
	{
		SetDirty();
	}

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
		canvas = GetComponent<Canvas>();
		InitializeHealthBarInfos();
	}

	private void Start()
	{
		FindCamera();
	}

	private void FindCamera()
	{
		uiCamera = canvas.rootCanvas.worldCamera.GetComponent<UICamera>();
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
		for (int num = trackedVictims.Count - 1; num >= 0; num--)
		{
			Remove(num);
		}
	}

	private void Remove(int trackedVictimIndex)
	{
		Remove(trackedVictimIndex, victimToHealthBarInfo[trackedVictims[trackedVictimIndex]]);
	}

	private void Remove(int trackedVictimIndex, HealthBarInfo healthBarInfo)
	{
		trackedVictims.RemoveAt(trackedVictimIndex);
		UnityEngine.Object.Destroy(healthBarInfo.healthBarRootObject);
		victimToHealthBarInfo.Remove(healthBarInfo.sourceHealthComponent);
	}

	private bool VictimIsValid(HealthComponent victim)
	{
		if ((bool)victim && victim.alive)
		{
			if (!(victimToHealthBarInfo[victim].endTime > Time.time))
			{
				return victim == crosshairTarget;
			}
			return true;
		}
		return false;
	}

	private void LateUpdate()
	{
		CleanUp();
	}

	private void CleanUp()
	{
		for (int num = trackedVictims.Count - 1; num >= 0; num--)
		{
			HealthComponent healthComponent = trackedVictims[num];
			if (!VictimIsValid(healthComponent))
			{
				Remove(num, victimToHealthBarInfo[healthComponent]);
			}
		}
	}

	private void UpdateAllHealthbarPositions(Camera sceneCam, Camera uiCam)
	{
		if (!sceneCam || !uiCam)
		{
			return;
		}
		foreach (HealthBarInfo value in victimToHealthBarInfo.Values)
		{
			if ((bool)value.sourceTransform && (bool)value.healthBarRootObjectTransform)
			{
				Vector3 position = value.sourceTransform.position;
				position.y += value.verticalOffset;
				Vector3 vector = sceneCam.WorldToScreenPoint(position);
				vector.z = ((vector.z > 0f) ? 1f : (-1f));
				vector = ((vector.z > 0f) ? vector : new Vector3(-9999f, -9999f, -1f));
				Vector3 position2 = uiCam.ScreenToWorldPoint(vector);
				value.healthBarRootObjectTransform.position = position2;
			}
		}
	}

	private void HandleDamage(HealthComponent victimHealthComponent, TeamIndex victimTeam)
	{
		if (viewerTeamIndex == victimTeam || victimTeam == TeamIndex.None)
		{
			return;
		}
		CharacterBody body = victimHealthComponent.body;
		if (!body || body.GetVisibilityLevel(viewerBody) >= VisibilityLevel.Revealed)
		{
			HealthBarInfo healthBarInfo = GetHealthBarInfo(victimHealthComponent);
			if (healthBarInfo != null)
			{
				healthBarInfo.endTime = Time.time + healthBarDuration;
			}
		}
	}

	private HealthBarInfo GetHealthBarInfo(HealthComponent victimHealthComponent)
	{
		if (!victimToHealthBarInfo.TryGetValue(victimHealthComponent, out var value))
		{
			value = GetHealthBarInfo();
			if (value == null)
			{
				return null;
			}
			value.healthBarRootObject.GetComponentInChildren<BuffDisplay>().SetSource(victimHealthComponent.body);
			value.healthBar.source = victimHealthComponent;
			value.healthBar.viewerBody = viewerBody;
			value.sourceHealthComponent = victimHealthComponent;
			value.verticalOffset = 0f;
			Collider component = victimHealthComponent.GetComponent<Collider>();
			if ((bool)component)
			{
				value.verticalOffset = component.bounds.extents.y;
			}
			value.sourceTransform = victimHealthComponent.body.coreTransform ?? victimHealthComponent.transform;
			ModelLocator component2 = victimHealthComponent.GetComponent<ModelLocator>();
			if ((bool)component2)
			{
				Transform modelTransform = component2.modelTransform;
				if ((bool)modelTransform)
				{
					ChildLocator component3 = modelTransform.GetComponent<ChildLocator>();
					if ((bool)component3)
					{
						Transform transform = component3.FindChild("HealthBarOrigin");
						if ((bool)transform)
						{
							value.sourceTransform = transform;
							value.verticalOffset = 0f;
						}
					}
				}
			}
			value.healthBar.InitializeHealthBar();
			victimToHealthBarInfo.Add(victimHealthComponent, value);
			trackedVictims.Add(victimHealthComponent);
		}
		return value;
	}

	private void SetDirty()
	{
		if (base.isActiveAndEnabled && !CanvasUpdateRegistry.IsRebuildingLayout())
		{
			LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		}
	}

	private static void LayoutForCamera(UICamera uiCamera)
	{
		Camera camera = uiCamera.camera;
		Camera sceneCam = uiCamera.cameraRigController.sceneCam;
		for (int i = 0; i < instancesList.Count; i++)
		{
			instancesList[i].UpdateAllHealthbarPositions(sceneCam, camera);
		}
	}

	public void SetLayoutHorizontal()
	{
		if ((bool)uiCamera)
		{
			doingUIRebuild = true;
			LayoutForCamera(uiCamera);
			doingUIRebuild = false;
		}
	}

	public void SetLayoutVertical()
	{
	}
}
