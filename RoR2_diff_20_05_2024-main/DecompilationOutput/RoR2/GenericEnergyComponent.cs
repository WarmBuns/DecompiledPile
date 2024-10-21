using System.Runtime.InteropServices;
using Unity;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GenericEnergyComponent : NetworkBehaviour
{
	[SerializeField]
	private float _capacity = 1f;

	[SerializeField]
	private float _chargeRate = 1f;

	[SyncVar]
	private Run.FixedTimeStamp referenceTime;

	private float _internalChargeRate;

	public bool hasEffectiveAuthority { get; private set; }

	public float energy
	{
		get
		{
			if (internalChargeRate == 0f)
			{
				return (referenceTime - Run.FixedTimeStamp.zero) * capacity;
			}
			return Mathf.Clamp01(referenceTime.timeSince * internalChargeRate) * capacity;
		}
		set
		{
			float num = (capacity.Equals(0f) ? 0f : Mathf.Clamp01(value / capacity));
			if (internalChargeRate == 0f)
			{
				NetworkreferenceTime = Run.FixedTimeStamp.zero + num;
			}
			else
			{
				NetworkreferenceTime = Run.FixedTimeStamp.now - num / internalChargeRate;
			}
		}
	}

	public float normalizedEnergy
	{
		get
		{
			if (capacity == 0f)
			{
				return 0f;
			}
			return Mathf.Clamp01(energy / capacity);
		}
	}

	private float internalChargeRate
	{
		get
		{
			return _internalChargeRate;
		}
		set
		{
			if (_internalChargeRate != value)
			{
				float num = energy;
				_internalChargeRate = value;
				energy = num;
			}
		}
	}

	public float chargeRate
	{
		get
		{
			return _chargeRate;
		}
		set
		{
			if (_chargeRate != value)
			{
				float num = energy;
				_chargeRate = value;
				internalChargeRate = ((capacity != 0f) ? (_chargeRate / capacity) : 0f);
				energy = num;
			}
		}
	}

	public float normalizedChargeRate
	{
		get
		{
			if (capacity == 0f)
			{
				return 0f;
			}
			return chargeRate / capacity;
		}
		set
		{
			chargeRate = ((capacity != 0f) ? (value * capacity) : 0f);
		}
	}

	public float capacity
	{
		get
		{
			return _capacity;
		}
		set
		{
			if (value != _capacity)
			{
				float num = energy;
				_capacity = value;
				energy = num;
			}
		}
	}

	public Run.FixedTimeStamp NetworkreferenceTime
	{
		get
		{
			return referenceTime;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref referenceTime, 1u);
		}
	}

	private void UpdateAuthority()
	{
		hasEffectiveAuthority = Util.HasEffectiveAuthority(base.gameObject);
	}

	private void Awake()
	{
		UpdateAuthority();
		internalChargeRate = ((capacity != 0f) ? (chargeRate / capacity) : 0f);
		if (NetworkServer.active)
		{
			energy = capacity;
		}
	}

	public override void OnStartAuthority()
	{
		base.OnStartAuthority();
		UpdateAuthority();
	}

	public override void OnStopAuthority()
	{
		base.OnStopAuthority();
		UpdateAuthority();
	}

	private void OnEnable()
	{
		internalChargeRate = ((capacity != 0f) ? (chargeRate / capacity) : 0f);
	}

	private void OnDisable()
	{
		internalChargeRate = 0f;
	}

	[Server]
	public bool TakeEnergy(float amount)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean RoR2.GenericEnergyComponent::TakeEnergy(System.Single)' called on client");
			return false;
		}
		if (amount > energy)
		{
			return false;
		}
		energy -= amount;
		return true;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			GeneratedNetworkCode._WriteFixedTimeStamp_Run(writer, referenceTime);
			return true;
		}
		bool flag = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag = true;
			}
			GeneratedNetworkCode._WriteFixedTimeStamp_Run(writer, referenceTime);
		}
		if (!flag)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		if (initialState)
		{
			referenceTime = GeneratedNetworkCode._ReadFixedTimeStamp_Run(reader);
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			referenceTime = GeneratedNetworkCode._ReadFixedTimeStamp_Run(reader);
		}
	}

	public override void PreStartClient()
	{
	}
}
