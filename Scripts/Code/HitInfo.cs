using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.WrappingRope.Scripts
{
    public class HitInfo
    {
        public int TriangleIndex {get; set;}
        public Vector3 Normal { get; set; }
        public Vector3 Point { get; set; }
        public Rigidbody Rigidbody { get; set; }
        public GameObject GameObject { get; set; }
        

        public HitInfo()
        {

        }

        public HitInfo(RaycastHit raycastHit)
        {
            TriangleIndex = raycastHit.triangleIndex;
            Normal = raycastHit.normal;
            Point = raycastHit.point;
            Rigidbody = raycastHit.rigidbody;
            GameObject = raycastHit.collider.gameObject;
        } 
    }
}
