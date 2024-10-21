using System.Collections.Generic;
using System.Reflection;
using Rewired.Integration.UnityUI;
using Rewired.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace RoR2.UI;

[RequireComponent(typeof(MPInput))]
public class MPInputModule : RewiredStandaloneInputModule
{
	private delegate bool ShouldStartDragDelegate(Vector2 pressPos, Vector2 currentPos, float threshold, bool useDragThreshold);

	private MouseState m_MouseState;

	private static readonly FieldInfo m_MouseStateField = typeof(RewiredPointerInputModule).GetField("m_MouseState", BindingFlags.Instance | BindingFlags.NonPublic);

	private static readonly ShouldStartDragDelegate ShouldStartDrag = (ShouldStartDragDelegate)typeof(RewiredPointerInputModule).GetMethod("ShouldStartDrag", BindingFlags.Static | BindingFlags.NonPublic).CreateDelegate(typeof(ShouldStartDragDelegate));

	public bool isHovering;

	private bool useCursor => ((MPEventSystem)base.eventSystem).isCursorVisible;

	protected override void Awake()
	{
		m_InputOverride = GetComponent<MPInput>();
		m_MouseState = (MouseState)m_MouseStateField.GetValue(this);
		base.Awake();
	}

	protected override void Start()
	{
		base.Start();
		ClearMouseInputSources();
		AddMouseInputSource((MPInput)m_InputOverride);
	}

	protected void UpdateHover(List<RaycastResult> raycastResults)
	{
		isHovering = false;
		if (!useCursor)
		{
			return;
		}
		foreach (RaycastResult raycastResult in raycastResults)
		{
			if ((bool)raycastResult.gameObject)
			{
				Selectable componentInParent = raycastResult.gameObject.GetComponentInParent<Selectable>();
				if (componentInParent != null && IsHoverable(componentInParent))
				{
					isHovering = true;
					break;
				}
			}
		}
		bool IsHoverable(Selectable selectable)
		{
			MPButton mPButton = selectable as MPButton;
			if (!mPButton)
			{
				return true;
			}
			return mPButton.InputModuleIsAllowed(this);
		}
	}

	protected override MouseState GetMousePointerEventData(int playerId, int mouseIndex)
	{
		IMouseInputSource mouseInputSource = GetMouseInputSource(playerId, mouseIndex);
		if (mouseInputSource == null)
		{
			return null;
		}
		PlayerPointerEventData data;
		bool pointerData = GetPointerData(playerId, mouseIndex, -1, out data, create: true, PointerEventType.Mouse);
		data.Reset();
		if (pointerData)
		{
			data.position = base.input.mousePosition;
		}
		Vector2 mousePosition = base.input.mousePosition;
		if (mouseInputSource.locked)
		{
			data.position = new Vector2(-1f, -1f);
			data.delta = Vector2.zero;
		}
		else
		{
			data.delta = mousePosition - data.position;
			data.position = mousePosition;
		}
		data.scrollDelta = mouseInputSource.wheelDelta;
		data.button = PointerEventData.InputButton.Left;
		base.eventSystem.RaycastAll(data, m_RaycastResultCache);
		RaycastResult pointerCurrentRaycast = BaseInputModule.FindFirstRaycast(m_RaycastResultCache);
		data.pointerCurrentRaycast = pointerCurrentRaycast;
		UpdateHover(m_RaycastResultCache);
		m_RaycastResultCache.Clear();
		GetPointerData(playerId, mouseIndex, -2, out var data2, create: true, PointerEventType.Mouse);
		CopyFromTo(data, data2);
		data2.button = PointerEventData.InputButton.Right;
		GetPointerData(playerId, mouseIndex, -3, out var data3, create: true, PointerEventType.Mouse);
		CopyFromTo(data, data3);
		data3.button = PointerEventData.InputButton.Middle;
		for (int i = 3; i < mouseInputSource.buttonCount; i++)
		{
			GetPointerData(playerId, mouseIndex, -2147483520 + i, out var data4, create: true, PointerEventType.Mouse);
			CopyFromTo(data, data4);
			data4.button = (PointerEventData.InputButton)(-1);
		}
		m_MouseState.SetButtonState(0, StateForMouseButton(playerId, mouseIndex, 0), data);
		m_MouseState.SetButtonState(1, StateForMouseButton(playerId, mouseIndex, 1), data2);
		m_MouseState.SetButtonState(2, StateForMouseButton(playerId, mouseIndex, 2), data3);
		for (int j = 3; j < mouseInputSource.buttonCount; j++)
		{
			GetPointerData(playerId, mouseIndex, -2147483520 + j, out var data5, create: false, PointerEventType.Mouse);
			m_MouseState.SetButtonState(j, StateForMouseButton(playerId, mouseIndex, j), data5);
		}
		return m_MouseState;
	}

	protected override void ProcessMove(PlayerPointerEventData pointerEvent)
	{
		GameObject newEnterTarget = ((!useCursor) ? null : pointerEvent.pointerCurrentRaycast.gameObject);
		HandlePointerExitAndEnter(pointerEvent, newEnterTarget);
	}

	protected override void ProcessDrag(PlayerPointerEventData pointerEvent)
	{
		if (!pointerEvent.IsPointerMoving() || !useCursor || pointerEvent.pointerDrag == null)
		{
			return;
		}
		if (!pointerEvent.dragging && ShouldStartDrag(pointerEvent.pressPosition, pointerEvent.position, base.eventSystem.pixelDragThreshold, pointerEvent.useDragThreshold))
		{
			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.beginDragHandler);
			pointerEvent.dragging = true;
		}
		if (pointerEvent.dragging)
		{
			if (pointerEvent.pointerPress != pointerEvent.pointerDrag)
			{
				ExecuteEvents.Execute(pointerEvent.pointerPress, pointerEvent, ExecuteEvents.pointerUpHandler);
				pointerEvent.eligibleForClick = false;
				pointerEvent.pointerPress = null;
				pointerEvent.rawPointerPress = null;
			}
			ExecuteEvents.Execute(pointerEvent.pointerDrag, pointerEvent, ExecuteEvents.dragHandler);
		}
	}
}
