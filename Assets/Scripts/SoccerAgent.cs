using UnityEngine;
using UnityEngine.InputSystem;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class SoccerAgent : Agent
{
    [Header("Identidad del Bot")]
    public GoalDetector.Team team;
    public Transform opponentTransform;
    public Transform targetGoal;
    public Transform ownGoal;
    public Rigidbody ballRb;

    [Header("Fisicas Arcade")]
    public float moveSpeed = 13f;
    public float turnSpeed = 260f;
    public float kickForce = 11f;

    private Rigidbody rb;
    private float touchCooldown;

    private float GetTeamDirection()
    {
        return (targetGoal.localPosition.x > ownGoal.localPosition.x) ? 1f : -1f;
    }

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 2f;
        rb.angularDamping = 10f;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    public void ResetAgent(Vector3 startPos)
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        transform.localPosition = startPos;

        // Arranca mirando hacia el arco rival
        float dir = GetTeamDirection();
        transform.localRotation = Quaternion.Euler(0f, dir > 0f ? 90f : -90f, 0f);
        touchCooldown = 0f;
    }

    void FixedUpdate()
    {
        if (touchCooldown > 0f) touchCooldown -= Time.fixedDeltaTime;

        // Castigo existencial minimo: obliga a buscar el gol antes de que acabe el tiempo
        AddReward(-0.0005f);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ball"))
        {
            // Impulso limpio hacia adelante + direccion del impacto
            Vector3 pushDir = (ballRb.transform.localPosition - transform.localPosition).normalized;
            pushDir.y = 0f;
            ballRb.AddForce(pushDir * kickForce, ForceMode.Impulse);

            // Micro-incentivo inicial por tocar el balon (con cooldown largo para evitar farmeo)
            if (touchCooldown <= 0f)
            {
                AddReward(0.03f);
                touchCooldown = 1.5f;
            }
        }
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float dir = GetTeamDirection();

        // Todo en espacio local del bot (sus propios ojos): 100% simetrico entre equipos
        Vector3 localBallPos = transform.InverseTransformPoint(ballRb.transform.position);
        Vector3 localBallVel = transform.InverseTransformDirection(ballRb.linearVelocity);
        Vector3 localSelfVel = transform.InverseTransformDirection(rb.linearVelocity);
        Vector3 localTargetGoal = transform.InverseTransformPoint(targetGoal.position);
        Vector3 localOwnGoal = transform.InverseTransformPoint(ownGoal.position);
        Vector3 localOpponent = transform.InverseTransformPoint(opponentTransform.position);

        sensor.AddObservation(localBallPos.x / 15f);
        sensor.AddObservation(localBallPos.z / 15f);

        sensor.AddObservation(localBallVel.x / 15f);
        sensor.AddObservation(localBallVel.z / 15f);

        sensor.AddObservation(localSelfVel.x / 15f);
        sensor.AddObservation(localSelfVel.z / 15f);

        sensor.AddObservation(localTargetGoal.x / 20f);
        sensor.AddObservation(localTargetGoal.z / 20f);

        sensor.AddObservation(localOwnGoal.x / 20f);
        sensor.AddObservation(localOwnGoal.z / 20f);

        sensor.AddObservation(localOpponent.x / 15f);
        sensor.AddObservation(localOpponent.z / 15f);

        // Brujula relativa a su direccion de ataque
        sensor.AddObservation(transform.forward.x * dir);
        sensor.AddObservation(transform.forward.z * dir);
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        int forwardAct = actions.DiscreteActions[0]; // 0: quieto, 1: adelante, 2: atras
        int strafeAct = actions.DiscreteActions[1];  // 0: quieto, 1: derecha, 2: izquierda
        int rotateAct = actions.DiscreteActions[2];  // 0: quieto, 1: girar der, 2: girar izq

        float forward = (forwardAct == 1) ? 1f : (forwardAct == 2 ? -0.7f : 0f);
        float strafe = (strafeAct == 1) ? 0.8f : (strafeAct == 2 ? -0.8f : 0f);
        float rotate = (rotateAct == 1) ? 1f : (rotateAct == 2 ? -1f : 0f);

        Vector3 moveDir = transform.forward * forward + transform.right * strafe;
        if (moveDir.sqrMagnitude > 1f) moveDir.Normalize();

        Vector3 targetVel = moveDir * moveSpeed;
        targetVel.y = rb.linearVelocity.y;
        rb.linearVelocity = Vector3.MoveTowards(rb.linearVelocity, targetVel, 85f * Time.fixedDeltaTime);

        rb.angularVelocity = new Vector3(0f, rotate * turnSpeed * Mathf.Deg2Rad, 0f);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var discrete = actionsOut.DiscreteActions;
        var kb = Keyboard.current;
        if (kb != null)
        {
            discrete[0] = kb.wKey.isPressed ? 1 : (kb.sKey.isPressed ? 2 : 0);
            discrete[1] = kb.eKey.isPressed ? 1 : (kb.qKey.isPressed ? 2 : 0);
            discrete[2] = kb.dKey.isPressed ? 1 : (kb.aKey.isPressed ? 2 : 0);
        }
    }
}