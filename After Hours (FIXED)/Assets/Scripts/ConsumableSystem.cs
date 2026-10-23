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

using System;
using GorillaLocomotion;
using UnityEngine;

public class ConsumableSystem : MonoBehaviour
{
    [Header("!SCRIPT MADE BY HUSK!")] 
    [Header("License is in the script content.")]
    [Header("Authorized Usage Only!")]
    
    [Header("Change the Draining speed process")]
    public float WaterDrainSpeed = 2f;
    public float FoodDrainSpeed = 2f;
    
    [Header("Current Amount of Food & Water")]
    [Range(0, 100)]
    public int FoodAmount = 100;
    [Range(0, 100)]
    public int WaterAmount = 100;
    
    [Header("Additional Settings")]
    [SerializeField] private bool MovementAffectsFood = false;
    [SerializeField] private bool MovementAffectsWater = false;
    [SerializeField] private GorillaLocomotion.Player Player;
    [SerializeField] private GameObject[] Controllers;
    [SerializeField] private float movementDrainFactor = 0.5f;
    
    private float foodExact;
    private float waterExact;
    private Vector3 lastPlayerPos;

    private void Start()
    {
        foodExact = FoodAmount;
        waterExact = WaterAmount;
        if (Player != null) lastPlayerPos = Player.transform.position;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        float speed = 0f;
        if (Player != null)
        {
            speed = (Player.transform.position - lastPlayerPos).magnitude / dt;
            lastPlayerPos = Player.transform.position;
        }
        float foodMulti = 1f + (MovementAffectsFood ? speed * movementDrainFactor : 0f);
        float waterMulti = 1f + (MovementAffectsWater ? speed * movementDrainFactor : 0f);
        foodExact -= FoodDrainSpeed * foodMulti * dt;
        waterExact -= WaterDrainSpeed * waterMulti * dt;
        FoodAmount = Mathf.CeilToInt(foodExact);
        WaterAmount = Mathf.CeilToInt(waterExact);
    }
    
    public void Eat(float amount)   { foodExact  = Mathf.Min(foodExact  + amount, 100f); }
    public void Drink(float amount) { waterExact = Mathf.Min(waterExact + amount, 100f); }
}
