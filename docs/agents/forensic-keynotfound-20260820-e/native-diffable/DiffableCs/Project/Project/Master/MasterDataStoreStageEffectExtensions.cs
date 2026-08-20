namespace Project.Master;

[Extension]
public static class MasterDataStoreStageEffectExtensions
{
	[CompilerGenerated]
	private sealed class <>c
	{
		public static readonly <>c <>9; //Field offset: 0x0
		public static Func<MStageEffects, Int64, Boolean> <>9__1_0; //Field offset: 0x8
		public static Func<MStageEffects, StageEffectModel> <>9__1_1; //Field offset: 0x10

		private static <>c() { }

		public <>c() { }

		internal bool <CreateStageEffectModels>b__1_0(MStageEffects MStageEffect, long no) { }

		internal StageEffectModel <CreateStageEffectModels>b__1_1(MStageEffects m) { }

	}


	[Extension]
	public static List<StageEffectModel> CreateStageEffectModels(MasterDataStore masterDataStore, long stageEffectNo) { }

	[Extension]
	public static List<StageEffectModel> CreateStageEffectModels(MStageEffects[] mStageEffects, long stageEffectNo) { }

}

