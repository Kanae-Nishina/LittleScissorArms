using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using WrappingRope.Scripts.Helpers;

namespace Assets.WrappingRope.Scripts.Utils
{
    public class Geometry
    {
        public static bool Raycast(Ray ray, out HitInfo hitInfo, float maxDistance, LayerMask layerMask)
        {
            hitInfo = new HitInfo();
            var raycastHit = new RaycastHit();
            if (Physics.Raycast(ray.origin, ray.direction, out raycastHit, maxDistance, layerMask))
            {
                var meshCollider = raycastHit.collider as MeshCollider;
                if (meshCollider != null && !meshCollider.convex)
                {
                    hitInfo = new HitInfo(raycastHit);
                    return true;
                }
                // Реализовать проверку на коллизию луча с мешем и если нет, то пошаговое движение вперед по лучу, чтобы проверить объекты за этим мешем
                return TryRaycast(ray, raycastHit.collider.gameObject, maxDistance, out hitInfo);
            }
            return false;
        }


        public static bool TryRaycast(Ray ray, GameObject gameObject, float maxDistance, out HitInfo hitInfo)
        {
            hitInfo = new HitInfo() { Rigidbody = gameObject.GetComponent<Rigidbody>(), GameObject = gameObject };

            // Если у объекта есть не выпуклый меш коллайдер, то используем "родной" Raycast 
            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider != null && !meshCollider.convex)
            {
                var raycastHit = new RaycastHit();
                if (meshCollider.Raycast(ray, out raycastHit, maxDistance))
                {
                    hitInfo = new HitInfo(raycastHit);
                    return true;
                }
                return false;
            }

            // Далее ищется коллизия с мешем
            var mesh = gameObject.GetComponent<MeshFilter>().sharedMesh;
            if (mesh == null)
            {
                throw new ApplicationException("No mesh in hited object");
            }
            var localOrigin = gameObject.transform.InverseTransformPoint(ray.origin);
            var localDest = gameObject.transform.InverseTransformPoint(ray.origin + ray.direction.normalized * maxDistance);
            var localDirection = localDest - localOrigin;
            var localDistance = localDirection.magnitude;
            var localRay = new Ray(localOrigin, localDirection);
            var plane1 = new Plane(localRay.origin, localRay.origin + localRay.direction, localRay.origin + GetThirdPoint(localRay.direction));
            var plane2 = new Plane(localRay.origin, localRay.origin + localRay.direction, localRay.origin + plane1.normal);
            float minLocalDistance = localDistance;
            var triangles = mesh.triangles;
            var vertices = mesh.vertices;
             //Debug.DrawLine(ray.origin, ray.origin + ray.direction.normalized * maxDistance, Color.red, 6f);
            for (int i = 0; i < triangles.Length; i += 3)
            {

                var v1 = vertices[triangles[i]];
                var v2 = vertices[triangles[i + 1]];
                var v3 = vertices[triangles[i + 2]];

                if ((plane1.SameSide(v1, v2) && plane1.SameSide(v2, v3))
                    || (plane2.SameSide(v1, v2) && plane2.SameSide(v2, v3)))
                    continue;

                var plane = new Plane(v1, v2, v3);

               // DebugDraw.DrawPlane(gameObject.transform.TransformPoint(), gameObject.transform.TransformPoint(plane1.normal))

                if (IsVisible(plane.normal, localDirection))
                {
                    float distance;
                    if (plane.Raycast(localRay, out distance))
                    {
                        var point = localRay.GetPoint(distance);

                        //Debug.DrawLine(gameObject.transform.TransformPoint(v1), gameObject.transform.TransformPoint(v2), Color.green, 6f);
                        //Debug.DrawLine(gameObject.transform.TransformPoint(v3), gameObject.transform.TransformPoint(v2), Color.green, 6f);
                        //Debug.DrawLine(gameObject.transform.TransformPoint(v1), gameObject.transform.TransformPoint(v3), Color.green, 6f);

                        if (IsPointInTriangle(point, v1, v2, v3))
                        {
                            if (distance > minLocalDistance)
                                continue;

                            minLocalDistance = distance;
                            hitInfo.Normal = plane.normal;
                            hitInfo.Point = point;
                            hitInfo.TriangleIndex = i / 3;
                        }
                    }
                }
            }
            ApplyTransform(ref hitInfo, gameObject.transform);
            return minLocalDistance != localDistance;
       }


        private static  void ApplyTransform(ref HitInfo hitInfo, Transform transform)
        {
            hitInfo.Normal = transform.TransformPoint(hitInfo.Normal);
            hitInfo.Point = transform.TransformPoint(hitInfo.Point);
        }

        private static bool IsVisible(Vector3 normal, Vector3 viewDir)
        {
            return (normal.x * viewDir.x + normal.y * viewDir.y + normal.z * viewDir.z) < 0;
        }


        private static Vector3 GetThirdPoint(Vector3 direction)
        {
            direction = new Vector3(Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z));
            //var components = new List<float>() { Mathf.Abs(direction.x), Mathf.Abs(direction.y), Mathf.Abs(direction.z) };
            var components = new List<float>() { direction.x, direction.y, direction.z};

            var minComp = components.Min();
                            
            var point = new Vector3();
            if (minComp == direction.y)
                return Vector3.up;
            if (minComp == direction.x )
                return Vector3.right;
            if (minComp == direction.z)
                return Vector3.forward;
            return point;
        }


        private static bool IsPointInTriangle(Vector3 point, Vector3 a, Vector3 b, Vector3 c)
        {
            var abcSqr = AreaOfTriangle(a, b, c);
            var abpSqr = AreaOfTriangle(a, b, point);
            var acpSqr = AreaOfTriangle(a, c, point);
            var bcpSqr = AreaOfTriangle(b, c, point);
            var sum = abpSqr + acpSqr + bcpSqr;
            return Mathf.Abs(abcSqr - sum) < 0.0005f;
        }


        public static float AreaOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            // Площадь треугольника
            float a = Vector3.Distance(p1, p2);
            float b = Vector3.Distance(p2, p3);
            float c = Vector3.Distance(p3, p1);
            float p = 0.5f * (a + b + c);
            float s = Mathf.Sqrt(p * (p - a) * (p - b) * (p - c));
            return s;
        }


        public static List<Vector3> CreatePolygon(int vertexCount, Axis normal, float radius, float initAngle)
        {
            var res = new List<Vector3>();
            for(var i = 0; i < vertexCount; i++)
            {
                var x = radius * Mathf.Cos((float)Math.PI * 360 * (1f - (float)i / (vertexCount)) / 180);
                var y = radius * Mathf.Sin((float)Math.PI * 360 * (1f - (float)i / (vertexCount)) / 180);
                switch(normal)
                {
                    case Axis.X:
                        res.Add(new Vector3(0, x, y));
                        break;
                    case Axis.Y:
                        res.Add(new Vector3(x, 0, y));
                        break;
                    case Axis.Z:
                        res.Add(new Vector3(x, y, 0));
                        break;

                }
            }
            return res;
        }


        public static List<Vector3> RotatePoly(List<Vector3> vertices, float angle, Vector3 axis)
        {
            var rotateResult = new List<Vector3>();
            for (var i = 0; i < vertices.Count; i++)
            {
                rotateResult.Add(Quaternion.AngleAxis(angle, axis) * vertices[i]);
            }
            return rotateResult;
        }


        public static List<Vector3> RotatePoly(List<Vector3> vertices, Quaternion rotation)
        {
            var rotateResult = new List<Vector3>();
            for (var i = 0; i < vertices.Count; i++)
            {
                rotateResult.Add(rotation * vertices[i]);
            }
            return rotateResult;
        }


        public static void RotatePoly(Vector3[] target, Vector3[] source, Quaternion rotation)
        {
            for (var i = 0; i < source.Length; i++)
            {
                target[i] = rotation * source[i];
            }
        }


        public static void RotatePoly(Vector3[] target, Vector3[] source, float angle, Vector3 axis)
        {
            for (var i = 0; i < source.Length; i++)
            {
                target[i] = Quaternion.AngleAxis(angle, axis) * source[i];
            }
        }


        public static List<Vector3> TranslatePoly(List<Vector3> vertices, Vector3 direction)
        {
            var translateResult = new List<Vector3>();
            for (var i = 0; i < vertices.Count; i++)
            {
                translateResult.Add(vertices[i] + direction);
            }
            return translateResult;
        }


        public static List<Vector3> ScalePoly(List<Vector3> vertices, float scale)
        {
            var result = new List<Vector3>();
            for (var i = 0; i < vertices.Count; i++)
            {
                result.Add(vertices[i] * scale);
            }
            return result;
        }


        public static void ScalePoly(Vector3[] target, Vector3[] source, float scale)
        {
            for (var i = 0; i < source.Length; i++)
            {
                target[i] = source[i] * scale;
            }
        }

        public static float Angle(Vector3 vec1, Vector3 vec2, Vector3 n)
        {
            ////Get the dot product
            //var dot = Vector3.Dot(vec1, vec2);
            //var sign = dot > 0;
            //// Divide the dot by the product of the magnitudes of the vectors
            //dot = dot / (vec1.magnitude * vec2.magnitude);
            ////Get the arc cosin of the angle, you now have your angle in radians 
            //var acos = Mathf.Acos(dot);
            ////Multiply by 180/Mathf.PI to convert to degrees
            //var angle = acos * 180 / Mathf.PI;
            ////if (sign)
            ////    angle = angle - 360;
            //return angle;


            // angle in [0,180]
            float angle = Vector3.Angle(vec1, vec2);
            float sign = Mathf.Sign(Vector3.Dot(n, Vector3.Cross(vec1, vec2)));

            // angle in [-179,180]

            float angle360;
            if (sign < 0)
                angle360 = 360 - angle;
            else
                angle360 = angle;
                // angle in [0,360] (not used but included here for completeness)
                //float angle360 =  (signed_angle + 180) % 360;
                return angle360;

        }
    }
}
