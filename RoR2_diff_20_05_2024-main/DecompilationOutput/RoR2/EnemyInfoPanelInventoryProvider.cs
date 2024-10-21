using System;
using UnityEngine;

namespace RoR2;

[RequireComponent(typeof(Inventory))]
[RequireComponent(typeof(TeamFilter))]
public class EnemyInfoPanelInventoryProvider : MonoBehaviour
{
	private static bool isDirty;

	public Inventory inventory { get; private set; }

	public TeamFilter teamFilter { get; private set; }

	public static event Action onInventoriesChanged;

	private void Awake()
	{
		inventory = GetComponent<Inventory>();
		teamFilter = GetComponent<TeamFilter>();
		inventory.onInventoryChanged += OnInventoryChanged;
	}

	private void OnInventoryChanged()
	{
		MarkAsDirty();
	}

	private void OnEnable()
	{
		InstanceTracker.Add(this);
		MarkAsDirty();
	}

	private void OnDisable()
	{
		MarkAsDirty();
		InstanceTracker.Remove(this);
	}

	private void MarkAsDirty()
	{
		if (!isDirty)
		{
			isDirty = true;
			RoR2Application.onNextUpdate += Refresh;
		}
	}

	private void Refresh()
	{
		EnemyInfoPanelInventoryProvider.onInventoriesChanged?.Invoke();
		isDirty = false;
	}
}
