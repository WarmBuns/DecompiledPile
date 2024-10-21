using UnityEngine;

namespace RoR2.Projectile;

public struct FireProjectileInfo
{
	public GameObject projectilePrefab;

	public Vector3 position;

	public Quaternion rotation;

	public GameObject owner;

	public GameObject target;

	public bool useSpeedOverride;

	private float _speedOverride;

	public bool useFuseOverride;

	private float _fuseOverride;

	public float damage;

	public float force;

	public bool crit;

	public DamageColorIndex damageColorIndex;

	public ProcChainMask procChainMask;

	public DamageTypeCombo? damageTypeOverride;

	public float speedOverride
	{
		get
		{
			if (!useSpeedOverride)
			{
				return -1f;
			}
			return _speedOverride;
		}
		set
		{
			useSpeedOverride = value != -1f;
			_speedOverride = value;
		}
	}

	public float fuseOverride
	{
		get
		{
			if (!useFuseOverride)
			{
				return -1f;
			}
			return _fuseOverride;
		}
		set
		{
			useFuseOverride = value != -1f;
			_fuseOverride = value;
		}
	}
}
