using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackspaceScript : MonoBehaviour
{
    public NameScript nameScript;
    public string HandTag;

    public void ClickBackspace()
    {
        if (nameScript != null && nameScript.NameVar.Length > 0)
        {
            nameScript.NameVar = nameScript.NameVar.Remove(nameScript.NameVar.Length - 1);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == HandTag)
        {
            if (nameScript != null && nameScript.NameVar.Length > 0)
            {
                nameScript.NameVar = nameScript.NameVar.Remove(nameScript.NameVar.Length - 1);
            }
        }
    }
}