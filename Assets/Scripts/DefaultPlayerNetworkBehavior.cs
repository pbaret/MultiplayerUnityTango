using System;
using UnityEngine;
using UnityEngine.Networking;

public class DefaultPlayerNetworkBehavior : NetworkBehaviour
{
    float translate_speed = 15f;
    float rot_speed = 30f;

    private void Update()
    {
        if (isLocalPlayer)
        {
            transform.Translate(Input.GetAxis("Horizontal") * Time.deltaTime * translate_speed, 0f, Input.GetAxis("Vertical") * Time.deltaTime * translate_speed, Space.Self);
            transform.Rotate(0f, Input.GetAxis("Mouse X") * Time.deltaTime * rot_speed, 0f, Space.World);
            transform.Rotate(-Input.GetAxis("Mouse Y") * Time.deltaTime * rot_speed, 0f, 0f, Space.Self);
        }
    }
}

