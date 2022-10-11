using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private TMP_InputField widthInput;
    private TMP_InputField heightInput;

    private WaveFunctionCollapse wfc;

    // Start is called before the first frame update
    void Start()
    {
        wfc = GameObject.Find("WaveFunctionCollapse").GetComponent<WaveFunctionCollapse>();

        widthInput = GameObject.Find("WidthInput").GetComponent<TMP_InputField>();
        heightInput = GameObject.Find("HeightInput").GetComponent<TMP_InputField>();
    }

    public void RunWaveFunctionCollapse()
    {
        if(widthInput.text == "" && heightInput.text == "") wfc.Run(10,10);
        else
        {
            try
            {
                wfc.Run(int.Parse(widthInput.text), int.Parse(heightInput.text));
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}
