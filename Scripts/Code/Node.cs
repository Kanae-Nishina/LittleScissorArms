using UnityEngine;
namespace Assets.WrappingRope.Scripts
{
    public struct Node
    {
        public Vector3 Vertex { get; set; }
        public Vector2 Uv { get; set; }

        private int _normalCount;

        public Vector3[] Normals { get; set; }

        public Node(int normalsCount) : this()
        {
            Normals = new Vector3[normalsCount];
        }

        private Vector3 _averageNormal;


        public void AddNormal(Vector3 normal)
        {
            Normals[_normalCount] = normal;
            _normalCount++;
        }

        public Vector3 GetAverageNormal()
        {
            if (_averageNormal == Vector3.zero)
            {
                var res = Normals[0];
                //Debug.DrawRay(Vertex, Normals[0]);
                for (var i = 1; i < _normalCount; i++)
                {
                    //Debug.DrawRay(Vertex, Normals[i]);
                    var normal = Normals[i];
                    if (normal == Vector3.zero)
                    {
                        continue;
                    }
                    res = Vector3.Slerp(res, Normals[i], 0.5f);
                }
                _averageNormal = res;
            }
            return _averageNormal;
        }

        public void ResetNormals()
        {
            _normalCount = 0;
            _averageNormal = Vector3.zero;
        }

    }
}
