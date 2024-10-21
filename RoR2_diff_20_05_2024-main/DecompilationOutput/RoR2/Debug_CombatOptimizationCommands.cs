using System.Collections;
using UnityEngine;

namespace RoR2;

public class Debug_CombatOptimizationCommands : MonoBehaviour
{
	public void SubmitCombatOptimizationCommands()
	{
		Console.instance.SubmitCmd(null, "give_item BleedOnHit 50");
		Console.instance.SubmitCmd(null, "give_item BleedOnHitAndExplode 50");
		StartCoroutine(GenerateEnemies());
	}

	private IEnumerator GenerateEnemies()
	{
		for (int q = 0; q < 12; q++)
		{
			Console.instance.SubmitCmd(null, "create_master BeetleMaster");
			yield return 0;
			Console.instance.SubmitCmd(null, "create_master LemurianMaster");
			yield return 0;
		}
	}
}
