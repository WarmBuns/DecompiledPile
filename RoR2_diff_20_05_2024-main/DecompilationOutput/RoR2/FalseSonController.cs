using RoR2.Skills;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

[RequireComponent(typeof(CharacterBody))]
public class FalseSonController : NetworkBehaviour
{
	public CharacterBody characterBody;

	public SkillDef falseSonBodyTheTamperedHeart;

	public SkillDef tempAltDefName;

	public GenericSkill passiveSkillSlot;

	[SerializeField]
	private float tamperedHeartRegen;

	[SerializeField]
	private float tamperedHeartMoveSpeed;

	[SerializeField]
	private float tamperedHeartArmor;

	[SerializeField]
	private float tamperedHeartAttackSpeed;

	private bool tamperedHeart;

	private SkillLocator skillLocator;

	private int maxSecondaryStock;

	private int oldMaxSecondaryStock;

	private int secondaryStock;

	private int oldSecondaryStock;

	private int extraShardFromHealth;

	private int oldExtraShardFromHealth;

	private float maxHealth;

	private float oldMaxHealth;

	private int bonusSecondarySkillCountFromItems;

	private int oldBonusSecondarySkillCountFromItems;

	private bool alreadyResetMaxSecondaryStockCount;

	private int lunarSpikeSkillIndex;

	private int laserBurstSkillIndex;

	private GenericSkill lunarSpikeSkill;

	private GenericSkill laserSpecialSkill;

	private bool cachedEffectiveAuthority;

	private NetworkIdentity networkIdentity;

	private float clubSwingAltSecondaryTimestamp;

	private static int kCmdCmdUpdateAmmoAmounts;

	private bool secondarySkillIsFalseSonDefault => lunarSpikeSkill.baseSkill.skillIndex == lunarSpikeSkillIndex;

	private bool sendAmmoUpdatesToServer
	{
		get
		{
			if (cachedEffectiveAuthority)
			{
				return !NetworkServer.active;
			}
			return false;
		}
	}

	private void OnEnable()
	{
		skillLocator = GetComponent<SkillLocator>();
		networkIdentity = skillLocator.GetComponent<NetworkIdentity>();
		lunarSpikeSkill = skillLocator.GetSkill(SkillSlot.Secondary);
		laserSpecialSkill = skillLocator.GetSkill(SkillSlot.Special);
		CacheEffectiveAuthority();
		oldMaxSecondaryStock = GetMaxSecondaryStock();
		oldSecondaryStock = maxSecondaryStock;
		lunarSpikeSkillIndex = SkillCatalog.FindSkillIndexByName("LunarSpikes");
		laserBurstSkillIndex = SkillCatalog.FindSkillIndexByName("LaserBurst");
	}

	private void Update()
	{
		bool flag = false;
		CacheEffectiveAuthority();
		if (passiveSkillSlot.skillDef == falseSonBodyTheTamperedHeart)
		{
			if (!secondarySkillIsFalseSonDefault && !alreadyResetMaxSecondaryStockCount)
			{
				characterBody.extraSecondaryFromSkill = 0;
				oldExtraShardFromHealth = 0;
				UpdateStats();
				alreadyResetMaxSecondaryStockCount = true;
			}
			maxHealth = characterBody.maxHealth;
			maxSecondaryStock = GetMaxSecondaryStock();
			if ((maxHealth != oldMaxHealth && characterBody.maxBonusHealth > 0f) || oldMaxSecondaryStock != maxSecondaryStock || (!secondarySkillIsFalseSonDefault ^ alreadyResetMaxSecondaryStockCount))
			{
				if (secondarySkillIsFalseSonDefault)
				{
					IncreaseStockCount();
					flag = true;
					alreadyResetMaxSecondaryStockCount = false;
				}
				oldMaxHealth = maxHealth;
				oldMaxSecondaryStock = maxSecondaryStock;
			}
			secondaryStock = GetSecondaryStock();
			if (secondaryStock != oldSecondaryStock || flag)
			{
				CalcTamperedHeartStats();
				flag = true;
			}
		}
		if (sendAmmoUpdatesToServer && flag)
		{
			CallCmdUpdateAmmoAmounts(secondaryStock, maxSecondaryStock);
		}
	}

	[Command]
	public void CmdUpdateAmmoAmounts(int _secondaryStock, int _maxSecondaryStock)
	{
		maxSecondaryStock = _maxSecondaryStock;
		secondaryStock = _secondaryStock;
		CalcTamperedHeartStats();
	}

	public int GetTotalSpikeCount()
	{
		int num = 0;
		float num2 = 15f;
		float num3 = 5f;
		float num4 = characterBody.maxBonusHealth - (characterBody.baseMaxHealth + characterBody.levelMaxHealth * (float)((int)characterBody.level - 1));
		int num5 = 10;
		while (num4 > num2)
		{
			num4 -= num2;
			if (num4 < 0f)
			{
				break;
			}
			num++;
			if (num + 4 > num5)
			{
				num5 += 10;
				num3 = 5f + 5f * (float)(num / 10);
			}
			num2 = 15f + num3 * (float)num;
		}
		return num;
	}

	private int GetMaxSecondaryStock()
	{
		if (cachedEffectiveAuthority)
		{
			return lunarSpikeSkill.GetBaseMaxStock();
		}
		return maxSecondaryStock;
	}

	private int GetSecondaryStock()
	{
		if (cachedEffectiveAuthority)
		{
			return lunarSpikeSkill.GetBaseStock();
		}
		return secondaryStock;
	}

	private void UpdateStats()
	{
		characterBody.RecalculateStats();
	}

	private void CalcTamperedHeartStats()
	{
		int num = maxSecondaryStock - secondaryStock;
		if (num < 0)
		{
			num = 0;
		}
		float heartRegen = tamperedHeartRegen * (float)num;
		float heartMSpeed = tamperedHeartMoveSpeed * (float)num;
		float attackSpeed = tamperedHeartAttackSpeed * (float)secondaryStock;
		float armor = tamperedHeartArmor * (float)secondaryStock;
		float heartDamage = 0f;
		characterBody.ApplyTamperedHeartPassive(heartRegen, heartMSpeed, heartDamage, armor, attackSpeed);
		UpdateStats();
		oldSecondaryStock = secondaryStock;
	}

	private float CalculateHyperbolicStatBonuses(float statCalc, float spikesToCalculate)
	{
		float num = 0f;
		if (spikesToCalculate > 0f)
		{
			if (spikesToCalculate <= 4f)
			{
				num += spikesToCalculate * statCalc;
			}
			else if (spikesToCalculate > 4f)
			{
				num += statCalc * 4f + (1f - 1f / (statCalc * (spikesToCalculate - 4f) + 1f));
			}
		}
		return num;
	}

	private float RegenAndMoveSpeedCalc(float statCalc)
	{
		if (secondaryStock < maxSecondaryStock)
		{
			return statCalc * (float)(maxSecondaryStock - secondaryStock);
		}
		return 0f;
	}

	private void IncreaseStockCount()
	{
		extraShardFromHealth = GetTotalSpikeCount();
		if (extraShardFromHealth != oldExtraShardFromHealth)
		{
			if (extraShardFromHealth < 0)
			{
				extraShardFromHealth = 0;
			}
			characterBody.extraSecondaryFromSkill = extraShardFromHealth;
			int num = extraShardFromHealth / 4;
			if (laserSpecialSkill.skillDef.skillIndex == laserBurstSkillIndex && num > 0)
			{
				characterBody.extraSpecialFromSkill = num;
			}
			UpdateStats();
			oldExtraShardFromHealth = extraShardFromHealth;
		}
		maxSecondaryStock = GetMaxSecondaryStock();
	}

	public float GetClubSwingAltSecondaryTimestamp()
	{
		return clubSwingAltSecondaryTimestamp;
	}

	public void SetClubSwingAltSecondaryTimestamp()
	{
		clubSwingAltSecondaryTimestamp = Time.time;
	}

	private void CacheEffectiveAuthority()
	{
		cachedEffectiveAuthority = Util.HasEffectiveAuthority(networkIdentity);
	}

	private void UNetVersion()
	{
	}

	protected static void InvokeCmdCmdUpdateAmmoAmounts(NetworkBehaviour obj, NetworkReader reader)
	{
		if (!NetworkServer.active)
		{
			Debug.LogError("Command CmdUpdateAmmoAmounts called on client.");
		}
		else
		{
			((FalseSonController)obj).CmdUpdateAmmoAmounts((int)reader.ReadPackedUInt32(), (int)reader.ReadPackedUInt32());
		}
	}

	public void CallCmdUpdateAmmoAmounts(int _secondaryStock, int _maxSecondaryStock)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("Command function CmdUpdateAmmoAmounts called on server.");
			return;
		}
		if (base.isServer)
		{
			CmdUpdateAmmoAmounts(_secondaryStock, _maxSecondaryStock);
			return;
		}
		NetworkWriter networkWriter = new NetworkWriter();
		networkWriter.Write((short)0);
		networkWriter.Write((short)5);
		networkWriter.WritePackedUInt32((uint)kCmdCmdUpdateAmmoAmounts);
		networkWriter.Write(GetComponent<NetworkIdentity>().netId);
		networkWriter.WritePackedUInt32((uint)_secondaryStock);
		networkWriter.WritePackedUInt32((uint)_maxSecondaryStock);
		SendCommandInternal(networkWriter, 0, "CmdUpdateAmmoAmounts");
	}

	static FalseSonController()
	{
		kCmdCmdUpdateAmmoAmounts = -634793383;
		NetworkBehaviour.RegisterCommandDelegate(typeof(FalseSonController), kCmdCmdUpdateAmmoAmounts, InvokeCmdCmdUpdateAmmoAmounts);
		NetworkCRC.RegisterBehaviour("FalseSonController", 0);
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
