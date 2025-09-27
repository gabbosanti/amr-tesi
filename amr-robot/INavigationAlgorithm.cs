
namespace RobotApp
{
    public interface INavigationAlgorithm
    {
        event Action ObstacleDetected;
        event Action GoalReached;

        Task StartNavigationAsync();
        Task StepTowardsTarget();
        Task HandleObstacleAsync();
        void StopNavigation();
    }
}
