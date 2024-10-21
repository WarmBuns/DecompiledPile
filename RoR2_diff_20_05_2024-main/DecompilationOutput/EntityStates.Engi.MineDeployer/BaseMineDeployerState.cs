using System.Collections.Generic;
using UnityEngine;

namespace EntityStates.Engi.MineDeployer;

public class BaseMineDeployerState : BaseState
{
	public static List<BaseMineDeployerState> instancesList = new List<BaseMineDeployerState>();

	public GameObject owner { get; private set; }

	public override void OnEnter()
	{
		base.OnEnter();
		owner = base.projectileController?.owner;
		instancesList.Add(this);
	}

	public override void OnExit()
	{
		instancesList.Remove(this);
		base.OnExit();
	}
}
