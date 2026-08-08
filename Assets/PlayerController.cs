using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    public float speed = 50;

    void FixedUpdate()
    {
        Rigidbody rightbody = GetComponent<Rigidbody>();

        if (TimeScript.instance.isTimeUp)
        {
            rightbody.velocity = Vector3.zero;
            rightbody.angularVelocity = Vector3.zero;
            return;
        }

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        rightbody.AddForce(x * speed,0,z * speed);
    }
    
}