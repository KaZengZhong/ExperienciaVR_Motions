using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaintainAxis : MonoBehaviour
{
    public string axis;
    public float position;

    private void Update()
    {
        if (axis == "X")
        {
            transform.position = new Vector3(position, transform.position.y, transform.position.z);
        } 
        else if (axis == "Y")
        {
            transform.position = new Vector3(transform.position.x, position, transform.position.z);
        }
        else if (axis == "Z")
        {
            transform.position = new Vector3(transform.position.x, transform.position.y, position);
        }
    }
}
