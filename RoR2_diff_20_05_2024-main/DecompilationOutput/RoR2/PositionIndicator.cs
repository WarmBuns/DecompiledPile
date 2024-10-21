using System.Collections.Generic;
using RoR2.ConVar;
using RoR2.UI;
using UnityEngine;

namespace RoR2;

[DisallowMultipleComponent]
public class PositionIndicator : MonoBehaviour
{
	public Transform targetTransform;

	private new Transform transform;

	private static readonly List<PositionIndicator> instancesList;

	[Tooltip("The child object to enable when the target is within the frame.")]
	public GameObject insideViewObject;

	[Tooltip("The child object to enable when the target is outside the frame.")]
	public GameObject outsideViewObject;

	[Tooltip("The child object to ALWAYS enable, IF its not my own position indicator.")]
	public GameObject alwaysVisibleObject;

	[Tooltip("Whether or not outsideViewObject should be rotated to point to the target.")]
	public bool shouldRotateOutsideViewObject;

	[Tooltip("The offset to apply to the rotation of the outside view object when shouldRotateOutsideViewObject is set.")]
	public float outsideViewRotationOffset;

	private float yOffset;

	private bool generateDefaultPosition;

	private static BoolConVar cvPositionIndicatorsEnable;

	public Vector3 defaultPosition { get; set; }

	private void Awake()
	{
		transform = base.transform;
	}

	private void Start()
	{
		if (!generateDefaultPosition)
		{
			generateDefaultPosition = true;
			defaultPosition = base.transform.position;
		}
		if ((bool)targetTransform)
		{
			yOffset = CalcHeadOffset(targetTransform) + 1f;
		}
	}

	private static float CalcHeadOffset(Transform transform)
	{
		Collider component = transform.GetComponent<Collider>();
		if ((bool)component)
		{
			return component.bounds.extents.y;
		}
		return 0f;
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void OnValidate()
	{
		if ((bool)insideViewObject && (bool)insideViewObject.GetComponentInChildren<PositionIndicator>())
		{
			Debug.LogError("insideViewObject may not be assigned another object with another PositionIndicator in its heirarchy!");
			insideViewObject = null;
		}
		if ((bool)outsideViewObject && (bool)outsideViewObject.GetComponentInChildren<PositionIndicator>())
		{
			Debug.LogError("outsideViewObject may not be assigned another object with another PositionIndicator in its heirarchy!");
			outsideViewObject = null;
		}
	}

	static PositionIndicator()
	{
		instancesList = new List<PositionIndicator>();
		cvPositionIndicatorsEnable = new BoolConVar("position_indicators_enable", ConVarFlags.None, "1", "Enables/Disables position indicators for allies, bosses, pings, etc.");
		UICamera.onUICameraPreCull += UpdatePositions;
	}

	private static void UpdatePositions(UICamera uiCamera)
	{
		Camera sceneCam = uiCamera.cameraRigController.sceneCam;
		Camera camera = uiCamera.camera;
		Rect pixelRect = camera.pixelRect;
		Vector2 center = pixelRect.center;
		pixelRect.size *= 0.95f;
		pixelRect.center = center;
		Vector2 center2 = pixelRect.center;
		float num = 1f / (pixelRect.width * 0.5f);
		float num2 = 1f / (pixelRect.height * 0.5f);
		Quaternion rotation = uiCamera.transform.rotation;
		CameraRigController cameraRigController = uiCamera.cameraRigController;
		Transform transform = null;
		if ((bool)cameraRigController && (bool)cameraRigController.target)
		{
			CharacterBody component = cameraRigController.target.GetComponent<CharacterBody>();
			transform = ((!component) ? cameraRigController.target.transform : component.coreTransform);
		}
		for (int i = 0; i < instancesList.Count; i++)
		{
			PositionIndicator positionIndicator = instancesList[i];
			bool flag = false;
			bool flag2 = false;
			bool active = false;
			if (!HUD.cvHudEnable.value)
			{
				positionIndicator.insideViewObject.SetActive(value: false);
				positionIndicator.outsideViewObject.SetActive(value: false);
				positionIndicator.alwaysVisibleObject.SetActive(value: false);
				continue;
			}
			float num3 = 0f;
			Vector3 vector2;
			int num4;
			if (cvPositionIndicatorsEnable.value)
			{
				Vector3 vector = (positionIndicator.targetTransform ? positionIndicator.targetTransform.position : positionIndicator.defaultPosition);
				if (!positionIndicator.targetTransform || ((bool)positionIndicator.targetTransform && positionIndicator.targetTransform != transform))
				{
					active = true;
					vector2 = sceneCam.WorldToScreenPoint(vector + new Vector3(0f, positionIndicator.yOffset, 0f));
					bool flag3 = vector2.z <= 0f;
					if (!flag3)
					{
						num4 = (pixelRect.Contains(vector2) ? 1 : 0);
						if (num4 != 0)
						{
							goto IL_0279;
						}
					}
					else
					{
						num4 = 0;
					}
					Vector2 vector3 = (Vector2)vector2 - center2;
					float a = Mathf.Abs(vector3.x * num);
					float b = Mathf.Abs(vector3.y * num2);
					float num5 = Mathf.Max(a, b);
					vector3 /= num5;
					vector3 *= (flag3 ? (-1f) : 1f);
					vector2 = vector3 + center2;
					if (positionIndicator.shouldRotateOutsideViewObject)
					{
						num3 = Mathf.Atan2(vector3.y, vector3.x) * 57.29578f;
					}
					goto IL_0279;
				}
			}
			goto IL_02a7;
			IL_0279:
			flag = (byte)num4 != 0;
			flag2 = num4 == 0;
			vector2.z = 1f;
			Vector3 position = camera.ScreenToWorldPoint(vector2);
			positionIndicator.transform.SetPositionAndRotation(position, rotation);
			goto IL_02a7;
			IL_02a7:
			if ((bool)positionIndicator.alwaysVisibleObject)
			{
				positionIndicator.alwaysVisibleObject.SetActive(active);
			}
			if (positionIndicator.insideViewObject == positionIndicator.outsideViewObject)
			{
				if ((bool)positionIndicator.insideViewObject)
				{
					positionIndicator.insideViewObject.SetActive(flag || flag2);
				}
				continue;
			}
			if ((bool)positionIndicator.insideViewObject)
			{
				positionIndicator.insideViewObject.SetActive(flag);
			}
			if ((bool)positionIndicator.outsideViewObject)
			{
				positionIndicator.outsideViewObject.SetActive(flag2);
				if (flag2 && positionIndicator.shouldRotateOutsideViewObject)
				{
					positionIndicator.outsideViewObject.transform.localEulerAngles = new Vector3(0f, 0f, num3 + positionIndicator.outsideViewRotationOffset);
				}
			}
		}
	}
}
