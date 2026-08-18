#nullable enable

namespace Project.Nether.FloorSelection
{
    public sealed class SubViewController
    {
    }

    public sealed class ManagedFloorModel
    {
        public long MNetherMapFloorId { get; init; }
        public long ExtendId { get; init; }
        public int FloorType { get; init; }
    }

    public sealed class ManagedMapFloorRow
    {
        public long id;
        public int min_erosion_point;
        public int max_erosion_point;
    }

    public sealed class ManagedEventRow
    {
        public long id;
        public long m_nether_map_floor_id;
        public int weight;
        public int type;
        public long m_nether_floor_event_part_id_1;
        public long m_nether_floor_event_part_id_2;
        public long m_nether_floor_event_part_id_3;
        public long m_nether_floor_event_part_id_4;
    }

    public sealed class ManagedEventPartRow
    {
        public long id;
        public int target_type_1;
        public long select_parameter_1;
        public int target_type_2;
        public long select_parameter_2;
        public int target_type_3;
        public long select_parameter_3;
        public int content_type;
        public long content_id;
        public int amount;
    }

    public sealed class ManagedBattleRow
    {
        public long id;
        public long m_nether_map_floor_id;
        public int type;
        public long m_nether_battle_stage_id;
        public int code_drop_ratio;
    }
}

namespace Project.Nether.NetherEventPopup
{
    public sealed class NetherEventPopupController
    {
        public void SetupPopupEvent(object popup, object close)
        {
        }
    }
}
