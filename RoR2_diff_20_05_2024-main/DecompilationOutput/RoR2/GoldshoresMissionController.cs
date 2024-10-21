using System.Collections.Generic;
using EntityStates.Interactables.GoldBeacon;
using RoR2.ExpansionManagement;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GoldshoresMissionController : MonoBehaviour
{
	public Xoroshiro128Plus rng;

	public EntityStateMachine entityStateMachine;

	public GameObject beginTransitionIntoBossFightEffect;

	public GameObject exitTransitionIntoBossFightEffect;

	public Transform bossSpawnPosition;

	[SerializeField]
	public ExpansionDef requiredExpansion;

	public List<GameObject> beaconInstanceList = new List<GameObject>();

	public int beaconsRequiredToSpawnBoss;

	public int beaconsToSpawnOnMap;

	public InteractableSpawnCard beaconSpawnCard;

	public static GoldshoresMissionController instance { get; private set; }

	public int beaconsActive => Ready.count;

	public int beaconCount => Ready.count + NotReady.count;

	private void OnEnable()
	{
		instance = SingletonHelper.Assign(instance, this);
	}

	private void OnDisable()
	{
		instance = SingletonHelper.Unassign(instance, this);
	}

	private void Awake()
	{
		if (NetworkServer.active)
		{
			rng = new Xoroshiro128Plus(Run.instance.stageRng.nextUint);
		}
		beginTransitionIntoBossFightEffect.SetActive(value: false);
		exitTransitionIntoBossFightEffect.SetActive(value: false);
	}

	public void SpawnBeacons()
	{
		if (!NetworkServer.active)
		{
			return;
		}
		for (int i = 0; i < beaconsToSpawnOnMap; i++)
		{
			GameObject gameObject = DirectorCore.instance.TrySpawnObject(new DirectorSpawnRequest(beaconSpawnCard, new DirectorPlacementRule
			{
				placementMode = DirectorPlacementRule.PlacementMode.Random
			}, rng));
			if ((bool)gameObject)
			{
				beaconInstanceList.Add(gameObject);
			}
		}
		beaconsToSpawnOnMap = beaconInstanceList.Count;
	}

	public void BeginTransitionIntoBossfight()
	{
		beginTransitionIntoBossFightEffect.SetActive(value: true);
		exitTransitionIntoBossFightEffect.SetActive(value: false);
	}

	public void ExitTransitionIntoBossfight()
	{
		beginTransitionIntoBossFightEffect.SetActive(value: false);
		exitTransitionIntoBossFightEffect.SetActive(value: true);
	}
}
