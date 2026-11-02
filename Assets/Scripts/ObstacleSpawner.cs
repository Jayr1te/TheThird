
using System.Collections.Generic;
using TMPro;
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
    public float laneWidth = 2f; // must match PlayerController
    public int laneCount = 3; // must match PlayerController

    [Header("Spawning")]
    public float spawnInterval = 1.2f;   // seconds between spawns

    [Header("Lookahead")]
    public float lookaheadDistance = 70f;
    public float physicalSpawnDistance = 35f;
    public float cleanupDistance = 5f;

    [Header("References")]
    public Transform player;
    public TMP_Text[] laneWarnings;

    [Header("Camera")]
    public Camera gameCamera;

    [Header("Difficulty")]
    public float minimumRowSpacing = 12f;
    public float minimumTimeBetweenRows = 1.5f;

    [Header("Row Spawn Weights")]
    [Min(0)] public float singleWeight = 75f;
    [Min(0)] public float doubleWeight = 20f;
    [Min(0)] public float tripleWeight = 5f;


    private class PlannedRow
    {
        public float encounterDistance;
        public WeightedObstacle[] lanes;
        public GameObject[] instances;
        public bool spawned;
    }

    // Upcoming row list for incoming text displays
    private readonly List<PlannedRow> plannedRows = new List<PlannedRow>();

    private float nextRowDistance;

    void Start()
    {
        nextRowDistance =GlobalVars.distance + GetRowSpacing();
    }

    void Update()
    {
        if (player == null || laneCount <= 0)
            return;

        PlanUpcomingRows();
        UpdatePhysicalObstacles();
        CleanupPassedRows();
        UpdateWarnings();
    }


    PlannedRow GenerateRow(float encounterDistance)
    {
        if (obstacles == null || obstacles.Length == 0)
            return null;

        PlannedRow row = new PlannedRow();

        row.encounterDistance = encounterDistance;
        row.lanes = new WeightedObstacle[laneCount];
        row.instances = new GameObject[laneCount];

        int rowSize = Mathf.Min(
            GetRandomRowSize(), laneCount
        );

        List<int> availableLanes = new List<int>();

        for (int i = 0; i < laneCount; i++)
            availableLanes.Add(i);

        for (int i = 0; i < rowSize; i++)
        {
            int index = Random.Range(
                0, availableLanes.Count
            );

            int lane = availableLanes[index];
            availableLanes.RemoveAt(index);

            row.lanes[lane] = GetWeightedObstacle(false);

            if (row.lanes[lane] == null)
                return null;
        }

        if (rowSize == laneCount)
        {
            bool hasSafeOption = false;

            for (int i = 0; i < laneCount; i++)
            {
                WeightedObstacle obstacle = row.lanes[i];

                if (obstacle.canBeJumped || obstacle.canBeSlid)
                {
                    hasSafeOption = true;
                    break;
                }
            }

            if (!hasSafeOption)
            {
                WeightedObstacle safe = GetWeightedObstacle(true);

                if (safe == null)
                    return null;

                int replacement = Random.Range(
                    0, laneCount
                );

                row.lanes[replacement] = safe;
            }
        }

        return row;
    }

    void SpawnPhysicalRow(PlannedRow row)
    {
        row.spawned = true;

        for (int lane = 0; lane < laneCount; lane++)
        {
            WeightedObstacle obstacle = row.lanes[lane];

            if (obstacle == null || obstacle.prefab == null)
                continue;

            float remaining =
                row.encounterDistance - GlobalVars.distance;

            Vector3 position = new Vector3(
                LaneToX(lane),
                player.position.y + remaining,
                0f
            );

            row.instances[lane] = Instantiate(
                obstacle.prefab,
                position,
                Quaternion.identity
            );
        }
    }

    void UpdatePhysicalObstacles()
    {
        foreach (PlannedRow row in plannedRows)
        {
            float remaining =
                row.encounterDistance - GlobalVars.distance;
            if (!row.spawned &&
                remaining <= physicalSpawnDistance &&
                remaining >= 0f)
            {
                SpawnPhysicalRow(row);
            }

            if (!row.spawned)
                continue;

            for (int lane = 0; lane < laneCount; lane++)
            {
                GameObject instance = row.instances[lane];

                if (instance == null)
                    continue;

                float x = LaneToX(lane);

                instance.transform.position = new Vector3(
                    x,
                    player.position.y + remaining,
                    instance.transform.position.z
                );
            }
        }
    }

    bool IsRowOffScreen(PlannedRow row, int lane)
    {
        if (gameCamera == null)
            return false;

        float remaining =
            row.encounterDistance - GlobalVars.distance;

        // The row already reached player
        if (remaining <= 0f)
            return false;

        float x = LaneToX(lane);
        float y = player.position.y + remaining;
        float z = 0f;
        GameObject instance = row.instances[lane];

        if (instance != null)
        {
            Vector3 position = instance.transform.position;
            x = position.x;
            y = position.y;
            z = position.z;

            Renderer renderer = instance.GetComponentInChildren<Renderer>();

            if (renderer != null)
            {
                y = renderer.bounds.min.y;
            }
        }

        Vector3 viewportPosition =
            gameCamera.WorldToViewportPoint(
                new Vector3(x, y, z)
            );

        return viewportPosition.z > 0f &&
            viewportPosition.y > 1f;
    }

    void PlanUpcomingRows()
    {
        while (nextRowDistance <= GlobalVars.distance + lookaheadDistance)
        {
            PlannedRow row = GenerateRow(nextRowDistance);

            if (row != null)
                plannedRows.Add(row);

            nextRowDistance += GetRowSpacing();
        }
    }

    // Render the upcoming warning displays for incoming obstacles
    void UpdateWarnings()
    {
        if (laneWarnings == null)
            return;

        for (int lane = 0;
             lane < laneCount && lane < laneWarnings.Length;
             lane++)
        {
            TMP_Text warning = laneWarnings[lane];

            if (warning == null)
                continue;

            WeightedObstacle nextObstacle = null;
            float nearestDistance = float.MaxValue;

            foreach (PlannedRow row in plannedRows)
            {
                float remaining =
                    row.encounterDistance - GlobalVars.distance;

                if (remaining <= 0f)
                    continue;

                WeightedObstacle candidate = row.lanes[lane];

                if (candidate == null)
                    continue;

                // Ignore obstacles already visible on screen.
                if (!IsRowOffScreen(row, lane))
                    continue;

                if (candidate == null)
                    continue;

                if (remaining < nearestDistance)
                {
                    nearestDistance = remaining;
                    nextObstacle = candidate;
                }
            }

            if (nextObstacle == null)
            {
                warning.text = "";
                continue;
            }

            string action;

            if (nextObstacle.canBeJumped &&
                nextObstacle.canBeSlid)
            {
                action = "↑ ↓";
            }
            else if (nextObstacle.canBeJumped)
            {
                action = "↑";
            }
            else if (nextObstacle.canBeSlid)
            {
                action = "↓";
            }
            else
            {
                action = "X";
            }

            warning.text =
                $"{Mathf.CeilToInt(nearestDistance)}m\n"+
                $"{action}\n";
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

    float LaneToX(int lane)
    {
        return (lane - (laneCount - 1) / 2f) * laneWidth;
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

    float GetRowSpacing()
    {
        float actualSpeed =
            GlobalVars.speedMult * 6f;

        return Mathf.Max(
            minimumRowSpacing,
            actualSpeed * minimumTimeBetweenRows
        );
    }

    // Cleanup
    void CleanupPassedRows()
    {
        for (int i = plannedRows.Count - 1; i >= 0; i--)
        {
            PlannedRow row = plannedRows[i];

            float remaining =
                row.encounterDistance - GlobalVars.distance;

            if (remaining >= -cleanupDistance)
                continue;

            foreach (GameObject instance in row.instances)
            {
                if (instance != null)
                    Destroy(instance);
            }

            plannedRows.RemoveAt(i);
        }
    }

}
