using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RainDemo : MonoBehaviour {

    public GameObject[] prefabs;
    public GameObject windZoneObj;
    public Text[] buttonText;
    Color defaultColor = new Color(50f / 255f, 50f / 255f, 50f / 255f);

    //Light Rain
    public void ShowLightRain()
    {
        HideVFX();
        ResetTextColor();

        prefabs[0].SetActive(true);

        buttonText[0].color = Color.red;
    }

    //Moderate Rain
    public void ShowModerateRain()
    {
        HideVFX();
        ResetTextColor();

        prefabs[1].SetActive(true);

        buttonText[1].color = Color.red;
    }

    //Heavy Rain
    public void ShowHeavyRain()
    {
        HideVFX();
        ResetTextColor();

        prefabs[2].SetActive(true);

        buttonText[2].color = Color.red;
    }

    public void ToggleWindZoneChanged(bool Selected) {
        windZoneObj.SetActive(Selected);
    }

    void HideVFX()
    {
        for (int i = 0; i < prefabs.Length; i++)
        {
            prefabs[i].SetActive(false);
        }
    }

    void ResetTextColor()
    {
        for (int i = 0; i < buttonText.Length; i++)
        {
            buttonText[i].color = defaultColor;
        }
    }
}
