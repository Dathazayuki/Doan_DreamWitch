using UnityEngine;

public class RotateZ : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 100f;

    void Update()
    {
        transform.Rotate(0f, 0f, -rotateSpeed * Time.deltaTime);
    }
}