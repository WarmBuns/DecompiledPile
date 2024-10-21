using System.Collections.Generic;
using System.Text;
using HG;
using RoR2.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RoR2.RemoteGameBrowser;

public class RemoteGameCardController : MonoBehaviour
{
	public TextMeshProUGUI nameLabel;

	public TextMeshProUGUI playerCountLabel;

	public TextMeshProUGUI pingLabel;

	public TextMeshProUGUI tagsLabel;

	public TextMeshProUGUI typeLabel;

	public ArtifactDisplayPanelController artifactDisplayPanelController;

	public RawImage mapImage;

	public GameObject passwordIconObject;

	public GameObject difficultyIconObject;

	public Image difficultyIcon;

	private static List<ArtifactDef> artifactBuffer = new List<ArtifactDef>();

	private RemoteGameInfo currentGameInfo;

	public void OpenCurrentGameDetails()
	{
		RectTransform parent = (RectTransform)RoR2Application.instance.mainCanvas.transform;
		GameObject obj = Object.Instantiate(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/RemoteGameBrowser/RemoteGameDetailsPanel"), parent);
		RectTransform obj2 = (RectTransform)obj.transform;
		Vector2 vector = -(obj2.rect.size / 2f);
		vector.y = 0f - vector.y;
		obj2.localPosition = Vector2.zero + vector;
		obj.GetComponent<RemoteGameDetailsPanelController>().SetGameInfo(currentGameInfo);
	}

	public void SetDisplayData(RemoteGameInfo remoteGameInfo)
	{
		currentGameInfo = remoteGameInfo;
		StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
		Texture texture = null;
		if (remoteGameInfo.currentSceneIndex.HasValue)
		{
			texture = SceneCatalog.GetSceneDef(remoteGameInfo.currentSceneIndex.Value)?.previewTexture;
		}
		mapImage.enabled = texture != null;
		mapImage.texture = texture;
		passwordIconObject.SetActive(remoteGameInfo.hasPassword == true);
		playerCountLabel.SetText(stringBuilder.Clear().AppendInt(remoteGameInfo.lobbyPlayerCount ?? remoteGameInfo.serverPlayerCount.GetValueOrDefault()).Append("/")
			.AppendInt(remoteGameInfo.lobbyMaxPlayers ?? remoteGameInfo.serverMaxPlayers.GetValueOrDefault()));
		if (remoteGameInfo.currentDifficultyIndex.HasValue)
		{
			DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(remoteGameInfo.currentDifficultyIndex.Value);
			difficultyIcon.sprite = difficultyDef?.GetIconSprite();
			difficultyIcon.enabled = true;
		}
		else
		{
			difficultyIcon.enabled = false;
		}
		nameLabel.SetText(remoteGameInfo.serverName ?? remoteGameInfo.lobbyName);
		stringBuilder.Clear();
		if (remoteGameInfo.ping.HasValue)
		{
			stringBuilder.AppendInt(remoteGameInfo.ping ?? (-1));
		}
		else
		{
			stringBuilder.Append("N/A");
		}
		pingLabel.SetText(stringBuilder);
		stringBuilder.Clear();
		if (remoteGameInfo.tags != null && remoteGameInfo.tags.Length != 0)
		{
			stringBuilder.Append(remoteGameInfo.tags[0]);
			for (int i = 1; i < remoteGameInfo.tags.Length; i++)
			{
				stringBuilder.Append(", ").Append(remoteGameInfo.tags[i]);
			}
		}
		tagsLabel.SetText(stringBuilder);
		artifactBuffer.Clear();
		foreach (ArtifactDef enabledArtifact in remoteGameInfo.GetEnabledArtifacts())
		{
			artifactBuffer.Add(enabledArtifact);
		}
		List<ArtifactDef>.Enumerator enabledArtifacts = artifactBuffer.GetEnumerator();
		if ((bool)artifactDisplayPanelController)
		{
			artifactDisplayPanelController.SetDisplayData(ref enabledArtifacts);
		}
		HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
	}
}
