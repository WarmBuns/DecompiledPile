using System.Collections.Generic;
using UnityEngine;

namespace RoR2;

public class PullNearby : MonoBehaviour
{
	public float pullRadius;

	public float pullDuration;

	public AnimationCurve pullStrengthCurve;

	public bool pullOnStart;

	public int maximumPullCount = int.MaxValue;

	private List<CharacterBody> victimBodyList = new List<CharacterBody>();

	private bool pulling;

	private TeamFilter teamFilter;

	private float fixedAge;

	private void Start()
	{
		teamFilter = GetComponent<TeamFilter>();
		if (pullOnStart)
		{
			InitializePull();
		}
	}

	private void FixedUpdate()
	{
		fixedAge += Time.fixedDeltaTime;
		if (!(fixedAge > pullDuration))
		{
			UpdatePull(Time.fixedDeltaTime);
		}
	}

	private void UpdatePull(float deltaTime)
	{
		if (!pulling)
		{
			return;
		}
		for (int i = 0; i < victimBodyList.Count; i++)
		{
			CharacterBody characterBody = victimBodyList[i];
			Vector3 vector = base.transform.position - characterBody.corePosition;
			float num = pullStrengthCurve.Evaluate(vector.magnitude / pullRadius);
			Vector3 vector2 = vector.normalized * num * deltaTime;
			CharacterMotor component = characterBody.GetComponent<CharacterMotor>();
			if ((bool)component)
			{
				component.rootMotion += vector2;
				continue;
			}
			Rigidbody component2 = characterBody.GetComponent<Rigidbody>();
			if ((bool)component2)
			{
				component2.velocity += vector2;
			}
		}
	}

	public void InitializePull()
	{
		if (pulling)
		{
			return;
		}
		pulling = true;
		Collider[] colliders;
		int num = HGPhysics.OverlapSphere(out colliders, base.transform.position, pullRadius, LayerIndex.CommonMasks.characterBodiesOrDefault);
		int i = 0;
		int num2 = 0;
		for (; i < num; i++)
		{
			if (num2 >= maximumPullCount)
			{
				break;
			}
			HealthComponent component = colliders[i].GetComponent<HealthComponent>();
			if ((bool)component)
			{
				TeamComponent component2 = component.GetComponent<TeamComponent>();
				bool flag = false;
				if ((bool)component2 && (bool)teamFilter)
				{
					flag = component2.teamIndex == teamFilter.teamIndex;
				}
				if (!flag)
				{
					AddToList(component.gameObject);
					num2++;
				}
			}
		}
		HGPhysics.ReturnResults(colliders);
	}

	private void AddToList(GameObject affectedObject)
	{
		CharacterBody component = affectedObject.GetComponent<CharacterBody>();
		if (!victimBodyList.Contains(component))
		{
			victimBodyList.Add(component);
		}
	}
}
