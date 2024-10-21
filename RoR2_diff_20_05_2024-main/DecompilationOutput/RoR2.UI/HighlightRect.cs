using System.Collections.Generic;
using System.Collections.ObjectModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Canvas))]
public class HighlightRect : MonoBehaviour
{
	public enum HighlightState
	{
		Expanding,
		Holding,
		Contracting
	}

	public AnimationCurve curve;

	public Color highlightColor;

	public Sprite cornerImage;

	public string nametagString;

	private Image bottomLeftImage;

	private Image bottomRightImage;

	private Image topLeftImage;

	private Image topRightImage;

	private TextMeshProUGUI nametagText;

	public Renderer targetRenderer;

	public GameObject cameraTarget;

	public RectTransform nametagRectTransform;

	public RectTransform bottomLeftRectTransform;

	public RectTransform bottomRightRectTransform;

	public RectTransform topLeftRectTransform;

	public RectTransform topRightRectTransform;

	public float expandTime = 1f;

	public float maxLifeTime;

	public bool destroyOnLifeEnd;

	private float time;

	public HighlightState highlightState;

	private static List<HighlightRect> instancesList;

	private Canvas canvas;

	private Camera uiCam;

	private Camera sceneCam;

	private static readonly Vector2[] extentPoints;

	static HighlightRect()
	{
		instancesList = new List<HighlightRect>();
		extentPoints = new Vector2[8];
		RoR2Application.onLateUpdate += UpdateAll;
	}

	private void Awake()
	{
		canvas = GetComponent<Canvas>();
	}

	private void OnEnable()
	{
		instancesList.Add(this);
	}

	private void OnDisable()
	{
		instancesList.Remove(this);
	}

	private void Start()
	{
		highlightState = HighlightState.Expanding;
		bottomLeftImage = bottomLeftRectTransform.GetComponent<Image>();
		bottomRightImage = bottomRightRectTransform.GetComponent<Image>();
		topLeftImage = topLeftRectTransform.GetComponent<Image>();
		topRightImage = topRightRectTransform.GetComponent<Image>();
		bottomLeftImage.sprite = cornerImage;
		bottomRightImage.sprite = cornerImage;
		topLeftImage.sprite = cornerImage;
		topRightImage.sprite = cornerImage;
		bottomLeftImage.color = highlightColor;
		bottomRightImage.color = highlightColor;
		topLeftImage.color = highlightColor;
		topRightImage.color = highlightColor;
		if ((bool)nametagRectTransform)
		{
			nametagText = nametagRectTransform.GetComponent<TextMeshProUGUI>();
			nametagText.color = highlightColor;
			nametagText.text = nametagString;
		}
	}

	private static void UpdateAll()
	{
		for (int num = instancesList.Count - 1; num >= 0; num--)
		{
			instancesList[num].DoUpdate();
		}
	}

	private void DoUpdate()
	{
		if (!targetRenderer)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		switch (highlightState)
		{
		case HighlightState.Expanding:
			time += Time.deltaTime;
			if (time >= expandTime)
			{
				time = expandTime;
				highlightState = HighlightState.Holding;
			}
			break;
		case HighlightState.Holding:
			if (destroyOnLifeEnd)
			{
				time += Time.deltaTime;
				if (time > maxLifeTime)
				{
					highlightState = HighlightState.Contracting;
					time = expandTime;
				}
			}
			break;
		case HighlightState.Contracting:
			time -= Time.deltaTime;
			if (time <= 0f)
			{
				Object.Destroy(base.gameObject);
				return;
			}
			break;
		}
		Rect rect = GUIRectWithObject(sceneCam, targetRenderer);
		Vector2 a = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, 0.5f), Mathf.Lerp(rect.yMin, rect.yMax, 0.5f));
		float t = curve.Evaluate(time / expandTime);
		bottomLeftRectTransform.anchoredPosition = Vector2.LerpUnclamped(a, new Vector2(rect.xMin, rect.yMin), t);
		bottomRightRectTransform.anchoredPosition = Vector2.LerpUnclamped(a, new Vector2(rect.xMax, rect.yMin), t);
		topLeftRectTransform.anchoredPosition = Vector2.LerpUnclamped(a, new Vector2(rect.xMin, rect.yMax), t);
		topRightRectTransform.anchoredPosition = Vector2.LerpUnclamped(a, new Vector2(rect.xMax, rect.yMax), t);
		if ((bool)nametagRectTransform)
		{
			nametagRectTransform.anchoredPosition = Vector2.LerpUnclamped(a, new Vector2(rect.xMin, rect.yMax), t);
		}
	}

	public static Rect GUIRectWithObject(Camera cam, Renderer rend)
	{
		Vector3 center = rend.bounds.center;
		Vector3 extents = rend.bounds.extents;
		extentPoints[0] = WorldToGUIPoint(cam, new Vector3(center.x - extents.x, center.y - extents.y, center.z - extents.z));
		extentPoints[1] = WorldToGUIPoint(cam, new Vector3(center.x + extents.x, center.y - extents.y, center.z - extents.z));
		extentPoints[2] = WorldToGUIPoint(cam, new Vector3(center.x - extents.x, center.y - extents.y, center.z + extents.z));
		extentPoints[3] = WorldToGUIPoint(cam, new Vector3(center.x + extents.x, center.y - extents.y, center.z + extents.z));
		extentPoints[4] = WorldToGUIPoint(cam, new Vector3(center.x - extents.x, center.y + extents.y, center.z - extents.z));
		extentPoints[5] = WorldToGUIPoint(cam, new Vector3(center.x + extents.x, center.y + extents.y, center.z - extents.z));
		extentPoints[6] = WorldToGUIPoint(cam, new Vector3(center.x - extents.x, center.y + extents.y, center.z + extents.z));
		extentPoints[7] = WorldToGUIPoint(cam, new Vector3(center.x + extents.x, center.y + extents.y, center.z + extents.z));
		Vector2 lhs = extentPoints[0];
		Vector2 lhs2 = extentPoints[0];
		Vector2[] array = extentPoints;
		foreach (Vector2 rhs in array)
		{
			lhs = Vector2.Min(lhs, rhs);
			lhs2 = Vector2.Max(lhs2, rhs);
		}
		return new Rect(lhs.x, lhs.y, lhs2.x - lhs.x, lhs2.y - lhs.y);
	}

	public static Vector2 WorldToGUIPoint(Camera cam, Vector3 world)
	{
		return cam.WorldToScreenPoint(world);
	}

	public static void CreateHighlight(GameObject viewerBodyObject, Renderer targetRenderer, GameObject highlightPrefab, float overrideDuration = -1f, bool visibleToAll = false)
	{
		ReadOnlyCollection<CameraRigController> readOnlyInstancesList = CameraRigController.readOnlyInstancesList;
		int i = 0;
		for (int count = readOnlyInstancesList.Count; i < count; i++)
		{
			CameraRigController cameraRigController = readOnlyInstancesList[i];
			if (!(cameraRigController.target != viewerBodyObject) || visibleToAll)
			{
				HighlightRect component = Object.Instantiate(highlightPrefab).GetComponent<HighlightRect>();
				component.targetRenderer = targetRenderer;
				component.canvas.worldCamera = cameraRigController.uiCam;
				component.uiCam = cameraRigController.uiCam;
				component.sceneCam = cameraRigController.sceneCam;
				if (overrideDuration > 0f)
				{
					component.maxLifeTime = overrideDuration;
				}
			}
		}
	}
}
