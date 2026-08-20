namespace Project.Master.NoaMessagePack;

[MessagePackObject(False)]
public sealed class MNetherFloorTreasures
{
	[Key(0)]
	public long id; //Field offset: 0x10
	[Key(1)]
	public long m_nether_map_floor_id; //Field offset: 0x18

	public MNetherFloorTreasures() { }

}

