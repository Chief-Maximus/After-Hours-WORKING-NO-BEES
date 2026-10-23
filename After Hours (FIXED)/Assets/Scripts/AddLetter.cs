using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AddLetter : MonoBehaviour
{
    public NameScript nameScript;
    public string Handtag;
    public string Letter;

    public void ClickLetter()
    {
        if (nameScript != null)
        {
            nameScript.NameVar += Letter;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.tag == Handtag)
        {
            nameScript.NameVar += Letter;
        }
    }
}