using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Tooltip("Tick = player can jump over it. Untick = player must dodge it by changing lanes.")]
    public bool canBeJumped = true;
    public bool canBeSlid = true;

    public float speed = 6f;      // how fast it moves down the screen
    public float destroyY = -7f;  // when it goes below this Y, it gets deleted

    void Update()
    {
        transform.Translate(Vector3.down * speed * Time.deltaTime * GlobalVars.speedMult, Space.World);

        if (transform.position.y < destroyY)
        {
            Destroy(gameObject);
        }
    }
}
