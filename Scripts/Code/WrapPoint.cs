using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WrappingRope.Scripts
{
    public class WrapPoint
    {
        public Vector3 Origin { get; set; }
        public Vector3 Normal1 { get; set; }
        public Vector3 Normal2 { get; set; }
        public Vector3 EdgePoint1 { get; set; }
        public Vector3 EdgePoint2 { get; set; }

        public Vector3 Lever
        {
            get
            {
                return Vector3.Lerp(Normal1, Normal2, 0.5f);
            }
        }
    }
}
