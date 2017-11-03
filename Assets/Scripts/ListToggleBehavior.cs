using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Tango;

public class ListToggleBehavior : MonoBehaviour
{
    public Text label;
    public Toggle toggle;
    [SerializeField]
    private Image background;
    [SerializeField]
    private Color bgColorUnchecked;
    [SerializeField]
    private Color bgColorChecked;

    public bool isSession;
    public bool isADF;

    public AreaDescription adf;
    public string networkAdress;
    public int networkPort;


    void Start()
    {
        ToggleChanged();
    }


    public void ToggleChanged()
    {
        if (toggle.isOn)
        {
            label.color = bgColorUnchecked;
            background.color = bgColorChecked;
        }
        else
        {
            label.color = bgColorChecked;
            background.color = bgColorUnchecked;
        }
    }
}
