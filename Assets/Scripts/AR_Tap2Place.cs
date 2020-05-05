using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;
using UnityEngine.UI;

public class AR_Tap2Place : MonoBehaviour
{
    public GameObject placementIndicator;
    public GameObject objectToPlace;
    public Text countText;

    List<GameObject> allObjects = new List<GameObject>();

    private ARSessionOrigin arOrigin;
    private Pose placementPose;
    private bool validPlacementPose = false;

    // Start is called before the first frame update
    void Start()
    {
        arOrigin = FindObjectOfType<ARSessionOrigin>();
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
        if (validPlacementPose && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            PlaceObject();
        }

        // keep our counter updated with list total
        countText.text = "Number of Objects in scene: " + allObjects.Count;
    }

    // called from the UI button
    public void ClearScene()
    {
        foreach (GameObject obj in allObjects)
        {
            Destroy(obj);
        }
        allObjects.Clear();

    }

    private void PlaceObject()
    {
        // store each obj we create into a list
        GameObject go = Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
        allObjects.Add(go);
    }

    private void UpdatePlacementIndicator()
    {

        /*  because the indicator doesn't get spawned, but rather sits active in the scene, we need to handle
            when we will hide and show it
            */

        // make sure we have a place to register as ground zero for the indicator
        if (validPlacementPose)
        {
            // we do so enable it
            placementIndicator.SetActive(true);
            // and update its position and rotation
            placementIndicator.transform.SetPositionAndRotation(placementPose.position, placementPose.rotation);
        }
        else
        {
            // no plane detected yet, or we lost it, so hide the indicator
            placementIndicator.SetActive(false);
        }
    }

    private void UpdatePlacementPose()
    {
        /* we need to shoot a ray straight out from screen center*/
        // find screen center
        var screenCenter = Camera.current.ViewportToScreenPoint(new Vector3(0.5f, 0.5f, 0.5f));
        // store what we've hit
        var hits = new List<ARRaycastHit>();
        // perform the raycast
        arOrigin.GetComponent<ARRaycastManager>().Raycast(screenCenter, hits, UnityEngine.XR.ARSubsystems.TrackableType.Planes);

        /* now, once we have successfully stored a hit, we need to turn on the indicator
        so we can see the plane of where we hit,  we'll track that with validPlacementPose */

        // make sure we've hit something first
        validPlacementPose = hits.Count > 0;
        if (validPlacementPose)
        {
            // the first thing we successfully register is where we will place the indicator
            placementPose = hits[0].pose;
            // orient indicator icon with camera
            var cameraForward = Camera.current.transform.forward;
            var cameraBearing = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;
            placementPose.rotation = Quaternion.LookRotation(cameraBearing);
        }
    }
}
