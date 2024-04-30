using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable] public class HeroHorizontalMovementSettings {
    public float acceleration = 20f;
    public float deceleration = 15f;
    public float turnBackFriction = 25f;
    public float speedMax = 5f;
}
