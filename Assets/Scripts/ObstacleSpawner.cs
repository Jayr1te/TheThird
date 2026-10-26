using UnityEngine;


[System.Serializable]
public class WeightedObstacle
{
    public GameObject prefab;

    [Min(0)]
    public float weight = 1f;

    public bool canBeJumped;
    public bool canBeSlid;
}

public class ObstacleSpawner : MonoBehaviour
{

    [Header("Obstacles")]
    public WeightedObstacle[] obstacles;

    [Header("Lanes")]
    public GameObject[] obstaclePrefabs; // drag your obstacle prefabs here
    public float laneWidth = 2f; // must match PlayerController
    public int laneCount = 3; // must match PlayerController

    [Header("Spawning")]
    public float spawnInterval = 1.2f;   // seconds between spawns
    private float timer;

    [Header("Row Spawn Weights")]
    [Min(0)] public float singleWeight = 75f;
    [Min(0)] public float doubleWeight = 20f;
    [Min(0)] public float tripleWeight = 5f;

    void Update()
    {
        timer += Time.deltaTime  * GlobalVars.speedMult;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnObstacle();
        }
    }

    // Reworked for the ability of spawning multiple objects across lanes
    void SpawnObstacle()
    {
        if (obstacles == null || obstacles.Length == 0)
            return;

        if (laneCount <= 0)
            return;

        int rowSize = Mathf.Min(GetRandomRowSize(), laneCount);

        // Build a list of available lane indices
        System.Collections.Generic.List<int> availableLanes =
            new System.Collections.Generic.List<int>();

        for (int i = 0; i < laneCount; i++)
        {
            availableLanes.Add(i);
        }
        System.Collections.Generic.List<int> selectedLanes =
            new System.Collections.Generic.List<int>();

        for (int i = 0; i < rowSize; i++)
        {
            int randomIndex = Random.Range(
                0, availableLanes.Count
            );

            selectedLanes.Add(availableLanes[randomIndex]);
            availableLanes.RemoveAt(randomIndex);
        }
        // Choose obstacles for each lane
        WeightedObstacle[] selectedObstacles =
            new WeightedObstacle[rowSize];

        bool hasSurvivableObstacle = false;

        for (int i = 0; i < rowSize; i++)
        {
            selectedObstacles[i] = GetRandomWeightedObstacle();

            if (selectedObstacles[i] == null)
                return;

            if (selectedObstacles[i].canBeJumped ||
                selectedObstacles[i].canBeSlid)
            {
                hasSurvivableObstacle = true;
            }
        }

        // Ensure atleast one obstacle can be slid/jumped over
        if (rowSize == laneCount && !hasSurvivableObstacle)
        {
            WeightedObstacle safeObstacle =
                GetRandomAvoidableObstacle();
            if (safeObstacle == null)
                return;

            int replacementIndex = Random.Range(0, rowSize);
            selectedObstacles[replacementIndex] = safeObstacle;
        }

        // Spawn the completed row
        for (int i = 0; i < rowSize; i++)
        {
            int lane = selectedLanes[i];

            float x = (lane - (laneCount - 1) / 2f)
                    * laneWidth;

            Instantiate(
                selectedObstacles[i].prefab,
                new Vector3(x, transform.position.y, 0f),
                Quaternion.identity
            );
        }
    }


    WeightedObstacle GetRandomWeightedObstacle()
    {
        return GetWeightedObstacle(false);
    }

    WeightedObstacle GetRandomAvoidableObstacle()
    {
        return GetWeightedObstacle(true);
    }

    WeightedObstacle GetWeightedObstacle(bool requireAvoidable)
    {
        float totalWeight = 0f;

        foreach (WeightedObstacle obstacle in obstacles)
        {
            if (obstacle.prefab == null || obstacle.weight <= 0f)
                continue;

            if (requireAvoidable &&
                !obstacle.canBeJumped &&
                !obstacle.canBeSlid)
                continue;

            totalWeight += obstacle.weight;
        }

        if (totalWeight <= 0f)
            return null;

        float roll = Random.value * totalWeight;

        foreach (WeightedObstacle obstacle in obstacles)
        {
            if (obstacle.prefab == null || obstacle.weight <= 0f)
                continue;

            if (requireAvoidable &&
                !obstacle.canBeJumped &&
                !obstacle.canBeSlid)
                continue;

            roll -= obstacle.weight;

            if (roll < 0f)
                return obstacle;
        }

        return null;
    }

    int GetRandomRowSize()
    {
        float totalWeight =singleWeight + doubleWeight + tripleWeight;

        if (totalWeight <= 0f)
            return 1;

        float roll = Random.value * totalWeight;

        if (roll < singleWeight)
            return 1;

        roll -= singleWeight;

        if (roll < doubleWeight)
            return 2;

        return 3;
    }
}
