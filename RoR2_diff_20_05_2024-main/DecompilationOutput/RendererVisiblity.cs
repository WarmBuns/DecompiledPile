using RoR2;
using UnityEngine;

public class RendererVisiblity : MonoBehaviour
{
	public enum DistanceClass
	{
		Near,
		Far,
		VeryFar
	}

	public CharacterModel parentModel;

	public DistanceClass approxDistance;

	public bool isVisible;

	[HideInInspector]
	public Animator animatorToUpdateOnceWhenVisible;

	private float visualUpdateTimer;

	public bool shouldUpdateVisuals = true;

	public void CalcDistanceClass(float distanceSq)
	{
		if (distanceSq < 2500f)
		{
			approxDistance = DistanceClass.Near;
		}
		else if (distanceSq < 10000f)
		{
			approxDistance = DistanceClass.Far;
		}
		else
		{
			approxDistance = DistanceClass.VeryFar;
		}
		if (!isVisible)
		{
			visualUpdateTimer -= Time.deltaTime;
			if (visualUpdateTimer < 0f)
			{
				visualUpdateTimer = 0.2f;
				shouldUpdateVisuals = true;
			}
			else
			{
				shouldUpdateVisuals = false;
			}
			return;
		}
		switch (approxDistance)
		{
		case DistanceClass.Near:
			shouldUpdateVisuals = true;
			break;
		case DistanceClass.Far:
			visualUpdateTimer -= Time.deltaTime;
			if (visualUpdateTimer < 0f)
			{
				visualUpdateTimer = 1f / 15f;
				shouldUpdateVisuals = true;
			}
			else
			{
				shouldUpdateVisuals = false;
			}
			break;
		case DistanceClass.VeryFar:
			visualUpdateTimer -= Time.deltaTime;
			if (visualUpdateTimer < 0f)
			{
				visualUpdateTimer = 2f / 15f;
				shouldUpdateVisuals = true;
			}
			else
			{
				shouldUpdateVisuals = false;
			}
			break;
		}
	}

	private void OnBecameVisible()
	{
		parentModel.SetVisible(visible: true);
		isVisible = true;
		if ((bool)animatorToUpdateOnceWhenVisible)
		{
			animatorToUpdateOnceWhenVisible.Update(0f);
			animatorToUpdateOnceWhenVisible = null;
		}
	}

	private void OnBecameInvisible()
	{
		parentModel.SetVisible(visible: false);
		isVisible = false;
	}
}
