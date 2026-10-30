using System;
using UnityEngine;

public class EnableNDisable : MonoBehaviour
{
    public bool IsDisable = false;
    public bool IsEnable = false;
    public GameObject GameObject;
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "HandTag" && !IsDisable)
        {
            GameObject.SetActive(true);
        } else if (other.tag == "HandTag" && IsDisable)
        {
            GameObject.SetActive(false);
        }
    }
}
