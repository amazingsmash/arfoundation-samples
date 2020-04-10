using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TapToPlace : MonoBehaviour
{
    public ARWorldMapController arController;
    public GameObject prefab = null;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (arController)
        {
            //// Do a raycast into the world that will only hit the Spatial Mapping mesh.
            //var headPosition = Camera.main.transform.position;
            //var gazeDirection = Camera.main.transform.forward;

            //RaycastHit hitInfo;
            //if (Physics.Raycast(headPosition, gazeDirection, out hitInfo,
            //30.0f, spatialMappingManager.LayerMask))
            //{
            //    // Move this object to where the raycast
            //    // hit the Spatial Mapping mesh.
            //    // Here is where you might consider adding intelligence
            //    // to how the object is placed. For example, consider
            //    // placing based on the bottom of the object's
            //    // collider so it sits properly on surfaces.
            //    this.transform.position = hitInfo.point;

            //    // Rotate this object to face the user.
            //    Quaternion toQuat = Camera.main.transform.localRotation;
            //    toQuat.x = 0;
            //    toQuat.z = 0;
            //    this.transform.rotation = toQuat;
            //}
        }
    }
}
