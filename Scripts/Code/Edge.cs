using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WrappingRope.Scripts
{
    public class Edge
    {
        public Vector3 Vertex1 { get; set; }
        public Vector3 Vertex2 { get; set; }

        public int TriangleIndex { get; set; }
    }
}
