using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UiNameScript : MonoBehaviour
{
    public NameButtonType TheButtonType;
    public NameScriptUI nameScript;
    public string Letter;

    public void OnEnter()
    {
        if (TheButtonType == NameButtonType.Letter)
        {
            nameScript.NameVar += Letter;
        }

        if (TheButtonType == NameButtonType.BackSpace)
        {
            nameScript.NameVar = nameScript.NameVar.Remove(nameScript.NameVar.Length - 1);
        }

        if (TheButtonType == NameButtonType.Enter)
        {
            nameScript.EnterName();

            if (nameScript.NameVar.Length == 0)
            {
                nameScript.NoName();
            }
        }
    }
}

public enum NameButtonType
{
    Letter,
    BackSpace,
    Enter
}