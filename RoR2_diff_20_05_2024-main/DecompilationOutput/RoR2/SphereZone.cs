using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SphereZone : BaseZoneBehavior, IZone
{
	[SyncVar]
	[Tooltip("The area of effect.")]
	public float radius;

	[Tooltip("The child range indicator object. Will be scaled to the radius.")]
	public Transform rangeIndicator;

	[Tooltip("The time it takes the range indicator to update")]
	public float indicatorSmoothTime = 0.2f;

	[Tooltip("If false, \"IsInBounds\" returns true when inside the sphere.  If true, outside the sphere is considered in bounds.")]
	public bool isInverted;

	private float rangeIndicatorScaleVelocity;

	public float Networkradius
	{
		get
		{
			return radius;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref radius, 1u);
		}
	}

	private void OnEnable()
	{
		if ((bool)rangeIndicator)
		{
			rangeIndicator.gameObject.SetActive(value: true);
		}
	}

	private void OnDisable()
	{
		if ((bool)rangeIndicator)
		{
			rangeIndicator.gameObject.SetActive(value: false);
		}
	}

	private void Update()
	{
		if ((bool)rangeIndicator)
		{
			float num = Mathf.SmoothDamp(rangeIndicator.localScale.x, radius, ref rangeIndicatorScaleVelocity, indicatorSmoothTime);
			rangeIndicator.localScale = new Vector3(num, num, num);
		}
	}

	public override bool IsInBounds(Vector3 position)
	{
		Vector3 vector = position - base.transform.position;
		if (isInverted)
		{
			return vector.sqrMagnitude > radius * radius;
		}
		return vector.sqrMagnitude <= radius * radius;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		bool flag = base.OnSerialize(writer, forceAll);
		if (forceAll)
		{
			writer.Write(radius);
			return true;
		}
		bool flag2 = false;
		if ((base.syncVarDirtyBits & (true ? 1u : 0u)) != 0)
		{
			if (!flag2)
			{
				writer.WritePackedUInt32(base.syncVarDirtyBits);
				flag2 = true;
			}
			writer.Write(radius);
		}
		if (!flag2)
		{
			writer.WritePackedUInt32(base.syncVarDirtyBits);
		}
		return flag2 || flag;
	}

	public override void OnDeserialize(NetworkReader reader, bool initialState)
	{
		base.OnDeserialize(reader, initialState);
		if (initialState)
		{
			radius = reader.ReadSingle();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			radius = reader.ReadSingle();
		}
	}

	public override void PreStartClient()
	{
		base.PreStartClient();
	}
}
