using UnityEngine;

namespace RoR2.UI;

public class ActiveReloadBarController : MonoBehaviour
{
	[SerializeField]
	private RectTransform timeIndicatorTransform;

	[SerializeField]
	private RectTransform windowIndicatorTransform;

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private string isWindowActiveParamName;

	[SerializeField]
	private string wasWindowHitParamName;

	[SerializeField]
	private string wasFailureParamName;

	private int isWindowActiveParamHash;

	private int wasWindowHitParamHash;

	private int wasFailureParamHash;

	public void OnEnable()
	{
		isWindowActiveParamHash = Animator.StringToHash(isWindowActiveParamName);
		wasWindowHitParamHash = Animator.StringToHash(wasWindowHitParamName);
		wasFailureParamHash = Animator.StringToHash(wasFailureParamName);
		SetTValue(0f);
	}

	public void SetWindowRange(float tStart, float tEnd)
	{
		tStart = Mathf.Max(0f, tStart);
		tEnd = Mathf.Min(1f, tEnd);
		windowIndicatorTransform.anchorMin = new Vector2(tStart, windowIndicatorTransform.anchorMin.y);
		windowIndicatorTransform.anchorMax = new Vector2(tEnd, windowIndicatorTransform.anchorMax.y);
	}

	public void SetIsWindowActive(bool isWindowActive)
	{
		if ((bool)animator)
		{
			animator.SetBool(isWindowActiveParamHash, isWindowActive);
		}
	}

	public void SetWasWindowHit(bool wasWindowHit)
	{
		if ((bool)animator)
		{
			animator.SetBool(wasWindowHitParamHash, wasWindowHit);
		}
	}

	public void SetWasFailure(bool wasFailure)
	{
		if ((bool)animator)
		{
			animator.SetBool(wasFailureParamHash, wasFailure);
		}
	}

	public void SetTValue(float t)
	{
		timeIndicatorTransform.anchorMin = new Vector2(t, timeIndicatorTransform.anchorMin.y);
		timeIndicatorTransform.anchorMax = new Vector2(t, timeIndicatorTransform.anchorMax.y);
	}
}
