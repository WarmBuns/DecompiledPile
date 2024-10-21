using System;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class MeteorController : NetworkBehaviour
{
	public GameObject meteorTest1;

	public GameObject meteorTest2;

	public GameObject meteorTest3;

	private ParticleSystem meteorTest1ps;

	private ParticleSystem meteorTest2ps;

	private ParticleSystem meteorTest3ps;

	public ShakeEmitter shakeEmitter;

	private bool meteorTimerOn;

	private float meteorTimer;

	private float meteorDuration = 30f;

	private void Awake()
	{
		meteorTest1ps = meteorTest1.GetComponent<ParticleSystem>();
		meteorTest2ps = meteorTest2.GetComponent<ParticleSystem>();
		meteorTest3ps = meteorTest3.GetComponent<ParticleSystem>();
		meteorTest1.SetActive(value: false);
		meteorTest2.SetActive(value: false);
		meteorTest3.SetActive(value: false);
	}

	private void Start()
	{
	}

	private void ActivateMeteors(bool value)
	{
		meteorTest1.SetActive(value);
		meteorTest2.SetActive(value);
		meteorTest3.SetActive(value);
	}

	private void Update()
	{
		if ((double)Run.instance.GetRunStopwatch() - Math.Floor(Run.instance.GetRunStopwatch() * (1f / 60f)) * 60.0 <= 0.1 && !meteorTimerOn)
		{
			meteorTimerOn = true;
			ActivateMeteors(value: true);
		}
		if (meteorTimerOn)
		{
			meteorTimer += Time.fixedDeltaTime;
		}
		if (meteorTimer >= meteorDuration)
		{
			meteorTimerOn = false;
			meteorTimer = 0f;
			ActivateMeteors(value: false);
		}
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool result = default(bool);
		return result;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
	}

	public override void PreStartClient()
	{
	}
}
