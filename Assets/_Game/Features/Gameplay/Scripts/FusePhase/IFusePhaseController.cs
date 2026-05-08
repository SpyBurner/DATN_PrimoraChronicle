public interface IFusePhaseController : IController
{
    void SetUnits(string primaryId, string secondaryId);
    void Fuse();
    void Cancel();
}
