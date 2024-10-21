using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(EventFunctions))]
public class UITransitionController : MonoBehaviour
{
	public enum TransitionStyle
	{
		Instant,
		CanvasGroupAlphaFade,
		SwipeYScale,
		SwipeXScale
	}

	public TransitionStyle transitionIn;

	public TransitionStyle transitionOut;

	public float transitionInSpeed = 1f;

	public float transitionOutSpeed = 1f;

	public bool transitionOutAtEndOfLifetime;

	public float lifetime;

	private float stopwatch;

	private Animator animator;

	private bool done;

	private void Awake()
	{
		animator = GetComponent<Animator>();
		PushMecanimTransitionInParameters();
	}

	private void PushMecanimTransitionInParameters()
	{
		animator.SetFloat("transitionInSpeed", transitionInSpeed);
		switch (transitionIn)
		{
		case TransitionStyle.Instant:
			animator.SetTrigger("InstantIn");
			break;
		case TransitionStyle.CanvasGroupAlphaFade:
			animator.SetTrigger("CanvasGroupAlphaFadeIn");
			break;
		case TransitionStyle.SwipeYScale:
			animator.SetTrigger("SwipeYScaleIn");
			break;
		case TransitionStyle.SwipeXScale:
			animator.SetTrigger("SwipeXScaleIn");
			break;
		}
	}

	private void PushMecanimTransitionOutParameters()
	{
		animator.SetFloat("transitionOutSpeed", transitionOutSpeed);
		switch (transitionOut)
		{
		case TransitionStyle.Instant:
			animator.SetTrigger("InstantOut");
			break;
		case TransitionStyle.CanvasGroupAlphaFade:
			animator.SetTrigger("CanvasGroupAlphaFadeOut");
			break;
		case TransitionStyle.SwipeYScale:
			animator.SetTrigger("SwipeYScaleOut");
			break;
		case TransitionStyle.SwipeXScale:
			animator.SetTrigger("SwipeXScaleOut");
			break;
		}
	}

	private void Update()
	{
		if (transitionOutAtEndOfLifetime && !done)
		{
			stopwatch += Time.deltaTime;
			if (stopwatch >= lifetime)
			{
				PushMecanimTransitionOutParameters();
				done = true;
			}
		}
	}
}
