﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

public class AR_Tap2Place : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Shown when a surface has been detected as the orgin for spawning")]
    private GameObject placementIndicator;

    [SerializeField]
    [Tooltip("Mesh to spawn on surface")]
    private AR_Object objectToPlace;

    [SerializeField]
    [Tooltip("UI Element to target for updates")]
    public Text countText;

    [SerializeField]
    [Tooltip("UI Element to target for object description display")]
    public Text descriptionText;


    [SerializeField]
    [Tooltip("Active Color for Selected Objects")]
    private Color activeColor = Color.blue;

    [SerializeField]
    [Tooltip("InActive Color for De-Selected Objects")]
    private Color inActiveColor = Color.red;

    [SerializeField]
    [Tooltip("Camera to use for ray casting")]
    private Camera AR_Camera = default;

    [SerializeField]
    [Tooltip("Do we limit spawning to a single mesh?")]
    private bool isSingleSpawn = false;

    [SerializeField]
    [Tooltip("Do we destroy the previous mesh with each spawn?")]
    private bool deletePrevious = false;

    /*  ================================================  */


    [System.Serializable]
    public class jsonObject
    {
        public int id;
        public string name;
        public int age;
    }


    /*  ================================================  */
    List<AR_Object> allObjects = new List<AR_Object>();

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private ARSessionOrigin arOrigin;
    private Pose placementPose;
    private bool validPlacementPose = false;
    private Vector2 touchPosition = default;

    private AR_Object singleMesh;

    private AR_Object selectedObject;

    private bool onTouchHold = false;
    private List<jsonObject> objList;

    public GameObject holdText;
    // Start is called before the first frame update
    void Start()
    {
        arOrigin = FindObjectOfType<ARSessionOrigin>();
        // clear out all the UI text fields
        descriptionText.text = "";
        countText.text = "";

        // setup fetch
        StartCoroutine(GetRequest("https://my-json-server.typicode.com/bauervision/unity_AR_base/objects"));


        // testing
        // holdText = GameObject.Find("Hold");
        // holdText.SetActive(false);
    }

    IEnumerator GetRequest(string uri)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = uri.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            }
            else
            {
                var data = webRequest.downloadHandler.text;
                Debug.Log(" data: " + data[1]);


                //    for (int i = 0; i < data; i++)
                //     {
                //         objList.Add(data[i]);
                //     }
                //     Debug.Log(" data: " + objList);

            }
        }
    }



    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
        HandleSelectionDetection();

        // keep our counter updated with list total once we have something to count
        if (allObjects.Count != 0)
        {
            countText.text = "Number of Objects in scene: " + allObjects.Count;
        }

        if (onTouchHold)
        {
            handleDragging();
        }


        // testing
        holdText.SetActive(onTouchHold);

    }

    // called from the UI button
    public void ClearScene()
    {
        foreach (AR_Object obj in allObjects)
        {
            Destroy(obj.transform.gameObject);
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
                    AR_Object placementObj = hitObject.transform.GetComponent<AR_Object>();
                    if (placementObj != null)
                    {
                        ChangeSelection(placementObj);
                        HandleSelectionEvent(placementObj);
                    }
                    else
                    {
                        PlaceObject();
                    }
                }
            }

            if (touch.phase == TouchPhase.Ended)
            {
                onTouchHold = false;
            }

        }
    }


    private void handleDragging()
    {
        // perform a new ray cast against where we have touched on the screen
        if (arOrigin.GetComponent<ARRaycastManager>().Raycast(touchPosition, hits, TrackableType.PlaneWithinPolygon))
        {
            // update our set selected object's position and rotation for the dragging
            selectedObject.transform.position = hits[0].pose.position;
            selectedObject.transform.rotation = hits[0].pose.rotation;
        }
    }

    private void ChangeSelection(AR_Object selected)
    {
        foreach (AR_Object obj in allObjects)
        {
            // change the color of the selected object
            MeshRenderer meshRenderer = obj.GetComponent<MeshRenderer>();
            // if we have found our selected obj
            if (selected == obj)
            {
                onTouchHold = true;
                selectedObject = obj;

                // set the UI text to this objects description
                descriptionText.text = obj.Description;
            }
            // handle color changing
            meshRenderer.material.color = (selected != obj) ? inActiveColor : activeColor;
        }
    }


    private void PlaceObject()
    {
        // do we want to limit the spawning to a single instance?
        if (isSingleSpawn)
        {
            // check to see if we have spawned our single mesh
            if (singleMesh == null)
            {
                singleMesh = Instantiate(objectToPlace, placementPose.position, placementPose.rotation);

                // make sure we still count this mesh, so we can select and delete it
                allObjects.Add(singleMesh);
            }
        }
        else
        {
            // check to see if we want to delete the previous with each spawn
            if (deletePrevious)
            {
                // make sure we have set an obj first
                if (allObjects.Count == 1)
                {
                    Destroy(allObjects[0].transform.gameObject);
                    allObjects.Clear();
                }
            }
            // store each obj we create into a list
            AR_Object aro = Instantiate(objectToPlace, placementPose.position, placementPose.rotation);

            // set the description of this object, with a default value once we run past our name list
            //aro.Description = (allObjects.Count <= objList.Length) ? objList[allObjects.Count] : "Default";
            allObjects.Add(aro);
        }

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
        hits = new List<ARRaycastHit>();
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


    public void HandleSelectionEvent(AR_Object selectedObj)
    {
        Debug.Log("HandleSelectionEvent called");
    }




}
