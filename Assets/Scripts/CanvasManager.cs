using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    private TMP_InputField widthInput;
    private TMP_InputField heightInput;
    private TMP_InputField speedInput;

    private WaveFunctionCollapse wfc;

    // Start is called before the first frame update
    void Start()
    {
        wfc = GameObject.Find("WaveFunctionCollapse").GetComponent<WaveFunctionCollapse>();

        widthInput = GameObject.Find("WidthInput").GetComponent<TMP_InputField>();
        heightInput = GameObject.Find("HeightInput").GetComponent<TMP_InputField>();
        speedInput = GameObject.Find("SpeedInput").GetComponent<TMP_InputField>();
    }

    public void RunWaveFunctionCollapse()
    {
        int width = 10;
        int height = 10;
        float speed = 0.2f;

        try
        {
            width = int.Parse(widthInput.text);
        }
        catch { }

        try
        {
            height = int.Parse(heightInput.text);
        }
        catch { }

        try
        {
            speed = float.Parse(speedInput.text);
        }
        catch { }

        try
        {
            wfc.Run(width, height, speed);
        }
        catch (System.Exception e)
        {
            Debug.Log(e);
        }
    }

    public void Exit()
    {
        Application.Quit();
    }
}
