using UnityEngine;

public class TruckCutsceneMover : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public float speed = 5f;

    private bool isMoving = true;

    private void Start()
    {
        if (startPoint != null)
        {
            transform.position = startPoint.position;
        }
    }

    private void Update()
    {
        if (!isMoving || startPoint == null || endPoint == null) return;

        // Move towards the end point at a constant speed
        transform.position = Vector3.MoveTowards(
            transform.position,
            endPoint.position,
            speed * Time.deltaTime
        );

        // Stop when we reach the end
        if (Vector3.Distance(transform.position, endPoint.position) < 0.01f)
        {
            isMoving = false;
        }
    }
}
