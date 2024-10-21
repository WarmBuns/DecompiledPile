using System.Collections.Generic;
using RoR2.WwiseUtils;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class CrosshairManager : MonoBehaviour
{
	public delegate void ShouldShowCrosshairDelegate(CrosshairManager crosshairManager, ref bool shouldShow);

	public delegate void PickCrosshairDelegate(CrosshairManager crosshairManager, ref GameObject crosshairPrefab);

	[Tooltip("The transform which should act as the container for the crosshair.")]
	public RectTransform container;

	public CameraRigController cameraRigController;

	[Tooltip("The hitmarker image.")]
	public Image hitmarker;

	private float hitmarkerAlpha;

	private float hitmarkerTimer;

	private const float hitmarkerDuration = 0.2f;

	private bool isCurrentFrameUpdated;

	private GameObject currentCrosshairPrefab;

	public CrosshairController crosshairController;

	private HudElement crosshairHudElement;

	private RtpcSetter rtpcDamageDirection;

	private static readonly List<CrosshairManager> instancesList;

	public event ShouldShowCrosshairDelegate shouldShowCrosshairGlobal;

	public event PickCrosshairDelegate pickCrosshairGlobal;

	private void OnEnable()
	{
		instancesList.Add(this);
		rtpcDamageDirection = new RtpcSetter("damageDirection", RoR2Application.instance.gameObject);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private static void StaticLateUpdate()
	{
		for (int i = 0; i < instancesList.Count; i++)
		{
			instancesList[i].DoLateUpdate();
		}
	}

	private void DoLateUpdate()
	{
		isCurrentFrameUpdated = false;
		if ((bool)cameraRigController)
		{
			UpdateCrosshair(cameraRigController.target ? cameraRigController.target.GetComponent<CharacterBody>() : null, cameraRigController.crosshairWorldPosition, cameraRigController.uiCam);
		}
		UpdateHitMarker();
	}

	private void UpdateCrosshair(CharacterBody targetBody, Vector3 crosshairWorldPosition, Camera uiCamera)
	{
		GameObject crosshairPrefab = null;
		bool shouldShow = (bool)targetBody && !targetBody.currentVehicle;
		this.shouldShowCrosshairGlobal?.Invoke(this, ref shouldShow);
		if (shouldShow && (bool)targetBody)
		{
			if (!cameraRigController.hasOverride)
			{
				if ((bool)targetBody && targetBody.healthComponent.alive)
				{
					crosshairPrefab = (targetBody.isSprinting ? LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/SprintingCrosshair") : (targetBody.hideCrosshair ? null : CrosshairUtils.GetCrosshairPrefabForBody(targetBody)));
				}
			}
			else
			{
				crosshairPrefab = CrosshairUtils.GetCrosshairPrefabForBody(targetBody);
			}
			this.pickCrosshairGlobal?.Invoke(this, ref crosshairPrefab);
		}
		if (crosshairPrefab != currentCrosshairPrefab)
		{
			if ((bool)crosshairController)
			{
				Object.Destroy(crosshairController.gameObject);
				crosshairController = null;
			}
			if ((bool)crosshairPrefab)
			{
				GameObject gameObject = Object.Instantiate(crosshairPrefab, container);
				crosshairController = gameObject.GetComponent<CrosshairController>();
				crosshairHudElement = gameObject.GetComponent<HudElement>();
			}
			currentCrosshairPrefab = crosshairPrefab;
		}
		if ((bool)crosshairController)
		{
			((RectTransform)crosshairController.gameObject.transform).anchoredPosition = new Vector2(0.5f, 0.5f);
		}
		if ((bool)crosshairHudElement)
		{
			crosshairHudElement.targetCharacterBody = targetBody;
		}
	}

	public void RefreshHitmarker(bool crit)
	{
		hitmarkerTimer = 0.2f;
		hitmarker.gameObject.SetActive(value: false);
		hitmarker.gameObject.SetActive(value: true);
		Util.PlaySound("Play_UI_hit", RoR2Application.instance.gameObject);
		if (crit)
		{
			Util.PlaySound("Play_UI_crit", RoR2Application.instance.gameObject);
		}
		isCurrentFrameUpdated = true;
	}

	private void UpdateHitMarker()
	{
		hitmarkerAlpha = Mathf.Pow(hitmarkerTimer / 0.2f, 0.75f);
		hitmarkerTimer = Mathf.Max(0f, hitmarkerTimer - Time.deltaTime);
		if ((bool)hitmarker)
		{
			Color color = hitmarker.color;
			color.a = hitmarkerAlpha;
			hitmarker.color = color;
		}
	}

	private static void HandleHitMarker(DamageDealtMessage damageDealtMessage)
	{
		for (int i = 0; i < instancesList.Count; i++)
		{
			CrosshairManager crosshairManager = instancesList[i];
			if (!crosshairManager.cameraRigController)
			{
				continue;
			}
			GameObject target = crosshairManager.cameraRigController.target;
			if (damageDealtMessage.attacker == target && !crosshairManager.isCurrentFrameUpdated)
			{
				crosshairManager.RefreshHitmarker(damageDealtMessage.crit);
			}
			else if (damageDealtMessage.victim == target)
			{
				Transform obj = crosshairManager.cameraRigController.transform;
				Vector3 position = obj.position;
				Vector3 forward = obj.forward;
				_ = obj.position;
				Vector3 vector = damageDealtMessage.position - position;
				float num = Vector2.SignedAngle(new Vector2(vector.x, vector.z), new Vector2(forward.x, forward.z));
				if (num < 0f)
				{
					num += 360f;
				}
				crosshairManager.rtpcDamageDirection.value = num;
				Util.PlaySound("Play_UI_takeDamage", RoR2Application.instance.gameObject);
			}
		}
	}

	static CrosshairManager()
	{
		instancesList = new List<CrosshairManager>();
		GlobalEventManager.onClientDamageNotified += HandleHitMarker;
		RoR2Application.onLateUpdate += StaticLateUpdate;
	}
}
