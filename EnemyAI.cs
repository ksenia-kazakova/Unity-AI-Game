using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public enum State
    {
        Patrol,
        Suspicious,
        Chase,
        Search
    }

    [Header("References")]
    public Transform player;
    public Transform patrolRoute;

    public GameBehavior gameBehavior;
    public float catchDistance = 1.5f;

    [Header("Patrol")]
    public float patrolWaitTime = 1f;

    [Header("Movement")]
    public float walkSpeed = 1f;
    public float runSpeed = 5f;

    [Header("Vision")]
    public float visionDistance = 10f;
    public float visionAngle = 90f;
    public LayerMask obstacleLayer;

    [Header("Search")]
    public float searchTime = 5f;

    private NavMeshAgent agent;
    private Animator animator;
    private List<Transform> patrolPoints = new List<Transform>();
    private int currentPatrolPoint = 0;

    private State currentState = State.Patrol;

    private float patrolWaitTimer;
    private float searchTimer;

    private float lostSightTimer = 0f;
    public float lostSightDelay = 0.5f;

    private Vector3 lastKnownPlayerPosition;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        if (agent == null)
        {
            Debug.LogError("EnemyAI: NavMeshAgent не найден!");
            enabled = false;
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError("EnemyAI: Противник находится вне NavMesh!");
            enabled = false;
            return;
        }

        if (player == null)
        {
            player = GameObject.Find("Player").transform;
        }

        InitializePatrolRoute();
        ChangeState(State.Patrol);

        if (gameBehavior == null)
        {
            gameBehavior = FindFirstObjectByType<GameBehavior>();
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Suspicious:
                Suspicious();
                break;

            case State.Chase:
                Chase();
                break;

            case State.Search:
                Search();
                break;
        }
    }

    void ChangeState(State newState)
    {
        currentState = newState;

        Debug.Log("Enemy state: " + currentState);

        switch (currentState)
        {
            case State.Patrol:
                patrolWaitTimer = 0f;
                agent.isStopped = false;
                agent.speed = walkSpeed;

                if (animator != null)
                {
                    animator.SetBool("IsWalking", true);
                    animator.SetBool("IsRunning", false);
                }

                MoveToNextPatrolPoint();
                break;

            case State.Suspicious:
                agent.isStopped = false;
                agent.SetDestination(lastKnownPlayerPosition);
                break;

            case State.Chase:
                lostSightTimer = 0f;
                agent.isStopped = false;
                agent.speed = runSpeed;

                if (animator != null)
                {
                    animator.SetBool("IsWalking", false);
                    animator.SetBool("IsRunning", true);
                    animator.SetTrigger("GoDown");
                }

                break;

            case State.Search:
                searchTimer = searchTime;
                agent.isStopped = false;
                agent.speed = walkSpeed;
                agent.SetDestination(lastKnownPlayerPosition);

                if (animator != null)
                {
                    animator.SetBool("IsRunning", false);
                    animator.SetBool("IsWalking", true);
                }

                break;
        }
    }

    void Patrol()
    {
        if (CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            ChangeState(State.Chase);
            return;
        }

        if (agent.remainingDistance <= 0.2f && !agent.pathPending)
        {
            patrolWaitTimer += Time.deltaTime;

            if (patrolWaitTimer >= patrolWaitTime)
            {
                MoveToNextPatrolPoint();
                patrolWaitTimer = 0f;
            }
        }
    }

    void MoveToNextPatrolPoint()
    {
        if (patrolPoints.Count == 0)
        {
            return;
        }

        agent.SetDestination(
            patrolPoints[currentPatrolPoint].position
        );

        currentPatrolPoint++;

        if (currentPatrolPoint >= patrolPoints.Count)
        {
            currentPatrolPoint = 0;
        }
    }

    void Suspicious()
    {
        if (CanSeePlayer())
        {
            lastKnownPlayerPosition = player.position;
            ChangeState(State.Chase);
            return;
        }

        if (!agent.pathPending && agent.remainingDistance <= 0.5f)
        {
            agent.isStopped = true;

            // остановка анимации ходьбы
            if (animator != null)
            {
                animator.SetBool("IsWalking", false);
                animator.SetBool("IsRunning", false);
            }

            // поворот к источнику звука
            LookAtSound();

            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                ChangeState(State.Patrol);
            }
        }
    }

    void Chase()
    {
        // поймал ли медведь игрока
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= catchDistance)
        {

            agent.isStopped = true;

            if (animator != null)
            {
                animator.SetBool("IsRunning", false);
                animator.SetBool("IsWalking", false);
            }

            gameBehavior.LoseGame();
            return;
        }

        if (CanSeePlayer())
        {
            lostSightTimer = 0f;

            lastKnownPlayerPosition = player.position;

            agent.isStopped = false;
            agent.speed = runSpeed;
            agent.SetDestination(player.position);
        }
        else
        {
            lostSightTimer += Time.deltaTime;
            if (lostSightTimer >= lostSightDelay)
            {
                lostSightTimer = 0f;
                ChangeState(State.Search);
            }
        }
    }

    void Search()
    {
        if (CanSeePlayer())
        {
            ChangeState(State.Chase);
            return;
        }

        if (agent.remainingDistance <= 0.5f && !agent.pathPending)
        {
            searchTimer -= Time.deltaTime;

            if (searchTimer <= 0f)
            {
                ChangeState(State.Patrol);
            }
        }
    }

    bool CanSeePlayer()
    {
        Vector3 directionToPlayer = player.position - transform.position;

        float distanceToPlayer = directionToPlayer.magnitude;

        // игрок слишком далеко
        if (distanceToPlayer > visionDistance)
        {
            return false;
        }

        // проверка угла обзора
        float angleToPlayer = Vector3.Angle(
            transform.forward,
            directionToPlayer
        );

        if (angleToPlayer > visionAngle / 2f)
        {
            return false;
        }

        // проверка не находится ли дерево/стена между врагом и игроком
        RaycastHit hit;

        if (Physics.Raycast(
            transform.position + Vector3.up,
            directionToPlayer.normalized,
            out hit,
            visionDistance,
            obstacleLayer
        ))
        {
            if (hit.transform != player)
            {
                return false;
            }
        }

        return true;
    }

    void InitializePatrolRoute()
    {
        if (patrolRoute == null)
        {
            return;
        }

        foreach (Transform child in patrolRoute)
        {
            patrolPoints.Add(child);
        }
    }

    void OnDrawGizmosSelected()
    {
        // радиус зрения
        Gizmos.DrawWireSphere(
            transform.position,
            visionDistance
        );

        // последняя известная позиция игрока
        Gizmos.DrawSphere(
            lastKnownPlayerPosition,
            0.3f
        );
    }
  
    void OnEnable()
    {
        NoiseSystem.OnNoiseMade += HearNoise;
    }

    void OnDisable()
    {
        NoiseSystem.OnNoiseMade -= HearNoise;
    }

    void HearNoise(Vector3 noisePosition, float noiseRadius)
    {
        if (currentState == State.Chase)
            return;

        float distance = Vector3.Distance(
            transform.position,
            noisePosition
        );

        if (distance <= noiseRadius)
        {
            lastKnownPlayerPosition = noisePosition;
            ChangeState(State.Suspicious);
        }
    }

    public void HearSound(Vector3 soundPosition, float soundRadius)
    {
        if (currentState == State.Chase)
            return;

        float distance = Vector3.Distance(
            transform.position,
            soundPosition
        );

        if (distance > soundRadius)
            return;

        lastKnownPlayerPosition = soundPosition;
        searchTimer = searchTime;

        if (currentState != State.Suspicious)
        {
            ChangeState(State.Suspicious);
        }

    }

    void LookAtSound()
    {
        Vector3 direction = lastKnownPlayerPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            120f * Time.deltaTime
        );
    }

}