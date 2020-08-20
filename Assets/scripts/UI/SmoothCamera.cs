using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCamera : MonoBehaviour
{

    /*public Transform player;
    public float cameraDistance=30.0f;
    public void Awake(){
        GetComponent<UnityEngine.Camera>().orthographicSize=((Screen.height/2)/cameraDistance );
    }
    public void FixedUpdate(){
        transform.position= new Vector3(player.position.x,player.position.y,player.position.z);
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }*/

    //Values that need to be change according  to mas values of map
    public Transform target;
    public Vector3 offset;

    void LateUpdate()
    {
        transform.position = new Vector3(target.position.x, target.position.y, transform.position.z);
    }
}
