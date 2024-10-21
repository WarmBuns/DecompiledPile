using System;
using System.Collections.Generic;
using HG;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(RectTransform))]
public class PointViewer : MonoBehaviour, ILayoutGroup, ILayoutController
{
	private struct ElementInfo
	{
		public Transform targetTransform;

		public Vector3 targetLastKnownPosition;

		public float targetWorldDiameter;

		public float targetWorldVerticalOffset;

		public bool scaleWithDistance;

		public GameObject elementInstance;

		public RectTransform elementRectTransform;

		public CanvasGroup elementCanvasGroup;
	}

	public struct AddElementRequest
	{
		public GameObject elementPrefab;

		public Transform target;

		public float targetWorldVerticalOffset;

		public float targetWorldDiameter;

		public bool scaleWithDistance;

		public float targetWorldRadius
		{
			get
			{
				return targetWorldDiameter * 0.5f;
			}
			set
			{
				targetWorldDiameter = value * 2f;
			}
		}
	}

	private UICamera uiCamController;

	private Camera sceneCam;

	private StructAllocator<ElementInfo> elementInfoAllocator = new StructAllocator<ElementInfo>(16u);

	private Dictionary<UnityObjectWrapperKey<GameObject>, StructAllocator<ElementInfo>.Ptr> elementToElementInfo = new Dictionary<UnityObjectWrapperKey<GameObject>, StructAllocator<ElementInfo>.Ptr>();

	protected RectTransform rectTransform { get; private set; }

	protected Canvas canvas { get; private set; }

	private void Awake()
	{
		rectTransform = (RectTransform)base.transform;
		canvas = GetComponent<Canvas>();
	}

	private void Start()
	{
		FindCamera();
	}

	private void Update()
	{
		SetDirty();
	}

	public GameObject AddElement(AddElementRequest request)
	{
		if (!request.target || !request.elementPrefab)
		{
			return null;
		}
		GameObject gameObject = UnityEngine.Object.Instantiate(request.elementPrefab, rectTransform);
		StructAllocator<ElementInfo>.Ptr ptr = elementInfoAllocator.Alloc();
		ref ElementInfo @ref = ref elementInfoAllocator.GetRef(ptr);
		@ref.targetTransform = request.target;
		@ref.targetWorldVerticalOffset = request.targetWorldVerticalOffset;
		@ref.targetWorldDiameter = request.targetWorldDiameter;
		@ref.targetLastKnownPosition = request.target.position;
		@ref.scaleWithDistance = request.scaleWithDistance;
		@ref.elementInstance = gameObject;
		@ref.elementRectTransform = (RectTransform)gameObject.transform;
		@ref.elementCanvasGroup = gameObject.GetComponent<CanvasGroup>();
		elementToElementInfo.Add(gameObject, ptr);
		return gameObject;
	}

	public void RemoveElement(GameObject elementInstance)
	{
		if (elementToElementInfo.TryGetValue(elementInstance, out var value))
		{
			elementToElementInfo.Remove(elementInstance);
			elementInfoAllocator.Free(in value);
			UnityEngine.Object.Destroy(elementInstance);
		}
	}

	public void RemoveAll()
	{
		foreach (KeyValuePair<UnityObjectWrapperKey<GameObject>, StructAllocator<ElementInfo>.Ptr> item in elementToElementInfo)
		{
			GameObject obj = item.Key;
			StructAllocator<ElementInfo>.Ptr ptr = item.Value;
			elementInfoAllocator.Free(in ptr);
			UnityEngine.Object.Destroy(obj);
		}
		elementToElementInfo.Clear();
	}

	private void FindCamera()
	{
		uiCamController = canvas.rootCanvas.worldCamera.GetComponent<UICamera>();
		sceneCam = (uiCamController ? uiCamController.cameraRigController.sceneCam : null);
	}

	private void SetDirty()
	{
		if (base.isActiveAndEnabled && !CanvasUpdateRegistry.IsRebuildingLayout())
		{
			LayoutRebuilder.MarkLayoutForRebuild(rectTransform);
		}
	}

	protected void UpdateAllElementPositions()
	{
		if (!uiCamController || !sceneCam)
		{
			return;
		}
		Camera camera = uiCamController.camera;
		Vector2 size = rectTransform.rect.size;
		float num = sceneCam.fieldOfView * (MathF.PI / 180f);
		float num2 = 1f / num;
		foreach (KeyValuePair<UnityObjectWrapperKey<GameObject>, StructAllocator<ElementInfo>.Ptr> item in elementToElementInfo)
		{
			_ = (GameObject)item.Key;
			StructAllocator<ElementInfo>.Ptr value = item.Value;
			ref ElementInfo @ref = ref elementInfoAllocator.GetRef(value);
			if ((bool)@ref.targetTransform)
			{
				@ref.targetLastKnownPosition = @ref.targetTransform.position;
			}
			Vector3 targetLastKnownPosition = @ref.targetLastKnownPosition;
			targetLastKnownPosition.y += @ref.targetWorldVerticalOffset;
			Vector3 position = sceneCam.WorldToViewportPoint(targetLastKnownPosition);
			float z = position.z;
			Vector3 vector = camera.ViewportToScreenPoint(position);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, vector, camera, out var localPoint);
			Vector3 localPosition = localPoint;
			localPosition.z = ((z >= 0f) ? 0f : (-1f));
			@ref.elementRectTransform.localPosition = localPosition;
			if (@ref.scaleWithDistance)
			{
				float num3 = @ref.targetWorldDiameter * num2 / z;
				@ref.elementRectTransform.sizeDelta = num3 * size;
			}
		}
	}

	public void SetLayoutHorizontal()
	{
		UpdateAllElementPositions();
	}

	public void SetLayoutVertical()
	{
	}
}
