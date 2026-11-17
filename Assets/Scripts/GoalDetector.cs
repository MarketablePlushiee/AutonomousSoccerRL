using UnityEngine;

public class GoalDetector : MonoBehaviour
{
    public enum Team { Orange, Blue }
    public Team defendingTeam;
    public SoccerEnvController envController;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
        {
            if (defendingTeam == Team.Orange)
                envController.GoalScored(Team.Blue);
            else
                envController.GoalScored(Team.Orange);
        }
    }
}