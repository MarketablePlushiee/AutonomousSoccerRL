using UnityEngine;

public class SoccerEnvController : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public SoccerAgent agentOrange;
    public SoccerAgent agentBlue;
    public Rigidbody ballRb;

    [Header("Configuracion de Partido")]
    public float maxEpisodeSeconds = 25f;
    private float timer;

    private Vector3 orangeStartPos = new Vector3(-4.5f, 0.5f, 0f);
    private Vector3 blueStartPos = new Vector3(4.5f, 0.5f, 0f);
    private Vector3 ballStartPos = new Vector3(0f, 0.5f, 0f);

    void Start()
    {
        ResetScene();
    }

    void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= maxEpisodeSeconds)
        {
            EndAllEpisodes();
        }
    }

    public void GoalScored(GoalDetector.Team scoringTeam)
    {
        // Bono por anotar rapido: hasta +1.5 si mete gol temprano, -1.0 al que lo recibe
        float speedBonus = Mathf.Clamp01(1f - (timer / maxEpisodeSeconds)) * 0.5f;

        if (scoringTeam == GoalDetector.Team.Orange)
        {
            agentOrange.AddReward(1.0f + speedBonus);
            agentBlue.AddReward(-1.0f);
        }
        else
        {
            agentBlue.AddReward(1.0f + speedBonus);
            agentOrange.AddReward(-1.0f);
        }

        EndAllEpisodes();
    }

    private void EndAllEpisodes()
    {
        agentOrange.EndEpisode();
        agentBlue.EndEpisode();
        ResetScene();
    }

    public void ResetScene()
    {
        timer = 0f;

        ballRb.linearVelocity = Vector3.zero;
        ballRb.angularVelocity = Vector3.zero;
        ballRb.transform.localPosition = ballStartPos + new Vector3(
            Random.Range(-0.8f, 0.8f),
            0f,
            Random.Range(-2.5f, 2.5f)
        );

        agentOrange.ResetAgent(orangeStartPos + new Vector3(
            Random.Range(-1.0f, 0.8f),
            0f,
            Random.Range(-3.0f, 3.0f)
        ));

        agentBlue.ResetAgent(blueStartPos + new Vector3(
            Random.Range(-0.8f, 1.0f),
            0f,
            Random.Range(-3.0f, 3.0f)
        ));
    }
}