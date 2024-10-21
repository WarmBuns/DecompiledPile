using System;
using System.Collections.Generic;
using HG;
using JetBrains.Annotations;
using RoR2.ExpansionManagement;
using UnityEngine;

namespace RoR2.EntitlementManagement;

public abstract class BaseUserEntitlementTracker<TUser> : IDisposable where TUser : class
{
	private Dictionary<TUser, List<EntitlementDef>> userToEntitlements = new Dictionary<TUser, List<EntitlementDef>>();

	private IUserEntitlementResolver<TUser>[] entitlementResolvers;

	private bool disposed;

	public static Action OnUserEntitlementsUpdated;

	public static Action OnAllUserEntitlementsUpdated;

	protected abstract void SubscribeToUserDiscovered();

	protected abstract void SubscribeToUserLost();

	protected abstract void UnsubscribeFromUserDiscovered();

	protected abstract void UnsubscribeFromUserLost();

	protected abstract IList<TUser> GetCurrentUsers();

	public BaseUserEntitlementTracker(IUserEntitlementResolver<TUser>[] entitlementResolvers)
	{
		this.entitlementResolvers = ArrayUtils.Clone(entitlementResolvers);
		SubscribeToUserDiscovered();
		SubscribeToUserLost();
		for (int i = 0; i < entitlementResolvers.Length; i++)
		{
			entitlementResolvers[i].onEntitlementsChanged += UpdateAllUserEntitlements;
		}
		IList<TUser> currentUsers = GetCurrentUsers();
		for (int j = 0; j < currentUsers.Count; j++)
		{
			OnUserDiscovered(currentUsers[j]);
		}
	}

	public virtual void Dispose()
	{
		disposed = true;
		for (int num = entitlementResolvers.Length - 1; num >= 0; num--)
		{
			entitlementResolvers[num].onEntitlementsChanged -= UpdateAllUserEntitlements;
		}
		UnsubscribeFromUserLost();
		UnsubscribeFromUserDiscovered();
		IList<TUser> currentUsers = GetCurrentUsers();
		for (int i = 0; i < currentUsers.Count; i++)
		{
			OnUserLost(currentUsers[i]);
		}
	}

	protected virtual void OnUserDiscovered(TUser user)
	{
		try
		{
			List<EntitlementDef> value = new List<EntitlementDef>();
			userToEntitlements.Add(user, value);
			UpdateUserEntitlements(user);
		}
		catch (Exception message)
		{
			LogError(message);
		}
	}

	protected virtual void OnUserLost(TUser user)
	{
		try
		{
			userToEntitlements.Remove(user);
		}
		catch (Exception message)
		{
			LogError(message);
		}
	}

	public void UpdateAllUserEntitlements()
	{
		IList<TUser> currentUsers = GetCurrentUsers();
		for (int i = 0; i < currentUsers.Count; i++)
		{
			UpdateUserEntitlements(currentUsers[i]);
		}
		OnAllUserEntitlementsUpdated?.Invoke();
	}

	protected void UpdateUserEntitlements(TUser user)
	{
		if (!userToEntitlements.TryGetValue(user, out var value))
		{
			return;
		}
		value.Clear();
		IUserEntitlementResolver<TUser>[] array = entitlementResolvers;
		foreach (IUserEntitlementResolver<TUser> userEntitlementResolver in array)
		{
			foreach (EntitlementDef entitlementDef in EntitlementCatalog.entitlementDefs)
			{
				if (!value.Contains(entitlementDef) && userEntitlementResolver.CheckUserHasEntitlement(user, entitlementDef))
				{
					value.Add(entitlementDef);
				}
			}
		}
		OnUserEntitlementsUpdated?.Invoke();
	}

	public bool UserHasEntitlement([NotNull] TUser user, [NotNull] EntitlementDef entitlementDef)
	{
		if (!entitlementDef)
		{
			throw new ArgumentNullException("entitlementDef");
		}
		if (user == null || !userToEntitlements.TryGetValue(user, out var value))
		{
			return false;
		}
		return value.Contains(entitlementDef);
	}

	public bool AnyUserHasEntitlement([NotNull] EntitlementDef entitlementDef)
	{
		IList<TUser> currentUsers = GetCurrentUsers();
		for (int i = 0; i < currentUsers.Count; i++)
		{
			if (UserHasEntitlement(currentUsers[i], entitlementDef))
			{
				return true;
			}
		}
		return false;
	}

	public bool UserHasEntitlementForUnlockable([NotNull] TUser user, UnlockableDef unlockableDef)
	{
		if (!unlockableDef)
		{
			return true;
		}
		if (user == null || !userToEntitlements.TryGetValue(user, out var value))
		{
			return false;
		}
		ExpansionDef expansionDefForUnlockable = UnlockableCatalog.GetExpansionDefForUnlockable(unlockableDef.index);
		if (!expansionDefForUnlockable)
		{
			return true;
		}
		EntitlementDef requiredEntitlement = expansionDefForUnlockable.requiredEntitlement;
		return value.Contains(requiredEntitlement);
	}

	protected static void LogError(object message)
	{
		RoR2Application.onNextUpdate += delegate
		{
			Debug.LogError(message);
		};
	}
}
