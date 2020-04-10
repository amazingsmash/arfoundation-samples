using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SharedARUIManager : MonoBehaviour
{

    public static string sharedARStatusMessage = "";
    [SerializeField] private Text screenMessageText = null;
    [SerializeField] private Text camPositionText = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (sharedARStatusMessage != screenMessageText.text)
        {
            screenMessageText.text = sharedARStatusMessage;
            Debug.Log(sharedARStatusMessage);
        }

        if (camPositionText != null)
        {
            Vector3 camPos = Camera.main.transform.position;
            camPositionText.text = $"Cam at: {camPos}";
        }
    }
}
