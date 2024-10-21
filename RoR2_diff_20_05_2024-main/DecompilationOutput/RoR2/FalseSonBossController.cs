using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class FalseSonBossController : MonoBehaviour
{
	public CharacterBody characterBody;

	public Animator characterAnimator;

	public ModelLocator modelLocator;

	public GameObject lunarSpikePrefab;

	public ChildLocator childLocator;

	public HealthComponent healthComponent;

	public GameObject masterFSPrefab;

	public Transform lunarSpike;

	public Transform lunarSpikeLocation;

	private float maxHealth;

	private float currentHealth;

	private bool health75;

	public bool health50;

	public bool lunarSpikeActive;

	private float testTimeGate = 2f;

	private float spikeTimer;

	public int swingCounter;

	public bool firedLunarSpike;

	private void OnEnable()
	{
		lunarSpikeLocation = childLocator.FindChild("LunarSpike");
	}

	private void Start()
	{
		maxHealth = healthComponent.health;
	}

	private void FixedUpdate()
	{
		if (healthComponent.health <= maxHealth * 0.5f && !health50)
		{
			PlayAnimation("FullBody, Override", "OverloadErruption", "OverloadErruption.playbackRate", 6f);
			spikeTimer += Time.deltaTime;
			if (spikeTimer > 2.8f)
			{
				SpawnLunarSpike();
			}
			health50 = true;
		}
		if (!lunarSpikeActive)
		{
			return;
		}
		if (characterBody.GetBuffCount(RoR2Content.Buffs.HiddenInvincibility) < 1)
		{
			characterBody.AddBuff(RoR2Content.Buffs.HiddenInvincibility);
		}
		if (!lunarSpike)
		{
			characterBody.RemoveBuff(RoR2Content.Buffs.HiddenInvincibility);
			characterBody.RemoveBuff(DLC2Content.Buffs.CorruptionFesters);
			if (health50)
			{
				characterBody.AddTimedBuff(DLC2Content.Buffs.WeakenedBeating, 60f);
			}
			lunarSpikeActive = false;
		}
	}

	protected void PlayAnimation(string layerName, string animationStateName, string playbackRateParam, float duration)
	{
		if (duration <= 0f)
		{
			Debug.LogWarningFormat("EntityState.PlayAnimation: Zero duration is not allowed. type={0}", GetType().Name);
			return;
		}
		Animator modelAnimator = GetModelAnimator();
		if ((bool)modelAnimator)
		{
			PlayAnimationOnAnimator(modelAnimator, layerName, animationStateName, playbackRateParam, duration);
		}
	}

	protected static void PlayAnimationOnAnimator(Animator modelAnimator, string layerName, string animationStateName, string playbackRateParam, float duration)
	{
		modelAnimator.speed = 1f;
		modelAnimator.Update(0f);
		int layerIndex = modelAnimator.GetLayerIndex(layerName);
		if (layerIndex >= 0)
		{
			modelAnimator.SetFloat(playbackRateParam, 1f);
			modelAnimator.PlayInFixedTime(animationStateName, layerIndex, 0f);
			modelAnimator.Update(0f);
			float length = modelAnimator.GetCurrentAnimatorStateInfo(layerIndex).length;
			modelAnimator.SetFloat(playbackRateParam, length / duration);
		}
	}

	protected Animator GetModelAnimator()
	{
		if ((bool)modelLocator && (bool)modelLocator.modelTransform)
		{
			return modelLocator.modelTransform.GetComponent<Animator>();
		}
		return null;
	}

	public void SpawnLunarSpike()
	{
		MasterSpawnSlotController component = GetComponent<MasterSpawnSlotController>();
		if (NetworkServer.active && (bool)component)
		{
			component.SpawnRandomOpen(1, Run.instance.stageRng, base.gameObject);
		}
		lunarSpike = lunarSpikeLocation.transform.GetChild(0);
		lunarSpike.transform.rotation = lunarSpikeLocation.rotation;
		lunarSpike.GetComponent<CharacterBody>().baseMaxHealth = maxHealth * 0.2f;
		DamageInfo damageInfo = new DamageInfo
		{
			damage = healthComponent.health * 0.1f
		};
		healthComponent.TakeDamage(damageInfo);
		characterBody.AddBuff(DLC2Content.Buffs.CorruptionFesters);
		lunarSpikeActive = true;
		firedLunarSpike = true;
	}
}
