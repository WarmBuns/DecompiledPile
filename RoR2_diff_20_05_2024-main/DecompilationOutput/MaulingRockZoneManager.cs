using System.Collections;
using System.Collections.Generic;
using RoR2;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

public class MaulingRockZoneManager : MonoBehaviour
{
	public List<GameObject> maulingRockProjectilePrefabs = new List<GameObject>();

	public Transform startZonePoint1;

	public Transform startZonePoint2;

	public Transform endZonePoint1;

	public Transform endZonePoint2;

	public static float baseDuration = 60f;

	public float knockbackForce = 10000f;

	private Vector3 vectorBetweenStartPoints = Vector3.zero;

	private Vector3 vectorBetweenEndPoints = Vector3.zero;

	private Vector3 MediumRockBump = Vector3.zero;

	private Vector3 LargeRockBump = Vector3.zero;

	private int salvoMaximumCount = 10;

	private float timeBetweenSalvoShotsLow = 0.1f;

	private float timeBetweenSalvoShotsHigh = 1f;

	private float timeBetweenSalvosLow = 3f;

	private float timeBetweenSalvosHigh = 5f;

	private int salvoRockCount;

	private int currentSalvoCount;

	private void Awake()
	{
		if (!NetworkServer.active)
		{
			base.enabled = false;
		}
	}

	private void Start()
	{
		vectorBetweenStartPoints = startZonePoint2.position - startZonePoint1.position;
		vectorBetweenEndPoints = endZonePoint2.position - endZonePoint1.position;
		FireSalvo();
	}

	private void FireSalvo()
	{
		salvoRockCount = Random.Range(0, salvoMaximumCount);
		currentSalvoCount = 0;
		FireRock();
	}

	private void FireRock()
	{
		GameObject gameObject = maulingRockProjectilePrefabs[Random.Range(0, maulingRockProjectilePrefabs.Count)];
		Vector3 vector = startZonePoint1.position + Random.Range(0f, 1f) * vectorBetweenStartPoints;
		Vector3 vector2 = endZonePoint1.position + Random.Range(0f, 1f) * vectorBetweenEndPoints;
		MaulingRock component = gameObject.GetComponent<MaulingRock>();
		float num = Random.Range(0f, 4f);
		num += component.verticalOffset;
		vector = new Vector3(vector.x, vector.y + num, vector.z);
		num = Random.Range(0f, 4f);
		num += component.verticalOffset;
		vector2 = new Vector3(vector2.x, vector2.y + num, vector2.z);
		Ray ray = new Ray(vector, vector2 - vector);
		FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
		fireProjectileInfo.projectilePrefab = gameObject;
		fireProjectileInfo.position = ray.origin;
		fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(ray.direction);
		fireProjectileInfo.owner = null;
		fireProjectileInfo.damage = component.damage * component.damageCoefficient;
		fireProjectileInfo.force = knockbackForce;
		fireProjectileInfo.crit = false;
		fireProjectileInfo.damageColorIndex = DamageColorIndex.Default;
		fireProjectileInfo.target = null;
		FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
		ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
		currentSalvoCount++;
		StartCoroutine(WaitToFireAnotherRock());
	}

	public IEnumerator WaitToFireAnotherSalvo()
	{
		float seconds = Random.Range(timeBetweenSalvosLow, timeBetweenSalvosHigh);
		yield return new WaitForSeconds(seconds);
		FireSalvo();
	}

	public IEnumerator WaitToFireAnotherRock()
	{
		float seconds = Random.Range(timeBetweenSalvoShotsLow, timeBetweenSalvoShotsHigh);
		if (currentSalvoCount >= salvoRockCount)
		{
			yield return null;
			StartCoroutine(WaitToFireAnotherSalvo());
		}
		else
		{
			yield return new WaitForSeconds(seconds);
			FireRock();
		}
	}
}
