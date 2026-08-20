namespace Project.Master.NoaMessagePack;

[MessagePackObject(False)]
public sealed class MNetherFloorEvents
{
	[Key(0)]
	public long id; //Field offset: 0x10
	[Key(1)]
	public long m_nether_map_floor_id; //Field offset: 0x18
	[Key(2)]
	public int group_id; //Field offset: 0x20
	[Key(3)]
	public int weight; //Field offset: 0x24
	[Key(4)]
	public int type; //Field offset: 0x28
	[Key(5)]
	public string description; //Field offset: 0x30
	[Key(6)]
	public long m_nether_floor_event_part_id_1; //Field offset: 0x38
	[Key(7)]
	public long m_nether_floor_event_part_id_2; //Field offset: 0x40
	[Key(8)]
	public long m_nether_floor_event_part_id_3; //Field offset: 0x48
	[Key(9)]
	public long m_nether_floor_event_part_id_4; //Field offset: 0x50

	public MNetherFloorEvents() { }

}

