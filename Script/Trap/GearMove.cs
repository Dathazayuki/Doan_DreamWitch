using UnityEngine;

public class GearMover : MonoBehaviour
{
    [Header("Move Points")]
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;

    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 2f;

    private Transform targetPoint;

    private void Start()
    {
        targetPoint = pointB;
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        transform.position = Vector2.MoveTowards(
            transform.position,
            targetPoint.position,
            moveSpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, targetPoint.position) < 0.05f)
        {
            targetPoint = targetPoint == pointA ? pointB : pointA;
        }
    }

    private void OnDrawGizmos()
    {
        if (pointA == null || pointB == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pointA.position, 0.15f);

        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(pointB.position, 0.15f);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(pointA.position, pointB.position);
    }
}