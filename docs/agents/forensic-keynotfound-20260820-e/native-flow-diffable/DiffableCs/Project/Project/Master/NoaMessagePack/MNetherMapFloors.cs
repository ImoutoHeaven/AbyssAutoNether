namespace Project.Master.NoaMessagePack;

[MessagePackObject(False)]
public sealed class MNetherMapFloors
{
	[Key(0)]
	public long id; //Field offset: 0x10
	[Key(1)]
	public long m_nether_map_id; //Field offset: 0x18
	[Key(2)]
	public int min_order; //Field offset: 0x20
	[Key(3)]
	public int max_order; //Field offset: 0x24
	[Key(4)]
	public int min_erosion_point; //Field offset: 0x28
	[Key(5)]
	public int max_erosion_point; //Field offset: 0x2C
	[Key(6)]
	public long m_nether_map_floor_id_prev; //Field offset: 0x30
	[Key(7)]
	public long m_nether_map_floor_id_next; //Field offset: 0x38
	[Key(8)]
	public int element_type; //Field offset: 0x40
	[Key(9)]
	public int size; //Field offset: 0x44
	[Key(10)]
	public int type; //Field offset: 0x48
	[Key(11)]
	public int used_count; //Field offset: 0x4C

	public MNetherMapFloors() { }

}

