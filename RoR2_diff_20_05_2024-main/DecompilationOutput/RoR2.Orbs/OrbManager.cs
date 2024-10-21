using System.Collections.Generic;
using UnityEngine;

namespace RoR2.Orbs;

public class OrbManager : MonoBehaviour
{
	private List<Orb> travelingOrbs = new List<Orb>();

	private float nextOrbArrival = float.PositiveInfinity;

	private List<IOrbFixedUpdateBehavior> orbsWithFixedUpdateBehavior = new List<IOrbFixedUpdateBehavior>();

	public static OrbManager instance { get; private set; }

	public float time { get; private set; }

	private void OnEnable()
	{
		if (!instance)
		{
			instance = this;
			return;
		}
		Debug.LogErrorFormat(this, "Duplicate instance of singleton class {0}. Only one should exist at a time.", GetType().Name);
	}

	private void OnDisable()
	{
		if (instance == this)
		{
			instance = null;
		}
	}

	private void FixedUpdate()
	{
		time += Time.fixedDeltaTime;
		for (int i = 0; i < orbsWithFixedUpdateBehavior.Count; i++)
		{
			orbsWithFixedUpdateBehavior[i].FixedUpdate();
		}
		if (!(nextOrbArrival <= time))
		{
			return;
		}
		nextOrbArrival = float.PositiveInfinity;
		for (int num = travelingOrbs.Count - 1; num >= 0; num--)
		{
			Orb orb = travelingOrbs[num];
			if (orb.arrivalTime <= time)
			{
				travelingOrbs.RemoveAt(num);
				if (orb is IOrbFixedUpdateBehavior item)
				{
					orbsWithFixedUpdateBehavior.Remove(item);
				}
				orb.OnArrival();
			}
			else if (nextOrbArrival > orb.arrivalTime)
			{
				nextOrbArrival = orb.arrivalTime;
			}
		}
	}

	public void ForceImmediateArrival(Orb orb)
	{
		orb.OnArrival();
		travelingOrbs.Remove(orb);
	}

	public void AddOrb(Orb orb)
	{
		orb.Begin();
		orb.arrivalTime = time + orb.duration;
		travelingOrbs.Add(orb);
		if (orb is IOrbFixedUpdateBehavior item)
		{
			orbsWithFixedUpdateBehavior.Add(item);
		}
		if (nextOrbArrival > orb.arrivalTime)
		{
			nextOrbArrival = orb.arrivalTime;
		}
	}
}
