using UnityEngine;

namespace RoR2;

public class MonsterCounter : MonoBehaviour
{
	private int enemyList;

	private int CountEnemies()
	{
		return TeamComponent.GetTeamMembers(TeamIndex.Monster).Count;
	}

	private void Update()
	{
		enemyList = CountEnemies();
	}

	private void OnGUI()
	{
		GUI.Label(new Rect(12f, 160f, 200f, 25f), "Living Monsters: " + enemyList);
	}
}
