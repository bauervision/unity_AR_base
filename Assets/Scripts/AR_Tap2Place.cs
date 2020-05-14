using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.XR.ARFoundation;
using UnityEngine.UI;
using UnityEngine.XR.ARSubsystems;

public class AR_Tap2Place : MonoBehaviour
{
    // =====================================
    #region GeneralSettings


    [Header("General Spawning Objects")]
    [SerializeField]
    [Tooltip("Shown when a surface has been detected as the orgin for spawning")]
    private GameObject placementIndicator;

    [SerializeField]
    [Tooltip("List of Spawnable Objects")]
    private List<AR_Object> SpawnList;


    [SerializeField]
    [Tooltip("Material to apply to ghost mesh prior to spawning")]
    private Material GhostMaterial;


    [SerializeField]
    [Tooltip("Camera to use for ray casting")]
    private Camera AR_Camera = default;

    [SerializeField]
    [Tooltip("Do we limit spawning to a single mesh?")]
    private bool isSingleSpawn = false;

    [SerializeField]
    [Tooltip("Do we destroy the previous mesh with each spawn?")]
    private bool deletePrevious = false;

    #endregion
    // =====================================
    #region UIfields
    [Header("UI Fields")]
    [SerializeField]
    [Tooltip("UI Element to target for updates")]
    public Text countText;

    [SerializeField]
    [Tooltip("UI Element to target for object ID display")]
    public Text idText;
    [SerializeField]
    [Tooltip("UI Element to target for object NAME display")]
    public Text nameText;
    [SerializeField]
    [Tooltip("UI Element to target for object AGE display")]
    public Text ageText;

    [SerializeField]
    [Tooltip("UI Element to target for toggling spawning options")]
    public GameObject spawningOptions;

    [SerializeField]
    [Tooltip("UI Element to target for toggling ui options")]
    public GameObject UI_Options;

    #endregion
    // =====================================
    #region SelectionSpecifics
    [Header("Selection Settings")]
    [SerializeField]
    [Tooltip("Active Color for Selected Objects")]
    private Color activeColor = Color.blue;

    [SerializeField]
    [Tooltip("InActive Color for De-Selected Objects")]
    private Color inActiveColor = Color.red;

    #endregion
    /*  =============== JSON related =====================  */
    #region JSONrelated
    [System.Serializable]
    private class JsonDataRaw
    {
        public List<ObjectList> objects;
    }


    [System.Serializable]
    private class ObjectList
    {
        public int id;
        public string name;
        public int age;
    }

    private JsonDataRaw jData;
    #endregion

    /*  ================== Private Members ======================  */
    #region PrivateMembers

    private AR_Object dynamicObjectToPlace;
    List<AR_Object> allObjects = new List<AR_Object>();

    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private ARSessionOrigin arOrigin;
    private Pose placementPose;
    private bool validPlacementPose = false;
    private Vector2 touchPosition = default;

    private AR_Object singleMesh;

    private AR_Object selectedObject;

    private bool onTouchHold = false;
    private List<ObjectList> objList;

    public GameObject holdText;
    private AR_Object ghost;
    private bool showGhost = false;

    private bool isUIBlocking = false;

    #endregion


    // Start is called before the first frame update
    void Start()
    {
        arOrigin = FindObjectOfType<ARSessionOrigin>();
        // handle all UI initializations
        InitializeUI();
        // setup fetch
        StartCoroutine(GetRequest("https://my-json-server.typicode.com/bauervision/unity_AR_base/data"));

        // start off by assigning the first mesh in the spawn list to objectToPlace
        AssignMesh(0);

        // testing
        // holdText = GameObject.Find("Hold");
        // holdText.GetComponent<Text>().text = jData.objectList[0].name;
    }

    private void InitializeUI()
    {
        // start clean with everything hidden
        countText.text = "";
        spawningOptions.SetActive(false);
        UI_Options.SetActive(false);

    }
    IEnumerator GetRequest(string url)
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            // Request and wait for the desired page.
            yield return webRequest.SendWebRequest();

            string[] pages = url.Split('/');
            int page = pages.Length - 1;

            if (webRequest.isNetworkError)
            {
                Debug.Log(pages[page] + ": Error: " + webRequest.error);
            }
            else
            {
                var data = webRequest.downloadHandler.text;
                jData = JsonUtility.FromJson<JsonDataRaw>(data);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();

        // make sure we don't have any UI elements visible
        if (!isUIBlocking)
        {
            HandleSelectionDetection();
        }


        // keep our counter updated with list total once we have something to count
        if (allObjects.Count != 0)
        {
            countText.text = "Number of Objects in scene: " + allObjects.Count;

            idText.text = selectedObject.id.ToString();
            nameText.text = selectedObject.arName;
            ageText.text = selectedObject.age.ToString();
        }

        if (onTouchHold)
        {
            handleDragging();
        }

        // testing
        holdText.SetActive(onTouchHold);
    }

    // all of these methods will be called from buttons on the UI
    #region UI_methods

    public void ClearScene()
    {
        foreach (AR_Object obj in allObjects)
        {
            Destroy(obj.transform.gameObject);
        }
        allObjects.Clear();

    }

    public void ToggleSpawningUI()
    {
        spawningOptions.SetActive(!spawningOptions.activeInHierarchy);
        isUIBlocking = spawningOptions.activeInHierarchy;
        // if the other UI window was opened, close it
        UI_Options.SetActive(UI_Options.activeInHierarchy && false);
    }

    public void ToggleOptionsUI()
    {
        UI_Options.SetActive(!UI_Options.activeInHierarchy);
        isUIBlocking = UI_Options.activeInHierarchy;
        // if the other UI window was opened, close it
        spawningOptions.SetActive(spawningOptions.activeInHierarchy && false);

    }


    public void ToggleShowGhost(bool isChecked)
    {
        placementIndicator.transform.GetChild(0).gameObject.SetActive(isChecked);
    }

    public void AssignMesh(int inputValue)
    {
        // assign desired to mesh to both the ghost and what will be spawned
        dynamicObjectToPlace = SpawnList[inputValue];
        //grab the actual mesh of what will be spawned
        Mesh newMesh = dynamicObjectToPlace.GetComponent<MeshFilter>().mesh;
        // and update the ghost
        placementIndicator.transform.GetChild(0).GetComponent<MeshFilter>().mesh = newMesh;
        Debug.Log("Ghost = " + placementIndicator.transform.GetChild(0).name);

    }
    #endregion


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
                    AR_Object selectedObj = hitObject.transform.GetComponent<AR_Object>();
                    if (selectedObj != null)
                    {
                        ChangeSelection(selectedObj);
                        HandleSelectionEvent(selectedObj);
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
                singleMesh = Instantiate(dynamicObjectToPlace, placementPose.position, placementPose.rotation);
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
            AR_Object aro = Instantiate(dynamicObjectToPlace, placementPose.position, placementPose.rotation);

            // set the data for this object from what we got from the url fetch
            aro.arName = (allObjects.Count <= jData.objects.Count) ? jData.objects[allObjects.Count].name : "Default";
            aro.id = (allObjects.Count <= jData.objects.Count) ? jData.objects[allObjects.Count].id : 0;
            aro.age = (allObjects.Count <= jData.objects.Count) ? jData.objects[allObjects.Count].age : 0;
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
