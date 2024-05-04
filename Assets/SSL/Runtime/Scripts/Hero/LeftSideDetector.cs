using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LeftSideDetector : MonoBehaviour {
    [Header("Detection")]
    [SerializeField] private Transform[] _detectionPoints;
    [SerializeField] private float _detectionLength = 0.1f;
    [SerializeField] private LayerMask _LeftSideLayerMask;

    public bool DetectLeftSideNearBy() {
        foreach (Transform detectionPoint in _detectionPoints) {
            RaycastHit2D hitResult = Physics2D.Raycast(
                detectionPoint.position,
                Vector2.left,
                _detectionLength,
                _LeftSideLayerMask
            );

            if (hitResult.collider != null) {
                return true;
            }
        }

        return false;
    }
}