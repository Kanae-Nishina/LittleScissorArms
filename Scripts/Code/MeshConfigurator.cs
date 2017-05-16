using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.WrappingRope.Scripts
{
    [Serializable]
    public class MeshConfigurator : object
    {
        [Range(0, 4)]
        public int BendCrossectionsNumber = 0;
        public int VertexNumber = 3;
        public bool FlipNormals = false;
        [SerializeField]
        public List<Vector3> Profile = new List<Vector3>(3);

    }
}
