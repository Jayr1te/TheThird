
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Lanes")]
    public float laneWidth = 2f;
    public int laneCount = 3;
    public float laneSwitchSpeed = 15f;

    [Header("Jump")]
    public float jumpDuration = 0.6f;
    public float jumpScale = 1.5f;

    [Header("Slide")]
    public float slideDuration = 0.6f;
    public float slideScale = 0.75f;

    [Header("Spawning")]
    public ObstacleSpawner obstacleSpawner;

    [Header("Movement Speed")]
    public float baseMovementSpeed = 6f;
    public float startingPlayerSpeed = 1f;
    public float startingFireSpeed = 0.75f;

    public float obstacleSpeedBonus = 1.1f;
    public float collisionSpeedMultiplier = 0.5f;

    public float minimumPlayerSpeed = 0.25f;
    public float maximumPlayerSpeed = 3f;


    private int currentLane;
    private float targetX;

    private bool isJumping;
    private bool isSliding;
    private bool isDead;

    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;

        // Start in the middle lane
        currentLane = laneCount / 2;
        targetX = LaneToX(currentLane);

        transform.position = new Vector3(
            targetX,
            transform.position.y,
            transform.position.z
        );

        GlobalVars.speedMult = startingPlayerSpeed;
        GlobalVars.distance = 0f;
        GlobalVars.fireDistance = -15f;
        GlobalVars.fireSpeed = startingFireSpeed;
    }

    void Update()
    {
        if (isDead) return;

        // Check keyboard input every frame
        HandleInput();

        // Smoothly move toward the target lane
        float newX = Mathf.MoveTowards(
            transform.position.x,
            targetX,
            laneSwitchSpeed * Time.deltaTime
        );

        transform.position = new Vector3(
            newX,
            transform.position.y,
            transform.position.z
        );

        // Update game speed and distances
        GlobalVars.distance +=
            baseMovementSpeed *
            GlobalVars.speedMult *
            Time.deltaTime;

        // Fire progress
        GlobalVars.fireDistance +=
            baseMovementSpeed *
            GlobalVars.fireSpeed *
            Time.deltaTime;

        GlobalVars.speedMult += 0.1f * Time.deltaTime;
        GlobalVars.fireSpeed += 0.025f * Time.deltaTime;

        // Restart when fire catches the player
        if (GlobalVars.distance <= GlobalVars.fireDistance)
        {
            RestartLevel();
        }
    }

    void HandleInput()
    {
        // Move left
        if (Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeLane(-1);
        }

        // Move right
        if (Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeLane(1);
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (!isJumping && !isSliding)
            {
                StartCoroutine(Jump());
            }
        }

        // Slide
        if (Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.DownArrow))
        {
            if (!isJumping && !isSliding)
            {
                StartCoroutine(Slide());
            }
        }
    }

    void ChangeLane(int direction)
    {
        currentLane = Mathf.Clamp(
            currentLane + direction,
            0,
            laneCount - 1
        );

        targetX = LaneToX(currentLane);
    }

    // Convert lane index to world X position
    float LaneToX(int lane)
    {
        return (lane - (laneCount - 1) / 2f) * laneWidth;
    }

    // Fake jump height by scaling the sprite
    IEnumerator Jump()
    {
        isJumping = true;
        float timer = 0f;

        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;

            float arc = Mathf.Sin(
                Mathf.Clamp01(timer / jumpDuration) * Mathf.PI
            );

            transform.localScale = baseScale *
                Mathf.Lerp(1f, jumpScale, arc);

            yield return null;
        }

        transform.localScale = baseScale;
        isJumping = false;
    }

    // Fake sliding by shrinking the sprite
    IEnumerator Slide()
    {
        isSliding = true;
        float timer = 0f;

        while (timer < slideDuration)
        {
            timer += Time.deltaTime;

            float arc = Mathf.Sin(
                Mathf.Clamp01(timer / slideDuration) * Mathf.PI
            );

            transform.localScale = baseScale *
                Mathf.Lerp(1f, slideScale, arc);

            yield return null;
        }

        transform.localScale = baseScale;
        isSliding = false;
    }

    // Detect obstacle collisions
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;

        Obstacle obstacle = other.GetComponent<Obstacle>();
        if (obstacle == null) return;
        bool successfullyAvoided =
            (isJumping && obstacle.canBeJumped) ||
            (isSliding && obstacle.canBeSlid);

        if (successfullyAvoided)
        {
            if (!obstacle.hasAwardedBonus)
            {
                obstacle.hasAwardedBonus = true;

                 GlobalVars.speedMult = Mathf.Min(
                    GlobalVars.speedMult * obstacleSpeedBonus,
                    maximumPlayerSpeed
                );

            }

            return;
        }

         // Player hit an obstacle
        if (!obstacle.hasAppliedPenalty)
        {
            obstacle.hasAppliedPenalty = true;

            GlobalVars.speedMult = Mathf.Max(
                GlobalVars.speedMult * collisionSpeedMultiplier,
                minimumPlayerSpeed
            );
        }

        Die();
    }

    void Die()
    {
        // isDead = true;

        Debug.Log("Player hit an obstacle! Restarting...");

        // Invoke(nameof(RestartLevel), 0.5f);

        GlobalVars.speedMult = 0.5f;
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex
        );
    }
}