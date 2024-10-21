using System.Collections.Generic;
using Grumpy;
using UnityEngine;

namespace RoR2.UI;

[RequireComponent(typeof(RectTransform))]
public class BuffDisplay : MonoBehaviour
{
	public class BuffIconDisplayData
	{
		public BuffIndex buffIndex;

		public BuffIcon buffIconComponent;

		public BuffDef buffDef;

		public int buffCount;

		public bool removeIcon;

		public BuffIconDisplayData(BuffIndex _buffIndex)
		{
			buffIndex = _buffIndex;
			buffDef = BuffCatalog.GetBuffDef(_buffIndex);
			buffCount = 0;
			buffIconComponent = null;
			removeIcon = false;
		}
	}

	private static int _staticFrameIndex;

	private static int maxFramesToWait = 4;

	private int frameIndex;

	private static PrefabComponentPool<BuffIcon> pool;

	private List<BuffIconDisplayData> buffIconDisplayData = new List<BuffIconDisplayData>();

	private bool buffsReallocated;

	private RectTransform rectTranform;

	private CharacterBody source;

	public GameObject buffIconPrefab;

	public float iconWidth = 24f;

	private static int frameIndexTicket
	{
		get
		{
			_staticFrameIndex = (_staticFrameIndex + 1) % maxFramesToWait;
			return _staticFrameIndex;
		}
	}

	private void Awake()
	{
		frameIndex = frameIndexTicket;
		rectTranform = GetComponent<RectTransform>();
		if (pool == null)
		{
			pool = new PrefabComponentPool<BuffIcon>
			{
				PrefabObject = buffIconPrefab,
				AlwaysGrowable = true,
				AddComponentIfMissing = true
			};
			pool.Initialize(1, 3);
		}
	}

	public void SetSource(CharacterBody _source)
	{
		if (!(source == _source))
		{
			source = _source;
			ResetAllocatedIcons();
		}
	}

	private void ResetAllocatedIcons()
	{
		PrepareToCullBuffDisplayData();
		CullExpiredBuffIcons();
		buffsReallocated = false;
		UpdateLayout();
	}

	private void AllocateIcons()
	{
		if (buffsReallocated || source == null)
		{
			return;
		}
		PrepareToCullBuffDisplayData();
		int num = this.buffIconDisplayData.Count;
		int i = 0;
		int num2 = num;
		BuffIndex[] nonHiddenBuffIndices = BuffCatalog.nonHiddenBuffIndices;
		foreach (BuffIndex buffIndex in nonHiddenBuffIndices)
		{
			if (!source.HasBuff(buffIndex))
			{
				continue;
			}
			int buffCount = source.GetBuffCount(buffIndex);
			for (; i < num && this.buffIconDisplayData[i].buffIndex < buffIndex; i++)
			{
			}
			if (i < num)
			{
				BuffIconDisplayData buffIconDisplayData = this.buffIconDisplayData[i];
				if (buffIconDisplayData.buffIndex == buffIndex)
				{
					buffsReallocated |= buffIconDisplayData.buffCount != buffCount;
					buffIconDisplayData.removeIcon = false;
					buffIconDisplayData.buffCount = buffCount;
					i++;
				}
				else
				{
					buffIconDisplayData = new BuffIconDisplayData(buffIndex);
					buffIconDisplayData.buffCount = buffCount;
					this.buffIconDisplayData.Insert(i, buffIconDisplayData);
					i++;
					num++;
					buffsReallocated = true;
				}
			}
			else
			{
				BuffIconDisplayData buffIconDisplayData = new BuffIconDisplayData(buffIndex);
				buffIconDisplayData.buffCount = buffCount;
				this.buffIconDisplayData.Add(buffIconDisplayData);
				buffsReallocated = true;
			}
		}
		buffsReallocated |= num2 != this.buffIconDisplayData.Count;
	}

	private void PrepareToCullBuffDisplayData()
	{
		foreach (BuffIconDisplayData buffIconDisplayDatum in buffIconDisplayData)
		{
			buffIconDisplayDatum.removeIcon = true;
		}
	}

	private void CullExpiredBuffIcons()
	{
		for (int num = this.buffIconDisplayData.Count - 1; num >= 0; num--)
		{
			BuffIconDisplayData buffIconDisplayData = this.buffIconDisplayData[num];
			if (buffIconDisplayData.removeIcon || buffIconDisplayData.buffDef == null || (buffIconDisplayData.buffDef != null && buffIconDisplayData.buffDef.iconSprite == null))
			{
				if (buffIconDisplayData.buffIconComponent != null)
				{
					ReturnBuffIconToPool(buffIconDisplayData.buffIconComponent);
				}
				this.buffIconDisplayData.RemoveAt(num);
				buffsReallocated = true;
			}
		}
	}

	public void UpdateLayout()
	{
		AllocateIcons();
		CullExpiredBuffIcons();
		if (!buffsReallocated)
		{
			return;
		}
		if ((bool)source)
		{
			Vector2 zero = Vector2.zero;
			foreach (BuffIconDisplayData buffIconDisplayDatum in buffIconDisplayData)
			{
				if (buffIconDisplayDatum.buffIconComponent == null)
				{
					buffIconDisplayDatum.buffIconComponent = GetNewBuffIcon();
				}
				BuffIcon buffIconComponent = buffIconDisplayDatum.buffIconComponent;
				buffIconComponent.buffDef = buffIconDisplayDatum.buffDef;
				buffIconComponent.buffCount = buffIconDisplayDatum.buffCount;
				buffIconComponent.rectTransform.anchoredPosition = zero;
				zero.x += iconWidth;
				buffIconComponent.UpdateIcon();
			}
		}
		buffsReallocated = false;
	}

	private void Update()
	{
		if (frameIndex == 0)
		{
			UpdateLayout();
		}
		frameIndex = (frameIndex + 1) % maxFramesToWait;
	}

	private BuffIcon GetNewBuffIcon()
	{
		BuffIcon @object = pool.GetObject();
		@object.ResetBuffIcon();
		Transform obj = @object.transform;
		obj.SetParent(rectTranform);
		obj.localPosition = Vector3.zero;
		obj.localScale = Vector3.one;
		@object.gameObject.SetActive(value: true);
		return @object;
	}

	private void ReturnBuffIconToPool(BuffIcon buffIcon)
	{
		buffIcon.transform.SetParent(null);
		pool.ReturnObject(buffIcon);
	}
}
