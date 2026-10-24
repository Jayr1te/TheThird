using UnityEngine;

public class ObstacleSpawner : MonoBehaviour
{
    public GameObject[] obstaclePrefabs; // drag your obstacle prefabs here
    public float laneWidth = 2f;         // must match PlayerController
    public int laneCount = 3;            // must match PlayerController
    public float spawnInterval = 1.2f;   // seconds between spawns

    private float timer;

    void Update()
    {
        timer += Time.deltaTime  * GlobalVars.speedMult;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnObstacle();
        }
    }

    void SpawnObstacle()
    {
        if (obstaclePrefabs.Length == 0) return;

        int lane = Random.Range(0, laneCount);
        float x = (lane - (laneCount - 1) / 2f) * laneWidth;

        GameObject prefab = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
        Instantiate(prefab, new Vector3(x, transform.position.y, 0f), Quaternion.identity);
    }
}
