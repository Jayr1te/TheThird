using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    [Header("Lanes")]
    public float laneWidth = 2f;        // distance between lane centers
    public int laneCount = 3;           // total number of lanes
    public float laneSwitchSpeed = 15f; // how fast the player slides between lanes

    [Header("Jump")]
    public float jumpDuration = 0.6f;   // seconds in the air
    public float jumpScale = 1.5f;      // how big the player gets at the peak (fake height)

    private int currentLane;
    private float targetX;
    private bool isJumping;
    private bool isDead;
    private Vector3 baseScale;

    void Start()
    {
        baseScale = transform.localScale;

        // Start in the middle lane
        currentLane = laneCount / 2;
        targetX = LaneToX(currentLane);
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        GlobalVars.speedMult = 1f;
        GlobalVars.distance = 0f;
        GlobalVars.fireDistance = -15f;
        GlobalVars.fireSpeed = 0.5f;
    }

    void Update()
    {
        if (isDead) return;

        HandleInput();

        // Smoothly slide toward the target lane
        float newX = Mathf.MoveTowards(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);

        GlobalVars.distance += GlobalVars.speedMult * Time.deltaTime;
        GlobalVars.fireDistance += GlobalVars.fireSpeed * Time.deltaTime;
        GlobalVars.speedMult += 0.1f * Time.deltaTime;
        GlobalVars.fireSpeed += 0.025f * Time.deltaTime;

        if (GlobalVars.distance <= GlobalVars.fireDistance)
        {
            Invoke(nameof(RestartLevel), 0.0f);
        }

    }

    void HandleInput()
    {
        // Move left
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            ChangeLane(-1);
        }

        // Move right
        if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            ChangeLane(1);
        }

        // Jump (can't jump while already in the air)
        if ((Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) && !isJumping)
        {
            StartCoroutine(Jump());
        }
    }

    void ChangeLane(int direction)
    {
        currentLane = Mathf.Clamp(currentLane + direction, 0, laneCount - 1);
        targetX = LaneToX(currentLane);
    }

    // Converts a lane index (0, 1, 2...) into a world X position centered on x = 0
    float LaneToX(int lane)
    {
        return (lane - (laneCount - 1) / 2f) * laneWidth;
    }

    // In a top-down view, "height" is faked by scaling the sprite up and back down
    IEnumerator Jump()
    {
        isJumping = true;
        float timer = 0f;

        while (timer < jumpDuration)
        {
            timer += Time.deltaTime;
            float arc = Mathf.Sin(timer / jumpDuration * Mathf.PI); // 0 -> 1 -> 0
            transform.localScale = baseScale * Mathf.Lerp(1f, jumpScale, arc);
            yield return null;
        }

        transform.localScale = baseScale;
        isJumping = false;
    }

    // Runs every physics step while overlapping a trigger collider
    void OnTriggerStay2D(Collider2D other)
    {
        if (isDead) return;

        Obstacle obstacle = other.GetComponent<Obstacle>();
        if (obstacle == null) return;

        // Safe only if we're in the air AND the obstacle is low enough to jump over
        if (isJumping && obstacle.canBeJumped)
        {
            GlobalVars.speedMult += 0.1f;
            return;
        };

        Die();
    }

    void Die()
    {
        //isDead = true;
        Debug.Log("Player hit an obstacle! Restarting...");
        //Invoke(nameof(RestartLevel), 0.5f);
        GlobalVars.speedMult = 0.5f;
    }

    void RestartLevel()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
