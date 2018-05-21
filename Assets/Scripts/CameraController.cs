using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class CameraController : MonoBehaviour
{
    public Transform lightTransform;
    public Transform cameraTransform;
    public Transform targetTransform;

    public float backDistance = 25f;
    public float upDistance = 25f;

    private float vertRotation = 0f;
    private float horizRotation = 0f;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        backDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * 750f;

        if (backDistance < 0.1f) backDistance = 0.1f;
        /*else if (backDistance > 30f) backDistance = 30f;*/

        if (Input.GetMouseButton(0))
        {
            horizRotation += Input.GetAxis("Mouse X") * Time.deltaTime * 400f;
            vertRotation -= Input.GetAxis("Mouse Y") * Time.deltaTime * 400f;

            /*if (vertRotation > 85f) vertRotation = 85f;
            else if (vertRotation < -10f) vertRotation = -10f;*/
        }
        
        if (Input.GetMouseButton(1))
        {
            lightTransform.Rotate(Input.GetAxis("Mouse Y") * Time.deltaTime * 400f, Input.GetAxis("Mouse X") * Time.deltaTime * 400f, 0, Space.World);
        }

        if (Input.GetMouseButton(2))
        {
            upDistance -= Input.GetAxis("Mouse Y") * Time.deltaTime * backDistance * 2f;

            /*if (upDistance > 15f) upDistance = 15f;
            else if (upDistance < 1f) upDistance = 1f;*/

            targetTransform.Translate(-cameraTransform.right * Input.GetAxis("Mouse X") * Time.deltaTime * backDistance * 2f);
        }
    }

    void LateUpdate()
    {
        cameraTransform.position = targetTransform.position;
        cameraTransform.rotation = transform.rotation;

        cameraTransform.Rotate(vertRotation, horizRotation, 0);

        cameraTransform.Translate(0, upDistance, 0);
        cameraTransform.Translate(0, 0, -backDistance);

        if (EditorController.instance.plane.activeSelf && cameraTransform.position.y < 0.2f) cameraTransform.position = new Vector3(cameraTransform.position.x, 0.2f, cameraTransform.position.z);
    }
}
