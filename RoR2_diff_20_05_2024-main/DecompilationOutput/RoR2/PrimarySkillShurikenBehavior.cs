using RoR2.Projectile;
using UnityEngine;

namespace RoR2;

public class PrimarySkillShurikenBehavior : CharacterBody.ItemBehavior
{
	private const float minSpreadDegrees = 0f;

	private const float rangeSpreadDegrees = 1f;

	private const int numShurikensPerStack = 1;

	private const int numShurikensBase = 2;

	private const string projectilePrefabPath = "Prefabs/Projectiles/ShurikenProjectile";

	private const float totalReloadTime = 10f;

	private const float damageCoefficientBase = 3f;

	private const float damageCoefficientPerStack = 1f;

	private const float force = 0f;

	private SkillLocator skillLocator;

	private float reloadTimer;

	private GameObject projectilePrefab;

	private InputBankTest inputBank;

	private void Awake()
	{
		base.enabled = false;
	}

	private void Start()
	{
		projectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ShurikenProjectile");
	}

	private void OnEnable()
	{
		if ((bool)body)
		{
			body.onSkillActivatedServer += OnSkillActivated;
			skillLocator = body.GetComponent<SkillLocator>();
			inputBank = body.GetComponent<InputBankTest>();
		}
	}

	private void OnDisable()
	{
		if ((bool)body)
		{
			body.onSkillActivatedServer -= OnSkillActivated;
			while (body.HasBuff(DLC1Content.Buffs.PrimarySkillShurikenBuff))
			{
				body.RemoveBuff(DLC1Content.Buffs.PrimarySkillShurikenBuff);
			}
		}
		inputBank = null;
		skillLocator = null;
	}

	private void OnSkillActivated(GenericSkill skill)
	{
		if ((object)skillLocator?.primary == skill && body.GetBuffCount(DLC1Content.Buffs.PrimarySkillShurikenBuff) > 0)
		{
			body.RemoveBuff(DLC1Content.Buffs.PrimarySkillShurikenBuff);
			FireShuriken();
		}
	}

	private void FixedUpdate()
	{
		int num = stack + 2;
		if (body.GetBuffCount(DLC1Content.Buffs.PrimarySkillShurikenBuff) < num)
		{
			float num2 = 10f / (float)num;
			reloadTimer += Time.fixedDeltaTime;
			while (reloadTimer > num2 && body.GetBuffCount(DLC1Content.Buffs.PrimarySkillShurikenBuff) < num)
			{
				body.AddBuff(DLC1Content.Buffs.PrimarySkillShurikenBuff);
				reloadTimer -= num2;
			}
		}
	}

	private void FireShuriken()
	{
		Ray aimRay = GetAimRay();
		ProjectileManager.instance.FireProjectile(projectilePrefab, aimRay.origin, Util.QuaternionSafeLookRotation(aimRay.direction) * GetRandomRollPitch(), base.gameObject, body.damage * (3f + 1f * (float)stack), 0f, Util.CheckRoll(body.crit, body.master), DamageColorIndex.Item);
	}

	private Ray GetAimRay()
	{
		if ((bool)inputBank)
		{
			return new Ray(inputBank.aimOrigin, inputBank.aimDirection);
		}
		return new Ray(base.transform.position, base.transform.forward);
	}

	protected Quaternion GetRandomRollPitch()
	{
		Quaternion quaternion = Quaternion.AngleAxis(Random.Range(0, 360), Vector3.forward);
		Quaternion quaternion2 = Quaternion.AngleAxis(0f + Random.Range(0f, 1f), Vector3.left);
		return quaternion * quaternion2;
	}
}
