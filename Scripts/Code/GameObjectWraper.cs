#define DEBUG
#undef DEBUG

using Assets.WrappingRope.Scripts;
using Assets.WrappingRope.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WrappingRope.Scripts.Helpers;

namespace WrappingRope.Scripts
{
    public class GameObjectWraper
    {
        private GameObject _gameObject;
        private Piece _piece;

        private PieceInfo _pieceInfo;
        private HitInfo _hitInfo;

        private int[] _triangles;

        private Vector3[] _vertices;

        private float _frontToHitToBackRelation;

        private float _sqrTreshold;
        private Vector3[] _localPiecePoints = new Vector3[4];
        private Plane _crossPlane;
        

        public GameObjectWraper(PieceInfo pieceInfo, HitInfo hitInfo)
        {
            _gameObject = hitInfo.GameObject;
            _piece = pieceInfo.Piece;
            _pieceInfo = pieceInfo;
            _hitInfo = hitInfo;
            var pieceStageDistance = (_pieceInfo.FrontBandPoint - _pieceInfo.BackBandPoint).magnitude;
            var frontToHitDistance = (_pieceInfo.FrontBandPoint - _hitInfo.Point).magnitude;
            _frontToHitToBackRelation = frontToHitDistance / pieceStageDistance;
            _sqrTreshold = _piece.Threshold * _piece.Threshold;
            var mesh = _gameObject.GetComponent<MeshFilter>().sharedMesh;
            _triangles = mesh.triangles;
            _vertices = mesh.vertices;
        }


        public List<Vector3> GetWrapPoints()
        {
            var wrapPoints = new List<Vector3>();
            List<WrapPoint> crossPoints = new List<WrapPoint>();
            List<WrapPoint> pathPoints = new List<WrapPoint>();
            Piece piece = _piece;
            try
            {
                // Преобразуем координаты концов куска в локальные.
                var localPiecePoints = GetLocalPiecePoints(_pieceInfo);
                bool cantDefineWrapSide;
                var piecePlane = GetPiecePlane(out cantDefineWrapSide, localPiecePoints);
                if (cantDefineWrapSide)
                    return wrapPoints;
                var crossPlane = GetCrossPlane(piecePlane, localPiecePoints);
#if DEBUG
                 DrawBasePlanes(_hitInfo.Point, piecePlane.normal, crossPlane.normal);
#endif
                var pathFinder = new WrapPathFinder(_gameObject, _pieceInfo, _hitInfo, localPiecePoints, piecePlane, crossPlane, _triangles, _vertices, _sqrTreshold);
                pathPoints = pathFinder.GetWrapPath();
                if (pathPoints.Count == 0) return wrapPoints;

                //// Получившиеся после оборачивания куски могут врезаться в объект, поэтому выполняем еще оборачивание самым первым и самым последним куском из получившихся при оборачивании
                //var frontPieceBackBandPoint = GetPointInWorldSpace(crossPlane.normal, pathPoints[0]);
                //var backPieceFrontBandPoint = GetPointInWorldSpace(crossPlane.normal, pathPoints[pathPoints.Count - 1]);

                //var frontPiece = new PieceInfo() { FrontBandPoint = _piece.FrontBandPoint.transform.position, BackBandPoint = frontPieceBackBandPoint };
                //var backPiece = new PieceInfo() { FrontBandPoint = backPieceFrontBandPoint, BackBandPoint = _piece.BackBandPoint.transform.position};

                //var frontThirdPoint = _pieceInfo.FrontBandPoint;
                //var backThirdPoint = _pieceInfo.BackBandPoint;

                //var extFrontPathPoints = ResolveNewPieceCollisionAndGetAdditionPathPoints(frontPiece, frontThirdPoint);
                //var extBackPathPoints = ResolveNewPieceCollisionAndGetAdditionPathPoints(backPiece, backThirdPoint);
                //pathPoints.InsertRange(0, extFrontPathPoints);
                //pathPoints.InsertRange(pathPoints.Count, extBackPathPoints);

                wrapPoints.AddRange(GetWrapPointsInWorldCoordinates(pathPoints, crossPlane.normal));
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }

            return wrapPoints;
        }


        private List<Vector3> GetWrapPointsInWorldCoordinates(List<WrapPoint> pathPoints, Vector3 localShift)
        {
            var resultPoints = new List<Vector3>();
            foreach (var pathPoint in pathPoints)
            {
                Vector3 resultPoint = GetPointInWorldSpace(localShift, pathPoint);
                resultPoints.Add(resultPoint);
            }
            return resultPoints;
        }

        private Vector3 GetPointInWorldSpace(Vector3 localShift, WrapPoint pathPoint)
        {
            float wrapDistance = _piece.WrapDistance < 0.001 ? 0.001f : _piece.WrapDistance;
            var shift = (_gameObject.transform.rotation * localShift).normalized * wrapDistance;
            var resultPoint = _gameObject.transform.TransformPoint(pathPoint.Origin) + shift;
            return resultPoint;
        }


        private List<WrapPoint> ResolveNewPieceCollisionAndGetAdditionPathPoints(PieceInfo pieceInfo, Vector3 thirdPoint)
        {
            Ray ray = new Ray(pieceInfo.FrontBandPoint, pieceInfo.BackBandPoint - pieceInfo.FrontBandPoint);
            HitInfo hitInfo;
            if (!Geometry.TryRaycast(ray, _gameObject, (pieceInfo.BackBandPoint - pieceInfo.FrontBandPoint).magnitude, out hitInfo))
                return new List<WrapPoint>();

            var localPiecePoints = GetLocalPiecePoints(pieceInfo);
            var piecePlane = new Plane(localPiecePoints[1], localPiecePoints[3], thirdPoint);
            var crossPlane = GetCrossPlane(piecePlane, localPiecePoints);
            var pathFinder = new WrapPathFinder(_gameObject, pieceInfo, hitInfo, localPiecePoints, piecePlane, crossPlane, _triangles, _vertices, _sqrTreshold);
            return pathFinder.GetWrapPath();
        }


        private void DrawBasePlanes(Vector2 origin, Vector3 piecePlaneNormal, Vector3 crossPlaneNormal )
        {
            DebugDraw.DrawPlane(origin, piecePlaneNormal, Color.green, Color.red);
            DebugDraw.DrawPlane(origin, crossPlaneNormal, Color.magenta, Color.yellow);
        }


        private Plane GetPiecePlane(out bool cantDefineWrapSide, Vector3[] localPiecePoints)
        {
            Vector3 thirdPoint = new Vector3();
            cantDefineWrapSide = false;
            if (!GetThirdPointByRigidbody(ref thirdPoint) && !GetThirdPointByCollider(ref thirdPoint))
            {
                cantDefineWrapSide = true;
                return new Plane();
            }
            thirdPoint = _gameObject.transform.InverseTransformPoint(thirdPoint);
            return new Plane(localPiecePoints[1], localPiecePoints[3], thirdPoint);
        }


        private Vector3[] GetLocalPiecePoints(PieceInfo pieceInfo)
        {
            var p1 = _gameObject.transform.InverseTransformPoint(pieceInfo.PrevFrontBandPoint);
            var p2 = _gameObject.transform.InverseTransformPoint(pieceInfo.BackBandPoint);
            var p3 = _gameObject.transform.InverseTransformPoint(pieceInfo.PrevBackBandPoint);
            // Еще одна точка для других задач
            var p4 = _gameObject.transform.InverseTransformPoint(pieceInfo.FrontBandPoint);
            return new[] { p1, p2, p3, p4 };
        }


        private Vector3 GetPieceVelocityInHitPoint()
        {
            var frontBandPointVelocity = _piece.DefineFrontBandPointVelocity();
            var backBandPointVelocity = _piece.DefineBackBandPointVelocity();
            Vector3 mergeVelocity = Vector3.zero;
            if (_piece.LastWrapPoint != null)
            {
                mergeVelocity = (_piece.FrontBandPoint.transform.position - _piece.LastWrapPoint.Origin) / Time.fixedDeltaTime;
            }
            return Vector3.Lerp(frontBandPointVelocity + mergeVelocity, backBandPointVelocity, _frontToHitToBackRelation);
        }


        private bool GetThirdPointByRigidbody(ref Vector3 thirdPoint)
        {
            var rigidbody = _hitInfo.Rigidbody;
            if (rigidbody == null) return false;
            var velocity = rigidbody.GetPointVelocity(_hitInfo.Point);
#if DEBUG
                Debug.DrawRay(_hitInfo.Point, velocity, Color.red, 5f);
#endif
            var pieceVelocityInHitPoint = GetPieceVelocityInHitPoint();
#if DEBUG

                Debug.DrawRay(_hitInfo.Point, -pieceVelocityInHitPoint, Color.green, 5f);
#endif
            var hitVector = velocity - pieceVelocityInHitPoint;
            if (hitVector.Equals(Vector3.zero)) return false;
            thirdPoint = _hitInfo.Point + hitVector;
            return true;
        }


        private bool GetThirdPointByCollider(ref Vector3 thirdPoint)
        {
            var hitVector = -GetPieceVelocityInHitPoint();
            if (hitVector.Equals(Vector3.zero)) return false;
            thirdPoint = _hitInfo.Point + hitVector;
            return true;
        }



        private Plane GetCrossPlane(Plane sourcePlane, Vector3[] localPiecePoints)
        {
            // Получаем точку над концом куска, так, чтобы отрезок "конец куска - точка" был перпендикулярен плоскости
            return GetCrossPlane(sourcePlane, localPiecePoints[1], localPiecePoints[3]);
        }


        private Plane GetCrossPlane(Plane sourcePlane, Vector3 p1, Vector3 p2)
        {
            var p3 = sourcePlane.normal + p1;
            return new Plane(p2, p1, p3);
        }


        public class WrapPathFinder
        {

            private GameObject _gameObject;
            private PieceInfo _pieceInfo;
            private HitInfo _hitInfo;
            private Vector3[] _localPiecePoints = new Vector3[4];
            private Plane _piecePlane;
            private Plane _crossPlane;
            private int[] _triangles;
            private Vector3[] _vertices;
            private float _sqrTreshold;
            private List<Edge[]> _edgeCache = new List<Edge[]>();

            public WrapPathFinder(
                GameObject gameObject,
                PieceInfo pieceInfo,
                HitInfo hitInfo,
                Vector3[] localPiecePoints,
                Plane piecePlane,
                Plane crossPlane,
                int[] triangles,
                Vector3[] vertices,
                float sqrTreshold
                )
            {
                _gameObject = gameObject;
                _pieceInfo = pieceInfo;
                _hitInfo = hitInfo;
                _localPiecePoints = localPiecePoints;
                _piecePlane = piecePlane;
                _crossPlane = crossPlane;
                _triangles = triangles;
                _vertices = vertices;
                _sqrTreshold = sqrTreshold;
            }

            public List<WrapPoint> GetWrapPath()
            {
                List<WrapPoint> crossPoints;
                TryFindCrossPointsInMeshCoords(out crossPoints, false);
                var pathPoints = GetWrapPathFromCrossPoints(crossPoints);
                return pathPoints;
            }

            private int FindEdgeGroupIndexByTriangleIndex(int triangleindex)
            {
                var edgeGroup = _edgeCache.Find(eg => eg[0].TriangleIndex == triangleindex);
                if (edgeGroup != null)
                    return _edgeCache.IndexOf(edgeGroup);
                return -1;
            }



            private List<WrapPoint> FindCrossPointsInMeshCoordsForHill(int startEdgeGroupIndex, out bool isCrosspointOutOfTreshold, bool checkTreshold = true)
            {
                var crossPoints = new List<WrapPoint>();
                if (startEdgeGroupIndex + 1 > _edgeCache.Count)
                {
#if DEBUG
                Debug.Log("GameObjectWrapper.FindCrossPointsInMeshCoordsForHill: Start edge group index is out of edge cache range.");
#endif
                    isCrosspointOutOfTreshold = false;
                    return crossPoints;
                }
                var startEdges = _edgeCache[startEdgeGroupIndex];
                var startEdge = startEdges[0];
                if (startEdge != null && !IsCrossPointInWorkSpace(startEdge.Vertex1, startEdge.Vertex2))
                    startEdge = startEdges[1];
                FindCrossPointsForEdgesRecursively(startEdge, ref crossPoints, out isCrosspointOutOfTreshold, checkTreshold);
                return crossPoints;
            }


            private bool CheckRaycastHit(Vector3 startPoint, Vector3 stopPoint, out int triangleIndex, out Vector3 hitPoint)
            {
                //RaycastHit hit = new RaycastHit();
                var direction = stopPoint - startPoint;
                Ray ray = new Ray() { origin = startPoint + direction.normalized * 0.01f, direction = direction };
                var distance = direction.magnitude;
                HitInfo hitInfo;
                //var res = Geometry.Raycast(ray, out hitInfo, distance);
                var res = Geometry.TryRaycast(ray, _gameObject, distance, out hitInfo);

                triangleIndex = hitInfo.TriangleIndex;
                hitPoint = hitInfo.Point;
                return res;
            }


            private bool TryFindCrossPointsInMeshCoords(out List<WrapPoint> crossPoints, bool checkTreshold = true)
            {
                int startEdgeGroupIndex = 0;
                crossPoints = new List<WrapPoint>();
                FillEdgesCache(_hitInfo.TriangleIndex, out startEdgeGroupIndex);
                if (_edgeCache.Count == 0)
                {
#if DEBUG
                var name = _gameObject.name;
                Debug.LogWarning(string.Format("Count of wrapped edges of gameObject's ({0}) mesh is zero, but collision of rope and this gameObject was obtained. Most likely wrapping path is wrong because of rounding.", name));
#endif
                }
                bool isCrosspointOutOfTreshold = false;
                var startPoint = _hitInfo.Point;
                do
                {
                    crossPoints.AddRange(FindCrossPointsInMeshCoordsForHill(startEdgeGroupIndex, out isCrosspointOutOfTreshold, checkTreshold));
                    if (isCrosspointOutOfTreshold)
                    {
                        crossPoints.Clear();
#if DEBUG
                    Debug.Log("CrosspointOutOfTreshold");
#endif
                        return false;
                    }
                    int triangleIndex;
                    if (CheckRaycastHit(startPoint, _pieceInfo.BackBandPoint, out triangleIndex, out startPoint))
                    {
                        startEdgeGroupIndex = FindEdgeGroupIndexByTriangleIndex(triangleIndex);
                    }
                    else { break; }
                } while (startEdgeGroupIndex > -1);
                return true;
            }


            private void FindCrossPointsForEdgesRecursively(Edge edge, ref List<WrapPoint> crossPoints, out bool isCrosspointOutOfTreshold, bool checkTreshold = true)
            {
                Edge nextEdge;
                isCrosspointOutOfTreshold = false;
                if (edge == null) return;
                if (!AddEdgeCrossPoint(edge.Vertex1, edge.Vertex2, ref crossPoints, out isCrosspointOutOfTreshold, checkTreshold))
                {
                    return;
                }
                nextEdge = FindNextEdgeInNaibourEdgeGroup(edge);
                if (nextEdge == null || checkTreshold && isCrosspointOutOfTreshold) return;
                if (crossPoints.Count > 20)
                {
                    isCrosspointOutOfTreshold = true;
                    crossPoints.Clear();
#if DEBUG
                Debug.LogWarning("Too many wrap points in one wrap (over 20)! Possible stack overflow exception!");
#endif
                    return;
                }
                FindCrossPointsForEdgesRecursively(nextEdge, ref crossPoints, out isCrosspointOutOfTreshold, checkTreshold);
            }


            private void FillEdgesCache(int triangleIndex, out int startEdgeGroupIndex)
            {
                _edgeCache.Clear();
                startEdgeGroupIndex = 0;
                var vertexTriangleIndex = triangleIndex * 3;
                for (int i = 0; i < _triangles.Length; i += 3)
                {
                    var v1 = _vertices[_triangles[i]];
                    var v2 = _vertices[_triangles[i + 1]];
                    var v3 = _vertices[_triangles[i + 2]];

                    if (!_crossPlane.GetSide(v1) && !_crossPlane.GetSide(v2) && !_crossPlane.GetSide(v3))
                        continue;

                    var edges = new Edge[2] { null, null };
                    var pos = 0;
                    if (!_piecePlane.SameSide(v1, v2))
                    {
                        edges[pos] = new Edge() { Vertex1 = v1, Vertex2 = v2, TriangleIndex = i / 3 };
                        pos++;
                    }

                    if (!_piecePlane.SameSide(v2, v3))
                    {
                        edges[pos] = new Edge() { Vertex1 = v2, Vertex2 = v3, TriangleIndex = i / 3 };
                        pos++;
                    }

                    if (!_piecePlane.SameSide(v3, v1))
                    {
                        edges[pos] = new Edge() { Vertex1 = v3, Vertex2 = v1, TriangleIndex = i / 3 };
                        pos++;
                    }

                    if (i == vertexTriangleIndex)
                    {
                        startEdgeGroupIndex = _edgeCache.Count;
                    }
                    if (pos > 0)
                        _edgeCache.Add(edges);
                }
            }


            private Edge FindNextEdgeInNaibourEdgeGroup(Edge edge)
            {
                foreach (var edgeGroup in _edgeCache)
                {
                    // Если группа содержит ребро, то она уже обработана
                    if (edge.Equals(edgeGroup[0]) || edge.Equals(edgeGroup[1]))
                        continue;
                    // Если координаты ребра совпадают с координатами первого ребра группы, то возвращааем второе ребро группы
                    if (edgeGroup[0] != null && (edge.Vertex1 == edgeGroup[0].Vertex1 && edge.Vertex2 == edgeGroup[0].Vertex2
                        || edge.Vertex1 == edgeGroup[0].Vertex2 && edge.Vertex2 == edgeGroup[0].Vertex1))
                        return edgeGroup[1];
                    // Если координаты ребра совпадают с координатами второго ребра группы, возвращааем первое ребро группы
                    if (edgeGroup[1] != null && (edge.Vertex1 == edgeGroup[1].Vertex1 && edge.Vertex2 == edgeGroup[1].Vertex2
                        || edge.Vertex1 == edgeGroup[1].Vertex2 && edge.Vertex2 == edgeGroup[1].Vertex1))
                        return edgeGroup[0];
                }
                return null;
            }


            private bool AddEdgeCrossPoint(Vector3 vertex1, Vector3 vertex2, ref List<WrapPoint> crossPoints, out bool isCrosspointOutOfTreshold, bool checkTreshold)
            {
                isCrosspointOutOfTreshold = false;
                if (_piecePlane.SameSide(vertex1, vertex2))
                    return false;
                if (!_crossPlane.GetSide(vertex1) && !_crossPlane.GetSide(vertex2))
                    return false;
                float distance;
                var ray = new Ray(vertex1, vertex2 - vertex1);
                _piecePlane.Raycast(ray, out distance);
                var crossPoint = ray.GetPoint(distance);
                if (!_crossPlane.GetSide(crossPoint))
                    return false;
                crossPoints.Add(new WrapPoint { Origin = crossPoint, EdgePoint1 = vertex1, EdgePoint2 = vertex2 });
                isCrosspointOutOfTreshold = checkTreshold && IsPointOutOfTreshold(crossPoint);
                return true;
            }

            private bool IsCrossPointInWorkSpace(Vector3 vertex1, Vector3 vertex2)
            {
                if (_piecePlane.SameSide(vertex1, vertex2))
                    return false;
                if (!_crossPlane.GetSide(vertex1) && !_crossPlane.GetSide(vertex2))
                    return false;
                float distance;
                var ray = new Ray(vertex1, vertex2 - vertex1);
                _piecePlane.Raycast(ray, out distance);
                var crossPoint = ray.GetPoint(distance);
                if (!_crossPlane.GetSide(crossPoint))
                    return false;
                return true;
            }


            private bool IsPointOutOfTreshold(Vector3 point)
            {
                var pieceVector = _pieceInfo.FrontBandPoint - _pieceInfo.BackBandPoint;
                var worldPoint = _gameObject.transform.TransformPoint(point);
                var worldPointVector = worldPoint - _pieceInfo.BackBandPoint;
                var worldPointProjection = Vector3.Project(worldPointVector, pieceVector);
                var distance = (worldPointVector - worldPointProjection).sqrMagnitude;
                return distance > _sqrTreshold;
            }


            private List<WrapPoint> GetWrapPathFromCrossPoints(List<WrapPoint> crossPoints)
            {
                var points = new List<WrapPoint>();
                DefineWrapPatchPoint(_piecePlane, new WrapPoint { Origin = _localPiecePoints[3] }, crossPoints, ref points);
                return points;
            }

            private void DefineWrapPatchPoint(Plane sourcePlane, WrapPoint sourcePoint, List<WrapPoint> crossPoints, ref List<WrapPoint> pathPoints)
            {
                // Глобальная проверка
                var point = sourcePlane.normal + sourcePoint.Origin;
                var plane = new Plane(sourcePoint.Origin, _localPiecePoints[1], point);
                // Если все точки пересечения за плоскостью 
                if (crossPoints.All(crossPoint => !plane.GetSide(crossPoint.Origin)))
                {
                    return; // Ничего не добавляем
                }
                WrapPoint pathPoint = null;
                foreach (var crossPoint in crossPoints)
                {
                    bool no = false;
                    plane = new Plane(sourcePoint.Origin, crossPoint.Origin, point);
                    foreach (var pnt in crossPoints)
                    {
                        if (plane.GetSide(pnt.Origin) && !pnt.Equals(crossPoint))
                        {
                            no = true;
                            break;
                        }
                    }
                    if (no) continue;
                    //if (crossPoints.Any(cp => plane.GetSide(cp.Origin) && !cp.Equals(crossPoint))) continue;
                    pathPoint = crossPoint;
                    break;
                }
                if (pathPoint == null) return;
                crossPoints.Remove(pathPoint);
                pathPoints.Add(pathPoint);
                DefineWrapPatchPoint(sourcePlane, pathPoint, crossPoints, ref pathPoints);
            }

        }
    }
}
