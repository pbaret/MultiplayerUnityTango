using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tango;

public class OnlineADFManager : MonoBehaviour, ITangoLifecycle, ITangoAreaDescriptionEvent
{
    TangoApplication tangoApp;

    private void Awake()
    {
        tangoApp = FindObjectOfType<TangoApplication>();

        if (tangoApp != null)
        {
            tangoApp.Register(this);
        }
    }

    public void StartTango()
    {
        DestroyImmediate(GameObject.Find("Main Camera"));

        if (tangoApp != null)
        {
            tangoApp.Register(this);
            tangoApp.RequestPermissions();
        }
    }

    #region TANGO LIFE CYCLE

    public void OnTangoPermissions(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            if (GlobalData.current_adf != null)
            {
                tangoApp.Startup(GlobalData.current_adf);
            }
            else
            {
                // No Area Descriptions available.
                Debug.LogError("No area descriptions has been set.");
                AndroidHelper.ShowAndroidToastMessage("ERROR: no ADF has been set!");
            }
        }
    }
    
    public void OnTangoServiceConnected()
    {
    }

    public void OnTangoServiceDisconnected()
    {
    }

    #endregion

    #region TANGO AREADESCRIPTION EVENT

    public void OnAreaDescriptionExported(bool isSuccessful)
    {
        Debug.Log("CALL OnAreaDescriptionExported");
    }

    public void OnAreaDescriptionImported(bool isSuccessful, AreaDescription areaDescription)
    {
        Debug.Log("CALL OnAreaDescriptionImported  " + AreaDescription.GetList().Length);

        if (isSuccessful)
        {
            Debug.Log("ADF : " + areaDescription.m_uuid + " " + areaDescription.GetMetadata().m_name);
            GlobalData.current_adf = areaDescription;
            CustomNetworkManager netManager = FindObjectOfType<CustomNetworkManager>();
            netManager.AddPlayer(CustomNetworkManager.PLAYER_TYPES.TANGO, netManager.client.connection);
        }
        else
        {
            Debug.Log("OnAreaDescriptionImported: FAILED!");
        }
    }

    #endregion
}
