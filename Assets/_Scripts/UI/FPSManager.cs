using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FPSManager : MonoBehaviour
{
    public Toggle FPSLimiterToggle;
    public Toggle VsyncToggle;
    public InputField FPSInputField;
    public Text FPSText;

    float fps;
    private IEnumerator FPS()
    {
        while (true)
        {
            fps = 1f / Time.unscaledDeltaTime;
            yield return new WaitForSeconds(0.1f);
        }
    }

    private void Start()
    {
        SetVsync();
        SetFPSLimiter();
        StartCoroutine(FPS());
    }

    private void Update()
    {
        FPSText.text = Mathf.Ceil(fps).ToString();
    }

    public void SetFPSLimiter()
    {
        if (FPSLimiterToggle.isOn)
        {
            try
            {
                Application.targetFrameRate = int.Parse(FPSInputField.text);
            }
            catch
            {
                Application.targetFrameRate = 60;
            }

        }
        else
        {
            Application.targetFrameRate = -1;
        }
    }

    public void SetVsync()
    {
        if (VsyncToggle.isOn)
        {
            QualitySettings.vSyncCount = 1;
        }
        else
        {
            QualitySettings.vSyncCount = 0;
        }
    }

}
