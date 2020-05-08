using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.Experimental.XR;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

public class AR_Tap2Place : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Shown when a surface has been detected as the orgin for spawning")]
    private GameObject placementIndicator;

    [SerializeField]
    [Tooltip("Mesh to spawn on surface")]
    private GameObject objectToPlace;

    [SerializeField]
    [Tooltip("UI Element to target for updates")]
    public Text countText;


    [SerializeField]
    [Tooltip("Active Color for Selected Objects")]
    private Color activeColor = Color.blue;

    [SerializeField]
    [Tooltip("InActive Color for De-Selected Objects")]
    private Color inActiveColor = Color.red;

    [SerializeField]
    [Tooltip("Camera to use for ray casting")]
    private Camera AR_Camera = default;

    List<GameObject> allObjects = new List<GameObject>();

    private ARSessionOrigin arOrigin;
    private Pose placementPose;
    private bool validPlacementPose = false;
    private Vector2 touchPosition = default;

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
        HandleSelectionDetection();

        // keep our counter updated with list total
        countText.text = "Number of Objects in scene: " + allObjects.Count;
    }

    // called from the UI button
    public void ClearScene()
    {
        foreach (GameObject obj in allObjects)
        {
            Debug.Log("Destroy" + allObjects.Count);
            Destroy(obj);
        }
        allObjects.Clear();

    }

    private void HandleSelectionDetection()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            touchPosition = touch.position;

            if (touch.phase == TouchPhase.Began)
            {
                Ray ray = AR_Camera.ScreenPointToRay(touch.position);
                RaycastHit hitObject;


                if (Physics.Raycast(ray, out hitObject))
                {
                    GameObject placementObj = hitObject.transform.gameObject;
                    if (placementObj != null)
                    {
                        ChangeSelection(placementObj);
                    }
                    else
                    {
                        PlaceObject();
                    }
                }
            }

        }
    }

    private void ChangeSelection(GameObject selected)
    {
        foreach (GameObject obj in allObjects)
        {
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            meshRenderer.material.color = (selected != obj) ? inActiveColor : activeColor;
        }
    }

    private void PlaceObject()
    {
        // store each obj we create into a list
        GameObject aro = Instantiate(objectToPlace, placementPose.position, placementPose.rotation);
        allObjects.Add(aro);
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
        arOrigin.GetComponent<ARRaycastManager>().Raycast(screenCenter, hits, TrackableType.PlaneWithinPolygon);

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
