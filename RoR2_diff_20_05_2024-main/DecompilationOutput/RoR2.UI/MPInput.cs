using System.Runtime.CompilerServices;
using Rewired;
using Rewired.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace RoR2.UI;

public class MPInput : BaseInput, IMouseInputSource
{
	public Player player;

	private MPEventSystem eventSystem;

	[FormerlySerializedAs("useAcceleration")]
	public bool useCursorAcceleration = true;

	[FormerlySerializedAs("acceleration")]
	public float cursorAcceleration = 8f;

	public float cursorStickyModifier = 1f / 3f;

	public float cursorScreenSpeed = 0.75f;

	private float stickMagnitude;

	private Vector2 _scrollDelta;

	private Vector2 internalScreenPositionDelta;

	public Vector2 internalMousePosition;

	public override Vector2 mousePosition => internalMousePosition;

	public override Vector2 mouseScrollDelta => _scrollDelta;

	public int playerId => player.id;

	public bool locked => !eventSystem.isCursorVisible;

	public int buttonCount => 3;

	public Vector2 screenPosition => internalMousePosition;

	public Vector2 screenPositionDelta => internalScreenPositionDelta;

	public Vector2 wheelDelta => _scrollDelta;

	protected override void Awake()
	{
		base.Awake();
		eventSystem = GetComponent<MPEventSystem>();
	}

	private static int MouseButtonToAction(int button)
	{
		return button switch
		{
			0 => 20, 
			1 => 21, 
			2 => 22, 
			_ => -1, 
		};
	}

	public override bool GetMouseButtonDown(int button)
	{
		return player.GetButtonDown(MouseButtonToAction(button));
	}

	public override bool GetMouseButtonUp(int button)
	{
		return player.GetButtonUp(MouseButtonToAction(button));
	}

	public override bool GetMouseButton(int button)
	{
		return player.GetButton(MouseButtonToAction(button));
	}

	public void CenterCursor()
	{
		internalMousePosition = new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f);
	}

	public void Update()
	{
		if (!eventSystem.isCursorVisible)
		{
			return;
		}
		float num = Screen.width;
		float num2 = Screen.height;
		float num3 = Mathf.Min(num / 1920f, num2 / 1080f);
		internalScreenPositionDelta = Vector2.zero;
		if (eventSystem.currentInputSource == MPEventSystem.InputSource.MouseAndKeyboard)
		{
			if (Application.isFocused)
			{
				internalMousePosition = Input.mousePosition;
			}
		}
		else
		{
			Vector2 vector = new Vector2(player.GetAxis(23), player.GetAxis(24));
			float magnitude = vector.magnitude;
			stickMagnitude = Mathf.Min(Mathf.MoveTowards(stickMagnitude, magnitude, cursorAcceleration * Time.unscaledDeltaTime), magnitude);
			float num4 = stickMagnitude;
			if (eventSystem.isHovering)
			{
				num4 *= cursorStickyModifier;
			}
			Vector2 vector2 = ((magnitude == 0f) ? Vector2.zero : (vector * (num4 / magnitude)));
			float num5 = 1920f * cursorScreenSpeed * num3;
			internalScreenPositionDelta = vector2 * Time.unscaledDeltaTime * num5;
			internalMousePosition += internalScreenPositionDelta;
		}
		internalMousePosition.x = Mathf.Clamp(internalMousePosition.x, 0f, num);
		internalMousePosition.y = Mathf.Clamp(internalMousePosition.y, 0f, num2);
		_scrollDelta = new Vector2(0f, player.GetAxis(26));
	}

	public bool GetButtonDown(int button)
	{
		return GetMouseButtonDown(button);
	}

	public bool GetButtonUp(int button)
	{
		return GetMouseButtonUp(button);
	}

	public bool GetButton(int button)
	{
		return GetMouseButton(button);
	}

	[SpecialName]
	bool IMouseInputSource.get_enabled()
	{
		return base.enabled;
	}
}
