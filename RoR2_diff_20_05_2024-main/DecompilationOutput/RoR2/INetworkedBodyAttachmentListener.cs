namespace RoR2;

public interface INetworkedBodyAttachmentListener
{
	void OnAttachedBodyDiscovered(NetworkedBodyAttachment networkedBodyAttachment, CharacterBody attachedBody);
}
