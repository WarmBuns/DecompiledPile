using System.Collections.Generic;
using Grumpy;

namespace RoR2;

public class EffectPool : PrefabComponentPool<EffectManagerHelper>
{
	protected List<EffectManagerHelper> _RemoveHelperList;

	protected List<EffectManagerHelper> _OrphanedEffects = new List<EffectManagerHelper>();

	protected const int MAX_EFFECT_POOL_CAPACITY = 20;

	protected override void PreDestroyObject(EffectManagerHelper inKillObject)
	{
		inKillObject.SetOwningPool(null);
	}

	public void Tick(float inDeltaTime)
	{
		for (int num = _OrphanedEffects.Count - 1; num >= 0; num--)
		{
			EffectManagerHelper effectManagerHelper = _OrphanedEffects[num];
			if (effectManagerHelper != null && effectManagerHelper.EffectComplete())
			{
				ReturnObject(effectManagerHelper);
				_OrphanedEffects.RemoveAt(num);
			}
		}
	}

	public override void ReturnObject(EffectManagerHelper inReturnObject)
	{
		if (inReturnObject.EffectComplete())
		{
			inReturnObject.OnReturnToPool();
			if (inReturnObject.OwningPool.PoolCount() >= 20)
			{
				DestroyObject(inReturnObject);
			}
			else
			{
				base.ReturnObject(inReturnObject);
			}
		}
		else
		{
			_OrphanedEffects.Add(inReturnObject);
		}
	}

	public void RemoveObject(EffectManagerHelper inReturnObject)
	{
		if (_InUse != null && _InUse.Contains(inReturnObject))
		{
			_InUse.Remove(inReturnObject);
		}
		if (_Pool == null || !_Pool.Contains(inReturnObject))
		{
			return;
		}
		if (_RemoveHelperList == null)
		{
			_RemoveHelperList = new List<EffectManagerHelper>();
		}
		if (_Pool == null || _Pool.Count <= 0)
		{
			return;
		}
		EffectManagerHelper effectManagerHelper = _Pool.Pop();
		while (effectManagerHelper != null)
		{
			if (!(effectManagerHelper == inReturnObject))
			{
				_RemoveHelperList.Add(effectManagerHelper);
			}
			effectManagerHelper = ((_Pool.Count <= 0) ? null : _Pool.Pop());
		}
		for (int num = _RemoveHelperList.Count - 1; num >= 0; num--)
		{
			_Pool.Push(_RemoveHelperList[num]);
		}
		_RemoveHelperList.Clear();
	}
}
