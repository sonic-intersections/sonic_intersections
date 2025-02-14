using Intersect.Enums;
using MessagePack;

namespace Intersect.Network.Packets.Client;

[MessagePackObject]
public partial class AttackPacket : AbstractTimedPacket
{
    //Parameterless Constructor for MessagePack
    public AttackPacket()
    {

    }
    
    public AttackPacket(Guid target, AttackType attackType)
    {
        Target = target;
        AttackType = attackType;
    }

    [Key(3)]
    public Guid Target { get; set; }
    [Key(4)]
    public AttackType AttackType { get; set; }

}
