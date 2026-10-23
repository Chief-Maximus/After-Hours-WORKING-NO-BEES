using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public class MetaName : MonoBehaviour
{
    void Awake()
    {
        Core.AsyncInitialize().OnComplete(OnInit);
    }

    void OnInit(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Init failed, Pretty weird auth you´re trying to do?");
            return;
        }

        Entitlements.IsUserEntitledToApplication().OnComplete(OnEntChecked);
    }

    void OnEntChecked(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Entitlement failed.");
            return;
        }

        Users.GetLoggedInUser().OnComplete(OnUserRec);
    }

    void OnUserRec(Message<User> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Login failed.");
            return;
        }

        User user = msg.Data;
        Debug.Log($"DN: {user.DisplayName}");
        Debug.Log($"Something else i think looks cool: {user.OculusID}");
    }
}
