using System.Collections;
using System.Collections.Generic;
//using System.Threading.Tasks.Dataflow;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }
    // Update is called once per frame
    void Update()
    {
        if (!TimeScript.instance.isTimeUp)
        {
            float moveX = Input.GetAxis("Horizontal") * Time.deltaTime * speed;
            float moveZ = Input.GetAxis("Vertical") * Time.deltaTime * speed;
            transform.position = new Vector3
            (
                transform.position.x + moveX,
                transform.position.y,
                transform.position.z + moveZ
            );
        }
    }
    public float speed = 50;

    void FixedUpdate()
    {
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Rigidbody rightbody = GetComponent<Rigidbody>();
        rightbody.AddForce(x * speed,0,z * speed);
    }
}
