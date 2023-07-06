using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Rotate : MonoBehaviour
{
    [SerializeField] GameObject bottle;
    void Start()
    {
        LeanTween.rotateAround(bottle, Vector3.down, 360, 2.5f).setLoopClamp();
    }
}
