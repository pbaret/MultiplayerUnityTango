using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tango;

public class OfflineADFManager : MonoBehaviour, ITangoLifecycle, ITangoAreaDescriptionEvent
{
    TangoApplication tangoApp;
    GameObject listTogglePrefab;
    public GameObject adfListContent;


    private void Awake()
    {
        tangoApp = FindObjectOfType<TangoApplication>();
        listTogglePrefab = GetComponent<OfflineManager>().listTogglePrefab;
    }

    private void Start()
    {
        if (tangoApp != null)
        {
            tangoApp.Register(this);
            tangoApp.RequestPermissions();
        }
    }

    public bool SetupTangoSession()
    {
        Toggle toggled = adfListContent.GetComponent<ToggleGroup>().ActiveToggles().FirstOrDefault();
        if (toggled == null)
        {
            AndroidHelper.ShowAndroidToastMessage("No ADF selected.");
            return false;
        }
        else
        {
            AreaDescription adf = toggled.gameObject.GetComponent<ListToggleBehavior>().adf;
            GlobalData.current_adf = adf;
            adf.ExportToFile(Application.persistentDataPath);
            GlobalData.adf_path = Application.persistentDataPath + "/" + GlobalData.current_adf.m_uuid;

            return true;
        }
    }

    public void SetupTangoClientSession()
    {

    }


    #region TANGO LIFE CYCLE

    public void OnTangoPermissions(bool permissionsGranted)
    {
        if (permissionsGranted)
        {
            // Populate ADF scroll list
            AreaDescription[] listADF = AreaDescription.GetList();

            foreach (AreaDescription areaDescription in listADF)
            {
                GameObject listElt = GameObject.Instantiate(listTogglePrefab, adfListContent.transform);
                ListToggleBehavior listEltBehavior = listElt.GetComponent<ListToggleBehavior>();
                listEltBehavior.isADF = true;
                listEltBehavior.isSession = false;
                listEltBehavior.adf = areaDescription;
                listEltBehavior.toggle.group = adfListContent.GetComponent<ToggleGroup>();
                listEltBehavior.label.text = areaDescription.GetMetadata().m_name;
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
        for (int i = 0; i < 100; i++)
            Debug.Log("CALL OnAreaDescriptionImported");

        if (isSuccessful)
        {
            GlobalData.current_adf = areaDescription;
            CustomNetworkManager netManager = FindObjectOfType<CustomNetworkManager>();
            netManager.AddPlayer(CustomNetworkManager.PLAYER_TYPES.TANGO, netManager.client.connection);
        }
    }

    #endregion
}
