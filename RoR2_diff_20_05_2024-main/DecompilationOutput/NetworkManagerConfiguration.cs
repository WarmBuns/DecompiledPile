using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class NetworkManagerConfiguration : MonoBehaviour
{
	public new bool DontDestroyOnLoad = true;

	public bool RunInBackground = true;

	public LogFilter.FilterLevel LogLevel = LogFilter.FilterLevel.Info;

	public string OfflineScene;

	public string OnlineScene;

	public GameObject PlayerPrefab;

	public bool AutoCreatePlayer = true;

	public PlayerSpawnMethod PlayerSpawnMethod;

	public List<GameObject> SpawnPrefabs = new List<GameObject>();

	public bool CustomConfig;

	public int MaxConnections = 4;

	public List<QosType> QosChannels = new List<QosType>();
}
