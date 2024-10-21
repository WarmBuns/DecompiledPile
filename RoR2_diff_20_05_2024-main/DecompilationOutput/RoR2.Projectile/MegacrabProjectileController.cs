using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2.Projectile;

public class MegacrabProjectileController : MonoBehaviour
{
	public enum MegacrabProjectileType
	{
		White,
		Black,
		Count
	}

	[Header("Cached Properties")]
	public ProjectileController projectileController;

	public ProjectileDamage projectileDamage;

	public Rigidbody projectileRigidbody;

	public MegacrabProjectileType megacrabProjectileType;

	[Header("Black Properties")]
	public AnimationCurve blackForceFalloffCurve;

	public float whiteToBlackTransformationRadius;

	public GameObject whiteToBlackTransformedProjectile;

	[Header("White Properties")]
	public AnimationCurve whiteForceFalloffCurve;

	public float minimumWhiteForceMagnitude;

	public float maximumWhiteForceMagnitude;

	public float whiteMinimumForceToFullyRotate;

	public float whiteScaleSmoothtimeMin = 0.1f;

	public float whiteScaleSmoothtimeMax = 0.1f;

	[Range(0f, 1f)]
	public float whitePoisson;

	public static List<MegacrabProjectileController> whiteProjectileList = new List<MegacrabProjectileController>();

	public static List<MegacrabProjectileController> blackProjectileList = new List<MegacrabProjectileController>();

	private float whiteScaleSmoothtime;

	private Vector3 whiteScaleVelocity;

	private float whiteRotationVelocity;

	private void OnEnable()
	{
		switch (megacrabProjectileType)
		{
		case MegacrabProjectileType.White:
			whiteProjectileList.Add(this);
			break;
		case MegacrabProjectileType.Black:
			blackProjectileList.Add(this);
			break;
		}
	}

	private void OnDisable()
	{
		switch (megacrabProjectileType)
		{
		case MegacrabProjectileType.White:
			whiteProjectileList.Remove(this);
			break;
		case MegacrabProjectileType.Black:
			blackProjectileList.Remove(this);
			break;
		}
	}

	private void Start()
	{
		whiteScaleSmoothtime = Random.Range(whiteScaleSmoothtimeMin, whiteScaleSmoothtimeMax);
	}

	private void FixedUpdate()
	{
		Vector3 zero = Vector3.zero;
		Vector3 position = base.transform.position;
		switch (megacrabProjectileType)
		{
		case MegacrabProjectileType.White:
		{
			for (int j = 0; j < blackProjectileList.Count; j++)
			{
				Vector3 vector3 = blackProjectileList[j].transform.position - position;
				float magnitude2 = vector3.magnitude;
				Vector3 vector4 = whiteForceFalloffCurve.Evaluate(magnitude2) * vector3.normalized;
				zero += vector4;
			}
			Quaternion rotation = base.transform.rotation;
			Quaternion target = base.transform.rotation;
			Vector3 localScale = base.transform.localScale;
			Vector3 target2 = Vector3.one;
			if (zero.magnitude > minimumWhiteForceMagnitude)
			{
				Quaternion b = Quaternion.LookRotation(zero);
				float num = Mathf.Min(maximumWhiteForceMagnitude, zero.magnitude);
				float num2 = 1f / Mathf.Lerp(num, 1f, whitePoisson);
				target2 = new Vector3(num2, num2, num);
				target = Quaternion.Slerp(rotation, b, whiteMinimumForceToFullyRotate);
			}
			base.transform.localScale = Vector3.SmoothDamp(localScale, target2, ref whiteScaleVelocity, whiteScaleSmoothtime);
			base.transform.rotation = Util.SmoothDampQuaternion(rotation, target, ref whiteRotationVelocity, whiteScaleSmoothtime);
			break;
		}
		case MegacrabProjectileType.Black:
		{
			for (int i = 0; i < whiteProjectileList.Count; i++)
			{
				Vector3 vector = whiteProjectileList[i].transform.position - position;
				float magnitude = vector.magnitude;
				Vector3 vector2 = blackForceFalloffCurve.Evaluate(magnitude) * vector.normalized;
				zero += vector2;
			}
			projectileRigidbody.AddForce(zero);
			break;
		}
		}
	}

	public void OnDestroy()
	{
		if (!NetworkServer.active || megacrabProjectileType != MegacrabProjectileType.Black)
		{
			return;
		}
		Vector3 position = base.transform.position;
		for (int i = 0; i < whiteProjectileList.Count; i++)
		{
			MegacrabProjectileController megacrabProjectileController = whiteProjectileList[i];
			if ((whiteProjectileList[i].transform.position - position).magnitude <= whiteToBlackTransformationRadius)
			{
				ProjectileExplosion component = megacrabProjectileController.GetComponent<ProjectileExplosion>();
				if (component.GetAlive())
				{
					ProjectileManager.instance.FireProjectile(whiteToBlackTransformedProjectile, megacrabProjectileController.transform.position, Quaternion.identity, projectileController.owner, projectileDamage.damage, 0f, projectileDamage.crit);
					component.SetAlive(newAlive: false);
				}
			}
		}
	}
}
