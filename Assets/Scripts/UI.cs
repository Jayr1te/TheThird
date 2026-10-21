using TMPro;
using UnityEngine;

public class UI : MonoBehaviour
{

    public GameObject distanceText;
    public GameObject fireDistanceText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        distanceText.GetComponent<TextMeshProUGUI>().text = "Distance: " + System.MathF.Round(GlobalVars.distance,1).ToString() + "m";
        fireDistanceText.GetComponent<TextMeshProUGUI>().text = System.MathF.Round(GlobalVars.distance - GlobalVars.fireDistance,1).ToString() + "m";
    }
}
