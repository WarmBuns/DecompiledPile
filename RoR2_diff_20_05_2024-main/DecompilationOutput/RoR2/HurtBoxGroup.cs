using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace RoR2;

public class HurtBoxGroup : MonoBehaviour, ILifeBehavior
{
	public class VolumeDistribution
	{
		private HurtBox[] hurtBoxes;

		public float totalVolume { get; private set; }

		public Vector3 randomVolumePoint
		{
			get
			{
				float num = UnityEngine.Random.Range(0f, totalVolume);
				HurtBox hurtBox = hurtBoxes[0];
				float num2 = 0f;
				for (int i = 0; i < hurtBoxes.Length; i++)
				{
					num2 += hurtBoxes[i].volume;
					if (num2 <= num)
					{
						hurtBox = hurtBoxes[i];
						break;
					}
				}
				return hurtBox.randomVolumePoint;
			}
		}

		public VolumeDistribution(HurtBox[] hurtBoxes)
		{
			totalVolume = 0f;
			for (int i = 0; i < hurtBoxes.Length; i++)
			{
				totalVolume += hurtBoxes[i].volume;
			}
			this.hurtBoxes = (HurtBox[])hurtBoxes.Clone();
		}
	}

	[Tooltip("The hurtboxes in this group. This really shouldn't be set manually.")]
	public HurtBox[] hurtBoxes;

	[Tooltip("The most important hurtbox in this group, usually a good center-of-mass target like the chest.")]
	public HurtBox mainHurtBox;

	[HideInInspector]
	public int bullseyeCount;

	private int _hurtBoxesDeactivatorCounter;

	public int hurtBoxesDeactivatorCounter
	{
		get
		{
			return _hurtBoxesDeactivatorCounter;
		}
		set
		{
			bool num = _hurtBoxesDeactivatorCounter <= 0;
			bool flag = value <= 0;
			_hurtBoxesDeactivatorCounter = value;
			if (num != flag)
			{
				SetHurtboxesActive(flag);
			}
		}
	}

	public void OnDeathStart()
	{
		int num = hurtBoxesDeactivatorCounter + 1;
		hurtBoxesDeactivatorCounter = num;
	}

	private void SetHurtboxesActive(bool active)
	{
		for (int i = 0; i < hurtBoxes.Length; i++)
		{
			hurtBoxes[i].gameObject.SetActive(active);
		}
	}

	public void OnValidate()
	{
		int num = 0;
		if (hurtBoxes == null)
		{
			hurtBoxes = Array.Empty<HurtBox>();
		}
		for (short num2 = 0; num2 < hurtBoxes.Length; num2++)
		{
			if (!hurtBoxes[num2])
			{
				Debug.LogWarningFormat("Object {0} HurtBoxGroup hurtbox #{1} is missing.", Util.GetGameObjectHierarchyName(base.gameObject), num2);
			}
			else
			{
				hurtBoxes[num2].hurtBoxGroup = this;
				hurtBoxes[num2].indexInGroup = num2;
				if (hurtBoxes[num2].isBullseye)
				{
					num++;
				}
			}
		}
		if (bullseyeCount != num)
		{
			bullseyeCount = num;
		}
		if (!mainHurtBox)
		{
			IEnumerable<HurtBox> source = from v in hurtBoxes
				where v
				where v.isBullseye
				select v;
			IEnumerable<HurtBox> source2 = source.Where((HurtBox v) => v.transform.parent.name.ToLower(CultureInfo.InvariantCulture) == "chest");
			mainHurtBox = source2.FirstOrDefault() ?? source.FirstOrDefault();
		}
	}

	public VolumeDistribution GetVolumeDistribution()
	{
		return new VolumeDistribution(hurtBoxes);
	}
}
