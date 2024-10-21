using ThreeEyedGames;
using UnityEngine;
using UnityEngine.Events;

namespace RoR2;

public class AnimateShaderAlpha : MonoBehaviour
{
	public AnimationCurve alphaCurve;

	private Renderer targetRenderer;

	private MaterialPropertyBlock _propBlock;

	private Material[] materials;

	public float timeMax = 5f;

	[Tooltip("Optional field if you want to animate Decal 'Fade' rather than renderer _ExternalAlpha.")]
	public Decal decal;

	public bool pauseTime;

	public bool destroyOnEnd;

	public bool disableOnEnd;

	[HideInInspector]
	public float time;

	private float initialFade;

	[HideInInspector]
	public bool initialyEnabled;

	[HideInInspector]
	public UnityEvent OnFinished;

	public bool continueExistingAfterTimeMaxIsReached;

	private bool initialised;

	private void Start()
	{
		Initialise();
	}

	private void Initialise()
	{
		if (!initialised)
		{
			targetRenderer = GetComponent<Renderer>();
			if ((bool)targetRenderer)
			{
				materials = targetRenderer.materials;
			}
			if ((bool)decal)
			{
				initialFade = decal.Fade;
			}
			initialised = true;
		}
	}

	public void Reset()
	{
		Initialise();
		time = 0f;
		if ((bool)decal)
		{
			decal.Fade = initialFade;
		}
		else
		{
			Material[] array = materials;
			for (int i = 0; i < array.Length; i++)
			{
				_ = array[i];
				_propBlock = new MaterialPropertyBlock();
				targetRenderer.GetPropertyBlock(_propBlock);
				_propBlock.SetFloat("_ExternalAlpha", 1f);
				targetRenderer.SetPropertyBlock(_propBlock);
			}
		}
		base.enabled = initialyEnabled;
	}

	private void OnDestroy()
	{
		OnFinished.RemoveAllListeners();
	}

	private void Update()
	{
		if (!pauseTime)
		{
			time = Mathf.Min(timeMax, time + Time.deltaTime);
		}
		float num = alphaCurve.Evaluate(time / timeMax);
		if ((bool)decal)
		{
			decal.Fade = num * initialFade;
		}
		else
		{
			Material[] array = materials;
			for (int i = 0; i < array.Length; i++)
			{
				_ = array[i];
				_propBlock = new MaterialPropertyBlock();
				targetRenderer.GetPropertyBlock(_propBlock);
				_propBlock.SetFloat("_ExternalAlpha", num);
				targetRenderer.SetPropertyBlock(_propBlock);
			}
		}
		if (time >= timeMax)
		{
			OnFinished?.Invoke();
			if (disableOnEnd)
			{
				base.enabled = false;
			}
			if (destroyOnEnd)
			{
				Object.Destroy(base.gameObject);
			}
		}
	}
}
