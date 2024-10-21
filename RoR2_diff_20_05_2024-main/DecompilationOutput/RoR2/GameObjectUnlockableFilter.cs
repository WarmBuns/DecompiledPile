using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class GameObjectUnlockableFilter : NetworkBehaviour
{
	[Obsolete("'requiredUnlockable' will be discontinued. Use 'requiredUnlockableDef' instead.", false)]
	[Tooltip("'requiredUnlockable' will be discontinued. Use 'requiredUnlockableDef' instead.")]
	public string requiredUnlockable;

	[Obsolete("'forbiddenUnlockable' will be discontinued. Use 'forbiddenUnlockableDef' instead.", false)]
	[Tooltip("'forbiddenUnlockable' will be discontinued. Use 'forbiddenUnlockableDef' instead.")]
	public string forbiddenUnlockable;

	public UnlockableDef requiredUnlockableDef;

	public UnlockableDef forbiddenUnlockableDef;

	[Tooltip("If all users have this achievement, disable")]
	public string AchievementToDisable;

	[SyncVar]
	private bool active;

	public bool Networkactive
	{
		get
		{
			return active;
		}
		[param: In]
		set
		{
			SetSyncVar(value, ref active, 1u);
		}
	}

	private void Start()
	{
		if (NetworkServer.active)
		{
			Run.instance?.RefreshUnlockAvailability();
			Networkactive = ShouldShowGameObject();
			base.gameObject.SetActive(active);
		}
	}

	private bool ShouldShowGameObject()
	{
		if ((bool)Run.instance)
		{
			ref string reference = ref requiredUnlockable;
			ref string reference2 = ref forbiddenUnlockable;
			if (!requiredUnlockableDef && !string.IsNullOrEmpty(reference))
			{
				requiredUnlockableDef = UnlockableCatalog.GetUnlockableDef(reference);
				reference = null;
			}
			if (!forbiddenUnlockableDef && !string.IsNullOrEmpty(reference2))
			{
				forbiddenUnlockableDef = UnlockableCatalog.GetUnlockableDef(reference2);
				reference2 = null;
			}
			if (AchievementToDisable != null)
			{
				bool flag = false;
				foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
				{
					if (!instance.networkUser.localUser.userProfile.HasAchievement(AchievementToDisable))
					{
						flag = true;
						break;
					}
				}
				Debug.LogFormat("GameObjectUnlockableFilter: Do all users have achievement {0}? : {1}", AchievementToDisable, flag);
				return flag;
			}
			bool flag2 = !requiredUnlockableDef || Run.instance.IsUnlockableUnlocked(requiredUnlockableDef);
			bool flag3 = !forbiddenUnlockableDef || Run.instance.DoesEveryoneHaveThisUnlockableUnlocked(forbiddenUnlockableDef);
			Debug.LogFormat("GameObjectUnlockableFilter: requiredUnlockableIsUnlocked {0}; forbiddenUnlockableIsUnlocked {1}", flag2, flag3);
			if (flag2)
			{
				return !flag3;
			}
			return false;
		}
		return true;
	}

	private void UNetVersion()
	{
	}

	public override bool OnSerialize(NetworkWriter writer, bool forceAll)
	{
		if (forceAll)
		{
			writer.Write(active);
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
			writer.Write(active);
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
			active = reader.ReadBoolean();
			return;
		}
		int num = (int)reader.ReadPackedUInt32();
		if (((uint)num & (true ? 1u : 0u)) != 0)
		{
			active = reader.ReadBoolean();
		}
	}

	public override void PreStartClient()
	{
	}
}
