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

using UnityEngine;
using Mirror;

public class Whistle : NetworkBehaviour
{
    [Header("Main")]
    public AudioSource Player;
    public AudioClip[] Clips;
    public GameObject[] Controllers;

    [Header("Radius")]
    public float Range = 10f;

    [Header("Cooldown Settings")]
    public float Cooldown = 2f;

    [Header("Net Settings")]
    public bool UseNet;

    [Header("Gizmo Settings")]
    public Color GizmoColor = new Color(1f, 0.85f, 0.1f, 1f);
    public bool AlwaysShowGizmo;

    private int lastIndex = -1;
    private float nextPlayTime;
    private float nextServerTime;
    private bool wasInside;

    public bool CanPlay
    {
        get { return Time.time >= nextPlayTime; }
    }

    public float CooldownLeft
    {
        get { return Mathf.Max(0f, nextPlayTime - Time.time); }
    }

    private void Update()
    {
        bool inside = AllInRange();

        if (inside && !wasInside)
        {
            Play();
        }

        wasInside = inside;
    }

    public bool AllInRange()
    {
        if (Controllers == null || Controllers.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < Controllers.Length; i++)
        {
            if (Controllers[i] == null)
            {
                return false;
            }

            if (!InRange(Controllers[i].transform.position))
            {
                return false;
            }
        }

        return true;
    }

    public void Play()
    {
        if (!CanPlay)
        {
            return;
        }

        if (Clips == null || Clips.Length == 0)
        {
            return;
        }

        int index = Random.Range(0, Clips.Length);

        if (Clips.Length > 1)
        {
            while (index == lastIndex)
            {
                index = Random.Range(0, Clips.Length);
            }
        }

        nextPlayTime = Time.time + Cooldown;

        if (UseNet && NetworkClient.active)
        {
            CmdPlay(index);
        }
        else
        {
            PlayLocal(index);
        }
    }

    [Command(requiresAuthority = false)]
    private void CmdPlay(int index)
    {
        if (Time.time < nextServerTime)
        {
            return;
        }

        nextServerTime = Time.time + Cooldown;
        RpcPlay(index);
    }

    [ClientRpc]
    private void RpcPlay(int index)
    {
        PlayLocal(index);
    }

    private void PlayLocal(int index)
    {
        if (Player == null || Clips == null || index < 0 || index >= Clips.Length)
        {
            return;
        }

        lastIndex = index;

        if (Clips[index] == null)
        {
            return;
        }

        Player.PlayOneShot(Clips[index]);
        Notify();
    }

    public bool InRange(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= Range * Range;
    }

    private void Notify()
    {
        if (Controllers == null)
        {
            return;
        }

        for (int i = 0; i < Controllers.Length; i++)
        {
            GameObject controller = Controllers[i];

            if (controller == null)
            {
                continue;
            }

            if (InRange(controller.transform.position))
            {
                controller.SendMessage("OnWhistle", transform.position, SendMessageOptions.DontRequireReceiver);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (AlwaysShowGizmo)
        {
            DrawRange();
        }
    }

    private void OnDrawGizmosSelected()
    {
        DrawRange();
    }

    private void DrawRange()
    {
        Color fill = GizmoColor;
        fill.a = 0.12f;

        Gizmos.color = fill;
        Gizmos.DrawSphere(transform.position, Range);

        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, Range);

        if (Controllers == null)
        {
            return;
        }

        for (int i = 0; i < Controllers.Length; i++)
        {
            if (Controllers[i] == null)
            {
                continue;
            }

            Vector3 target = Controllers[i].transform.position;
            Gizmos.color = InRange(target) ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, target);
        }
    }
}