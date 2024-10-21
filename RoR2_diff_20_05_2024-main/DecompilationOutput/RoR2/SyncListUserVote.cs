using UnityEngine.Networking;

namespace RoR2;

public class SyncListUserVote : SyncListStruct<UserVote>
{
	public override void SerializeItem(NetworkWriter writer, UserVote item)
	{
		writer.Write(item.networkUserObject);
		writer.WritePackedUInt32((uint)item.voteChoiceIndex);
	}

	public override UserVote DeserializeItem(NetworkReader reader)
	{
		UserVote result = default(UserVote);
		result.networkUserObject = reader.ReadGameObject();
		result.voteChoiceIndex = (int)reader.ReadPackedUInt32();
		return result;
	}
}
