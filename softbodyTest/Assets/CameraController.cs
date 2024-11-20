using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{

    InputAction fly;
    float speed = 20;

    InputAction rotate;
    float rotateSpeed = 60;

    // Start is called before the first frame update
    void Start()
    {
        fly = InputSystem.actions.FindAction("Fly");
        rotate = InputSystem.actions.FindAction("Rotate");
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 flyValue = fly.ReadValue<Vector3>();
        transform.Translate(flyValue * speed * Time.deltaTime, Space.Self);

        Vector2 rotateValue = rotate.ReadValue<Vector2>();
        transform.Rotate(new Vector3(rotateValue.y, rotateValue.x, 0) * Time.deltaTime * rotateSpeed); ;

    }
}
