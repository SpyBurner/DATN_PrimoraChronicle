using Fusion;
using Core.GDS;

public struct CombatSkillExecutionContext
{
    public string CasterId;
    public UnitPublicData CasterData;
    public HexCoord Target;
    public SkillData SkillData;
    public NetworkRunner Runner;
    public IUnitSubsystem UnitSubsystem;
    public IBoardSubsystem BoardSubsystem;
    public IDamagePipelineSubsystem DamagePipeline;
    public ITileEffectSubsystem TileEffectSubsystem;
    public ICardLoadingManagerSubsystem CardLoading;
    public IDebugLogger Logger;
    public CombatNetworkView CombatView;
}
