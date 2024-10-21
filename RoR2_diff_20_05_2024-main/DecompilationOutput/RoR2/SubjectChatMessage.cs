using UnityEngine;
using UnityEngine.Networking;

namespace RoR2;

public class SubjectChatMessage : ChatMessageBase
{
	private GameObject subjectNetworkUserObject;

	private GameObject subjectCharacterBodyGameObject;

	public string baseToken;

	private MemoizedGetComponent<NetworkUser> subjectNetworkUserGetComponent;

	private MemoizedGetComponent<CharacterBody> subjectCharacterBodyGetComponent;

	public NetworkUser subjectAsNetworkUser
	{
		get
		{
			return subjectNetworkUserGetComponent.Get(subjectNetworkUserObject);
		}
		set
		{
			subjectNetworkUserObject = (value ? value.gameObject : null);
			CharacterBody characterBody = null;
			if ((bool)value)
			{
				characterBody = value.GetCurrentBody();
			}
			subjectCharacterBodyGameObject = (characterBody ? characterBody.gameObject : null);
		}
	}

	public CharacterBody subjectAsCharacterBody
	{
		get
		{
			return subjectCharacterBodyGetComponent.Get(subjectCharacterBodyGameObject);
		}
		set
		{
			subjectCharacterBodyGameObject = (value ? value.gameObject : null);
			subjectNetworkUserObject = Util.LookUpBodyNetworkUser(value)?.gameObject;
		}
	}

	protected string GetSubjectName()
	{
		if ((bool)subjectAsNetworkUser)
		{
			return Util.EscapeRichTextForTextMeshPro(subjectAsNetworkUser.userName);
		}
		if ((bool)subjectAsCharacterBody)
		{
			return subjectAsCharacterBody.GetDisplayName();
		}
		return "???";
	}

	protected bool IsSecondPerson()
	{
		if (LocalUserManager.readOnlyLocalUsersList.Count == 1 && (bool)subjectAsNetworkUser && subjectAsNetworkUser.localUser != null)
		{
			return true;
		}
		return false;
	}

	protected string GetResolvedToken()
	{
		if (!IsSecondPerson())
		{
			return baseToken;
		}
		return baseToken + "_2P";
	}

	public override string ConstructChatString()
	{
		return string.Format(Language.GetString(GetResolvedToken()), GetSubjectName());
	}

	public override void Serialize(NetworkWriter writer)
	{
		((MessageBase)this).Serialize(writer);
		writer.Write(subjectNetworkUserObject);
		writer.Write(subjectCharacterBodyGameObject);
		writer.Write(baseToken);
	}

	public override void Deserialize(NetworkReader reader)
	{
		((MessageBase)this).Deserialize(reader);
		subjectNetworkUserObject = reader.ReadGameObject();
		subjectCharacterBodyGameObject = reader.ReadGameObject();
		baseToken = reader.ReadString();
	}
}
