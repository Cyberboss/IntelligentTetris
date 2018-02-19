namespace Cyberboss.IntelligentInvaders
{
    public interface IGameOrchestrator
    {
        IGameInstance CreateGameInstance(bool demoMode);
    }
}
