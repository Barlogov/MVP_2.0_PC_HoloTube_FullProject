using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CameraSliders : MonoBehaviour
{
    public GameObject OnlineModelCamera_Down;
    public GameObject OnlineModelCamera_Up;

    public Slider ZCameraSlider;
    public Slider UpCameraSlider;
    public Slider DownCameraSlider;

    public Text ZSliderText;
    public Text UpSliderText;
    public Text DownSliderText;
    

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        OnlineModelCamera_Down.transform.position = new Vector3(0, DownCameraSlider.value, -20);
        OnlineModelCamera_Down.GetComponent<Camera>().orthographicSize = ZCameraSlider.value;
        DownSliderText.text = OnlineModelCamera_Down.transform.position.y.ToString();

        //ZCameraSlider.value
        OnlineModelCamera_Up.transform.position = new Vector3(0, UpCameraSlider.value, -20);
        OnlineModelCamera_Up.GetComponent<Camera>().orthographicSize = ZCameraSlider.value;
        UpSliderText.text = OnlineModelCamera_Up.transform.position.y.ToString();

        ZSliderText.text = OnlineModelCamera_Down.GetComponent<Camera>().orthographicSize.ToString();
        //ZSliderText.text = OnlineModelCamera_Up.transform.position.z.ToString();

    }
}
