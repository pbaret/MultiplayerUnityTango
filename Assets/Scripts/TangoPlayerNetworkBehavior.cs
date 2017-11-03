using System;
using UnityEngine;
using UnityEngine.Networking;

public class TangoPlayerNetworkBehavior : NetworkBehaviour
{
    public GameObject mesh;
    Transform tangoCamera;

    private void Start()
    {
        if (isLocalPlayer)
        {
            Debug.Log("Tango Player created");
            mesh.SetActive(false);
            tangoCamera = FindObjectOfType<TangoPoseController>().transform;
            FindObjectOfType<OnlineADFManager>().StartTango();
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
        {
            transform.position = tangoCamera.position;
            transform.rotation = tangoCamera.rotation;
        }
    }
}
