using RoR2.Orbs;
using UnityEngine;

public class OrbEffectSingleton : MonoBehaviour
{
	public static OrbEffectSingleton instance;

	public static OrbEffect[] orbEffectArray;

	public static int numPnts;

	private void Awake()
	{
		if (instance != null)
		{
			Object.DestroyImmediate(this);
			return;
		}
		instance = this;
		orbEffectArray = new OrbEffect[50];
	}

	public static void RegisterInstance(OrbEffect newOrbEffect)
	{
		if (orbEffectArray == null || orbEffectArray.Length == numPnts)
		{
			OrbEffect[] array = new OrbEffect[(int)((float)(numPnts + 1) * 1.5f)];
			orbEffectArray.CopyTo(array, 0);
			orbEffectArray = array;
		}
		newOrbEffect.singletonArrayIndex = numPnts;
		orbEffectArray[numPnts] = newOrbEffect;
		numPnts++;
	}

	public static void UnregisterInstance(OrbEffect orbToDisable)
	{
		int singletonArrayIndex = orbToDisable.singletonArrayIndex;
		if (singletonArrayIndex != numPnts - 1)
		{
			orbEffectArray[singletonArrayIndex] = orbEffectArray[numPnts - 1];
			orbEffectArray[singletonArrayIndex].singletonArrayIndex = singletonArrayIndex;
		}
		orbToDisable.singletonArrayIndex = -1;
		numPnts--;
	}

	public void Update()
	{
		for (int i = 0; i < numPnts; i++)
		{
			orbEffectArray[i].UpdateOrb(Time.deltaTime);
		}
	}
}
