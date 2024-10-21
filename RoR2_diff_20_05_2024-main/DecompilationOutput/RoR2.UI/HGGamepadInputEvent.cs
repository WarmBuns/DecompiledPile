using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI;

[RequireComponent(typeof(MPEventSystemLocator))]
public class HGGamepadInputEvent : MonoBehaviour
{
	public string actionName;

	public UnityEvent actionEvent;

	public UILayerKey requiredTopLayer;

	public GameObject[] enabledObjectsIfActive;

	private MPEventSystemLocator eventSystemLocator;

	public bool RequireInputMapperNotRunning;

	private bool couldAcceptInput;

	protected MPEventSystem eventSystem => eventSystemLocator?.eventSystem;

	protected void Awake()
	{
		eventSystemLocator = GetComponent<MPEventSystemLocator>();
	}

	protected void Update()
	{
		bool flag = CanAcceptInput();
		if (couldAcceptInput != flag)
		{
			GameObject[] array = enabledObjectsIfActive;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].SetActive(flag);
			}
		}
		if (CanAcceptInput() && eventSystem.player.GetButtonDown(actionName))
		{
			actionEvent.Invoke();
		}
		couldAcceptInput = flag;
	}

	protected bool CanAcceptInput()
	{
		if (base.gameObject.activeInHierarchy && (!requiredTopLayer || requiredTopLayer.representsTopLayer))
		{
			MPEventSystem mPEventSystem = eventSystem;
			if ((object)mPEventSystem != null && mPEventSystem.currentInputSource == MPEventSystem.InputSource.Gamepad)
			{
				if (RequireInputMapperNotRunning)
				{
					if (!InputMapperHelper.anyRemapperListening)
					{
						return InputMapperHelper.anyRemapperListeningStopTimestamp != Time.unscaledTime;
					}
					return false;
				}
				return true;
			}
		}
		return false;
	}
}
