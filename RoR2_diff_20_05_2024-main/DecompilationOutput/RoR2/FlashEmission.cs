using UnityEngine;

namespace RoR2;

public class FlashEmission : MonoBehaviour
{
	[SerializeField]
	[Tooltip("The strength of a single flash (off,on,off)")]
	private AnimationCurve flashCurve;

	[SerializeField]
	[Tooltip("The duration of a single flash (off,on,off)")]
	private float flashDuration;

	[SerializeField]
	private Renderer targetRenderer;

	private Material[] materials;

	[HideInInspector]
	public float time;

	private MaterialPropertyBlock propBlock;

	private bool active;

	private void OnEnable()
	{
		Initialize();
	}

	private void OnDisable()
	{
		active = false;
	}

	private void Initialize()
	{
		if (targetRenderer == null)
		{
			targetRenderer = GetComponent<Renderer>();
		}
		if (targetRenderer == null)
		{
			Debug.LogWarning("FlashEmission on " + base.gameObject.name + " couldn't find a Renderer, shutting down.");
			base.enabled = false;
		}
		else if (materials == null)
		{
			materials = targetRenderer.materials;
		}
	}

	private void Update()
	{
		if (!active)
		{
			return;
		}
		float deltaTime = Time.deltaTime;
		if (deltaTime != 0f)
		{
			time = (time + deltaTime) % flashDuration;
			float value = flashCurve.Evaluate(time / flashDuration);
			Material[] array = materials;
			for (int i = 0; i < array.Length; i++)
			{
				_ = array[i];
				propBlock = new MaterialPropertyBlock();
				targetRenderer.GetPropertyBlock(propBlock);
				propBlock.SetFloat("_EmPower", value);
				targetRenderer.SetPropertyBlock(propBlock);
			}
		}
	}

	public void StartFlash()
	{
		active = true;
	}

	public void StopFlash()
	{
		active = false;
	}
}
