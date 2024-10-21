using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class TreebotSunBuffGranter : MonoBehaviour
{
	public Transform referenceTransform;

	public float raycastFrequency;

	public BuffDef buffDef;

	private const float raycastLength = 1000f;

	private float timer;

	private bool hadSunlight;

	private Light sun;

	private CharacterBody characterBody;

	private void Start()
	{
		FindSun();
		characterBody = GetComponent<CharacterBody>();
	}

	private void FixedUpdate()
	{
		timer -= Time.fixedDeltaTime;
		if (timer <= 0f)
		{
			timer = 1f / raycastFrequency;
			CheckSunlight();
		}
	}

	private void FindSun()
	{
		sun = RenderSettings.sun;
	}

	private void CheckSunlight()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		if (!sun)
		{
			FindSun();
			if (!sun)
			{
				return;
			}
		}
		bool flag = !Physics.Raycast(base.transform.position, -sun.transform.forward, 1000f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore);
		if ((bool)buffDef)
		{
			if (hadSunlight && !flag)
			{
				characterBody.RemoveBuff(buffDef.buffIndex);
			}
			else if (flag && !hadSunlight)
			{
				characterBody.AddBuff(buffDef.buffIndex);
			}
		}
		hadSunlight = flag;
	}
}
