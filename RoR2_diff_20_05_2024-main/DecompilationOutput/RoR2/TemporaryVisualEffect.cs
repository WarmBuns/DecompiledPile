using UnityEngine;

namespace RoR2;

public class TemporaryVisualEffect : MonoBehaviour
{
	public enum VisualState
	{
		Enter,
		Exit
	}

	public float radius = 1f;

	public Transform parentTransform;

	public Transform visualTransform;

	public MonoBehaviour[] enterComponents;

	public MonoBehaviour[] exitComponents;

	public VisualState visualState;

	private VisualState previousVisualState;

	[HideInInspector]
	public HealthComponent healthComponent;

	private void Start()
	{
		RebuildVisualComponents();
	}

	private void FixedUpdate()
	{
		if (previousVisualState != visualState)
		{
			RebuildVisualComponents();
		}
		previousVisualState = visualState;
	}

	private void RebuildVisualComponents()
	{
		switch (visualState)
		{
		case VisualState.Enter:
		{
			MonoBehaviour[] array = enterComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			array = exitComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			break;
		}
		case VisualState.Exit:
		{
			MonoBehaviour[] array = enterComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = false;
			}
			array = exitComponents;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			break;
		}
		}
	}

	private void LateUpdate()
	{
		bool flag = healthComponent;
		if ((bool)parentTransform)
		{
			base.transform.position = parentTransform.position;
		}
		else
		{
			Object.Destroy(base.gameObject);
		}
		if (!flag || (flag && !healthComponent.alive))
		{
			visualState = VisualState.Exit;
		}
		if ((bool)visualTransform)
		{
			visualTransform.localScale = new Vector3(radius, radius, radius);
		}
	}
}
