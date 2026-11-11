using UnityEngine;

public class CameraBehavior : MonoBehaviour
{
    public Transform target;

    public Vector3 camOffset = new Vector3(0f, 2.5f, -4f);

    public float sensitivity = 3f;
    public float minPitch = -20f;
    public float maxPitch = 60f;

    private float yaw;
    private float pitch;

    void Start()
    {
        if (target == null)
        {
            target = GameObject.Find("Player").transform;
        }

        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        // движение мыши
        yaw += Input.GetAxis("Mouse X") * sensitivity;
        pitch -= Input.GetAxis("Mouse Y") * sensitivity;

        //вращение
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        // положение камеры относительно игрока
        transform.position = target.position + rotation * camOffset;
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}