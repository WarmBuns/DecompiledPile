using UnityEngine;
using UnityEngine.Events;

namespace RoR2.UI;

[RequireComponent(typeof(HudElement))]
[RequireComponent(typeof(RectTransform))]
public class LoaderHookCrosshairController : MonoBehaviour
{
	public float range;

	public UnityEvent onAvailable;

	public UnityEvent onUnavailable;

	public UnityEvent onEnterRange;

	public UnityEvent onExitRange;

	private HudElement hudElement;

	private bool isAvailable;

	private bool inRange;

	private void Awake()
	{
		hudElement = GetComponent<HudElement>();
	}

	private void SetAvailable(bool newIsAvailable)
	{
		if (isAvailable != newIsAvailable)
		{
			isAvailable = newIsAvailable;
			(isAvailable ? onAvailable : onUnavailable).Invoke();
		}
	}

	private void SetInRange(bool newInRange)
	{
		if (inRange != newInRange)
		{
			inRange = newInRange;
			(inRange ? onEnterRange : onExitRange)?.Invoke();
		}
	}

	private void Update()
	{
		if (!hudElement.targetCharacterBody)
		{
			SetAvailable(newIsAvailable: false);
			SetInRange(newInRange: false);
			return;
		}
		SetAvailable(hudElement.targetCharacterBody.skillLocator.secondary.CanExecute());
		bool flag = false;
		if (isAvailable)
		{
			flag = hudElement.targetCharacterBody.inputBank.GetAimRaycast(range, out var _);
		}
		SetInRange(flag);
	}
}
