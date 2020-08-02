using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class camera : MonoBehaviour
{

    public Transform player;
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
        
    }
}
