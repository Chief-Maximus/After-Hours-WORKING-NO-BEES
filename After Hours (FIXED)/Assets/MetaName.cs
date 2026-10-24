// (C) Husk / Assetbundle

// ---------------------- LICENSED SCRIPT

// Copyright (c) Husk / Assetbundle. All rights reserved.
//
// PROPRIETARY LICENSE - USE ONLY WITH WRITTEN PERMISSION
//
// This script and all of its contents are the exclusive property of
// Husk / Assetbundle. No part of this script may be used, copied,
// modified, merged, published, distributed, sublicensed, sold, or
// otherwise made available to third parties without the prior
// explicit written permission of the copyright holder.
//
// Any use of this script without such permission is strictly prohibited
// and may result in legal action.
//
// Permission may be revoked at any time. Where permission has been
// granted, it applies only to the specific person and purpose named
// by the copyright holder and may not be transferred.
//
// THIS SCRIPT IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE COPYRIGHT HOLDER BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY ARISING FROM THE USE OF
// THIS SCRIPT.
//
// To request permission, contact: Husk / Assetbundle

// ----------------------

using Mirror;
using Mirror.VR;
using TMPro;
using UnityEngine;
using Oculus.Platform;
using Oculus.Platform.Models;

public class MetaName : NetworkBehaviour
{
    [Header("Main")]
    public TMP_Text NameText;

    [SyncVar(hook = nameof(OnNameChanged))]
    private string playerName = "";

    private void Awake()
    {
        if (NameText == null)
        {
            NameText = GetComponent<TMP_Text>();
        }
    }

    public override void OnStartClient()
    {
        ApplyName(playerName);
    }

    public override void OnStartLocalPlayer()
    {
        if (Core.IsInitialized())
        {
            CheckEntitlement();
        }
        else
        {
            Core.AsyncInitialize().OnComplete(OnInit);
        }
    }

    private void OnInit(Message<PlatformInitialize> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Init failed: " + msg.GetError().Message);
            return;
        }

        CheckEntitlement();
    }

    private void CheckEntitlement()
    {
        Entitlements.IsUserEntitledToApplication().OnComplete(OnEntChecked);
    }

    private void OnEntChecked(Message msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Entitlement failed: " + msg.GetError().Message);
            return;
        }

        Users.GetLoggedInUser().OnComplete(OnUserRec);
    }

    private void OnUserRec(Message<User> msg)
    {
        if (msg.IsError)
        {
            Debug.LogError("Login failed: " + msg.GetError().Message);
            return;
        }

        string value = msg.Data.DisplayName;

        if (string.IsNullOrEmpty(value))
        {
            value = msg.Data.OculusID;
        }

        if (string.IsNullOrEmpty(value))
        {
            Debug.LogError("Meta name is empty.");
            return;
        }

        if (MirrorVRPlayer.Player != null)
        {
            MirrorVRPlayer.Player.name = value;
        }

        ApplyName(value);
        CmdSetName(value);
    }

    [Command]
    private void CmdSetName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (value.Length > 32)
        {
            value = value.Substring(0, 32);
        }

        playerName = value;
    }

    private void OnNameChanged(string oldName, string newName)
    {
        ApplyName(newName);
    }

    private void ApplyName(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return;
        }

        if (NameText != null)
        {
            NameText.text = value;
        }
    }
}