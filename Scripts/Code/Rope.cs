#define DEBUG
#undef DEBUG


using UnityEngine;
using System.Collections.Generic;
using System;
using Ray = UnityEngine.Ray;
using WrappingRope.Scripts;
using Assets.WrappingRope.Scripts.Utils;
using Assets.WrappingRope.Scripts;
using WrappingRope.Scripts.Helpers;
using System.Linq;
using Assets.WrappingRope.Contracts;
using Assets.WrappingRope.Contracts.Events;


//[ExecuteInEditMode]
public class Rope : MonoBehaviour
{
    [SerializeField]
    private GameObject _frontEnd;
    public GameObject FrontEnd
    {
        get { return _frontEnd; }
    }

    [SerializeField]
    private GameObject _backEnd;
    public GameObject BackEnd
    {
        get { return _backEnd; }
    }

    public float Threshold;

    public float WrapDistance;

    [SerializeField]
    private GameObject _pieceInstance;

    public GameObject BendInstance;

    [SerializeField]
    private float _width;
    public float Width
    {
        get
        {
            return _width;
        }
        set
        {
            _width = value;
            SetPieceInstanceRatio();
            SetBendInstanceRatio();
            SetProfileWidth(value);
        }
    }

    public Axis extendAxis;

    public TexturingMode TexturingMode;
    public UVLocation UVLocation;

    [SerializeField]
    private AnchoringMode _anchoringMode;
    public AnchoringMode AnchoringMode
    {
        get { return _anchoringMode; }
        set
        {
            if (_anchoringMode == value)
                return;
            _anchoringMode = value;
            if (value != AnchoringMode.None)
            {
                _initialLength = GetRopeLength();
            }
        }
    }

    [Range(0, float.MaxValue)]
    public float ElasticModulus = 0;

    private bool _isDestroyed;


    [SerializeField]
    private string _ignoreLayer;

    public Piece FrontPiece { get; set; }

    public Piece BackPiece { get; set; }


    public Vector3 PieceInstanceSize { get; private set; }


    public Vector3 PieceInstanceRatio { get; private set; }
    public float BendInstanceRatio { get; private set; }

    public int BendCrossectionsNumber
    {
        get { return MeshConfiguration.BendCrossectionsNumber; }
    }

    private List<Vector3> _initProfile = new List<Vector3>();

    private Vector3[] _workProfile;
    private Vector3[] _baseProfile;
    private Renderer _rend;
    private MeshFilter _meshFilter;


    protected float _initialLength;

    public int ProfileLenght()
    {
        return _initProfile.Count;
    }

    public MeshConfigurator MeshConfiguration;

    private Vector2 _textureSize;

    private List<Vector3> _triangulationPath1;
    private List<Vector3> _triangulationPath2;


    public event ObjectWrapEventHandler ObjectWrap;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
        _meshFilter = GetComponent<MeshFilter>();
        int width = 1;
        int height = 1;
        if (IsProcedural())
        {
            var texture = _rend.material.mainTexture;
            if (texture != null)
            {
                width = texture.width;
                height = texture.height;
            }
        }
        _textureSize = new Vector2(width, height);

        _initProfile = MeshConfiguration.Profile;
        var points = new List<Vector3>();
        _initProfile.ForEach(v => points.Add(v));
        _workProfile = points.ToArray();
        Geometry.ScalePoly(_workProfile, _workProfile, Width);
        _baseProfile = GetWorkProfileClone();
        _prevVect = Vector3.forward;

        if (IsProcedural())
        {
            var triangulator = new Triangulator();
            var profile = MeshConfiguration.Profile.Select(point => (Vector2)point).ToList();
            _triangulationPath1 = triangulator.GetTriangulationIndexes(profile);
            _triangulationPath2 = new List<Vector3>(_triangulationPath1.Count);
            for (var i = _triangulationPath1.Count - 1; i >= 0; i--)
            {
                var triangle = _triangulationPath1[_triangulationPath1.Count - 1 - i];
                _triangulationPath2.Add(new Vector3(triangle.x, triangle.z, triangle.y));
            }
            SetTransformToDefault();
        }

        if (WrapDistance < 0.001f) WrapDistance = 0.001f;
        if (_width < 0.001f) _width = 0.001f;
        if (Threshold < .03f) Threshold = 0.03f;
        CheckAndCorrectIgnoreLayerName();
        SetPieceInstanceRatio();
        SetBendInstanceRatio();

        GameObject pieceInstance = GetPieceInstance();
        Piece piece = pieceInstance.GetComponent("Piece") as Piece;
        FrontPiece = piece;
        BackPiece = piece;
        piece.Init(_frontEnd, _backEnd, null, null, this);
        if (_anchoringMode != AnchoringMode.None)
        {
            _initialLength = GetRopeLength();
        }
    }


    private void SetTransformToDefault()
    {
        transform.position = new Vector3(0, 0, 0);
        transform.rotation = new Quaternion(0, 0, 0, 1);
        transform.localScale = new Vector3(1, 1, 1);
    }

    private void SetPieceInstanceRatio()
    {
        if (_pieceInstance == null)
        {
            PieceInstanceRatio = new Vector3(1, 1, 1);
            return;
        }
        var renderer = _pieceInstance.GetComponent<MeshRenderer>();
        if (renderer == null || IsProcedural())
        {
            PieceInstanceRatio = new Vector3(1, 1, 1);
            return;
        }
        PieceInstanceSize = renderer.bounds.size;
        switch (extendAxis)
        {
            case Axis.X:
                PieceInstanceRatio = new Vector3(1 / PieceInstanceSize.x, Width / PieceInstanceSize.y, Width / PieceInstanceSize.z);
                break;
            case Axis.Y:
                PieceInstanceRatio = new Vector3(Width / PieceInstanceSize.x, 1 / PieceInstanceSize.y, Width / PieceInstanceSize.z);
                break;
            case Axis.Z:
                PieceInstanceRatio = new Vector3(Width / PieceInstanceSize.x, Width / PieceInstanceSize.y, 1 / PieceInstanceSize.z);
                break;
        }
    }


    private void SetBendInstanceRatio()
    {
        if (BendInstance == null || IsProcedural())
        {
            BendInstanceRatio = 1f;
            return;
        }
        var BandInstanceSize = BendInstance.GetComponent<MeshRenderer>().bounds.size.x;
        BendInstanceRatio = Width / BandInstanceSize;
    }

    private void SetProfileWidth(float width)
    {
        if (!IsProcedural())
            return;
        Geometry.ScalePoly(_workProfile, _initProfile.ToArray(), width);
    }


    public GameObject GetPieceInstance()
    {
        GameObject pieceInstance;
        if (_pieceInstance == null)
            pieceInstance = new GameObject();
        else
            pieceInstance = Instantiate(_pieceInstance) as GameObject;
        pieceInstance.AddComponent<Piece>();
        return pieceInstance;
    }


    public GameObject GetBendInstance()
    {
        if (BendInstance == null)
            return null;
        GameObject bendInstance = Instantiate<GameObject>(BendInstance);
        return bendInstance;
    }


    protected int GetPiecesCount()
    {
        var piece = FrontPiece;
        int cnt = 0;
        do
        {
            cnt++;
            piece = piece.BackPiece;
        } while (piece != null);
        return cnt;
    }

    void FixedUpdate()
    {
        MainUpdate();
    }


    void Update()
    {
        if (!IsProcedural())
            Texturing();
    }


    void LateUpdate()
    {
        if (IsProcedural())
        {
            UpdateProceduralRope();
        }
    }

    #region Procedural Mesh Generation

    private Vector3 _prevVect;
    private Quaternion _rotation = new Quaternion(0, 0, 0, 1);

    private void UpdateProceduralRope()
    {
        float length = 0;
        var piece = FrontPiece;
        var ropeLength = GetRopeLength();
        do
        {
            piece.LocateSections(ropeLength, ref length);
            piece = piece.BackPiece;
        }
        while (piece != null);

        GenerateMesh();

        Vector2 textureTransform = new Vector2(0, 0);
        Vector2 textureScale;
        var textureRatio = _textureSize.x / _textureSize.y;
        if (TexturingMode == TexturingMode.Stretched)
        {
            textureScale = new Vector2(1, 1);
        }
        else
        {
            if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.AlongU)
            {
                textureScale = new Vector2(ropeLength / (Width * 3) / textureRatio, 1);
                if (TexturingMode == TexturingMode.TiledFromBackEnd)
                    textureTransform = new Vector2((float)Math.Truncate(ropeLength / (Width * 3) / textureRatio) - ropeLength / (Width * 3) / textureRatio, 0);
            }
            else
            {
                textureScale = new Vector2(1, ropeLength / (Width * 3) * textureRatio);
                if (TexturingMode == TexturingMode.TiledFromBackEnd)
                    textureTransform = new Vector2(0, (float)Math.Truncate(ropeLength / (Width * 3) * textureRatio) - ropeLength / (Width * 3) * textureRatio);

            }

        }
        TransformTexture(textureScale, textureTransform);

    }


    private void TransformTexture(Vector2 scale, Vector2 translate)
    {
        if (_rend == null)
            return;
        if (_rend.material.HasProperty("_MainTex"))
        {
            _rend.material.SetTextureScale("_MainTex", scale);
            _rend.material.SetTextureOffset("_MainTex", translate);
        }
        else
        {
#if DEBUG
            var errorTextureName = "Material of rope has not texture named '{0}'. Texturing of rope is possible only with Unity's builtin shaders with common texture names.";
            Debug.Log(string.Format(errorTextureName, "_MainTex"));
#endif
        }
        if (_rend.material.HasProperty("_BumpMap"))
        {
            _rend.material.SetTextureScale("_BumpMap", scale);
            _rend.material.SetTextureOffset("_BumpMap", translate);
        }
        else
        {
#if DEBUG
            var errorTextureName = "Material of rope has not texture named '{0}'. Texturing of rope is possible only with Unity's builtin shaders with common texture names.";
            Debug.Log(string.Format(errorTextureName, "_BumpMap"));
#endif
        }

    }


    private void GenerateMesh()
    {
        var vertices = new List<Vertex>();
        var triangles = new List<int>();
        Node[] section1;
        Node[] section2;
        List<Node[]> sections = new List<Node[]>();
        var piece = FrontPiece;
        do
        {
            sections.AddRange(piece.GetSections());
            piece = piece.BackPiece;
        }
        while (piece != null);

        int sectionsCount = sections.Count;
        for (var i = 0; i < sectionsCount - 1; i++)
        {
            section1 = sections[i];
            section2 = sections[i + 1];
            // Для всех точек
            for (var j = 0; j < section1.Length; j++)
            {
                float extend;
                var nextJ = j == section1.Length - 1 ? 0 : j + 1;
                // Грань:
                //Треугольник:
                var normal = new Plane(section1[j].Vertex, section2[j].Vertex, section1[nextJ].Vertex).normal;
                
                // Точка 1
                var nodeIndex = new int[] { i, j };
                vertices.Add(new Vertex(section1[j].Vertex, nodeIndex, section1[j].Uv));
                triangles.Add(vertices.Count - 1);
                
              
                if (!MeshConfiguration.FlipNormals)
                {
                    // Точка 2
                    nodeIndex = new int[] { i + 1, j };
                    vertices.Add(new Vertex(section2[j].Vertex, nodeIndex, section2[j].Uv));
                    triangles.Add(vertices.Count - 1);

                    // Точка 3
                    nodeIndex = new int[] { i, nextJ };
                    if (nextJ == 0)
                    {
                        if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.AlongU)
                            extend = section1[nextJ].Uv.x;
                        else
                            extend = section1[nextJ].Uv.y;
                        vertices.Add(new Vertex(section1[nextJ].Vertex, nodeIndex, GetUv(1, extend)));
                    }
                    else
                        vertices.Add(new Vertex(section1[nextJ].Vertex, nodeIndex, section1[nextJ].Uv));
                    triangles.Add(vertices.Count - 1);
                }
                else
                {
                    // Точка 3
                    nodeIndex = new int[] { i, nextJ };
                    if (nextJ == 0)
                    {
                        if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.AlongU)
                            extend = section1[nextJ].Uv.x;
                        else
                            extend = section1[nextJ].Uv.y;
                        vertices.Add(new Vertex(section1[nextJ].Vertex, nodeIndex, GetUv(1, extend)));
                    }
                    else
                        vertices.Add(new Vertex(section1[nextJ].Vertex, nodeIndex, section1[nextJ].Uv));
                    triangles.Add(vertices.Count - 1);
                    // Точка 2
                    nodeIndex = new int[] { i + 1, j };
                    vertices.Add(new Vertex(section2[j].Vertex, nodeIndex, section2[j].Uv));
                    triangles.Add(vertices.Count - 1);
                }

                section1[j].AddNormal(normal);
                section1[nextJ].AddNormal(normal);
                //Debug.DrawRay(section1[j].Vertex, normal);
                //Debug.DrawRay(section1[nextJ].Vertex, normal);

                //Второй треугольник:
                normal = new Plane(section1[nextJ].Vertex, section2[j].Vertex, section2[nextJ].Vertex).normal;
                // Точка 1
                nodeIndex = new int[] { i, nextJ };
                if (nextJ == 0)
                {
                    if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.AlongU)
                        extend = section1[nextJ].Uv.x;
                    else
                        extend = section1[nextJ].Uv.y;
                    vertices.Add(new Vertex(section1[nextJ].Vertex, nodeIndex, GetUv(1, extend)));
                }
                else
                    vertices.Add(new Vertex(section1[nextJ].Vertex, nodeIndex, section1[nextJ].Uv));
                triangles.Add(vertices.Count - 1);


                if (!MeshConfiguration.FlipNormals)
                {
                    // Точка 2
                    nodeIndex = new int[] { i + 1, j };
                    vertices.Add(new Vertex(section2[j].Vertex, nodeIndex, section2[j].Uv));
                    triangles.Add(vertices.Count - 1);

                    // Точка 3
                    nodeIndex = new int[] { i + 1, nextJ };
                    if (nextJ == 0)
                    {
                        if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.AlongU)
                            extend = section2[nextJ].Uv.x;
                        else
                            extend = section2[nextJ].Uv.y;

                        vertices.Add(new Vertex(section2[nextJ].Vertex, nodeIndex, GetUv(1, extend)));
                    }
                    else
                        vertices.Add(new Vertex(section2[nextJ].Vertex, nodeIndex, section2[nextJ].Uv));
                    triangles.Add(vertices.Count - 1);
                }
                else
                {
                    // Точка 3
                    nodeIndex = new int[] { i + 1, nextJ };
                    if (nextJ == 0)
                    {
                        if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.AlongU)
                            extend = section2[nextJ].Uv.x;
                        else
                            extend = section2[nextJ].Uv.y;

                        vertices.Add(new Vertex(section2[nextJ].Vertex, nodeIndex, GetUv(1, extend)));
                    }
                    else
                        vertices.Add(new Vertex(section2[nextJ].Vertex, nodeIndex, section2[nextJ].Uv));
                    triangles.Add(vertices.Count - 1);
                    // Точка 2
                    nodeIndex = new int[] { i + 1, j };
                    vertices.Add(new Vertex(section2[j].Vertex, nodeIndex, section2[j].Uv));
                    triangles.Add(vertices.Count - 1);
                }
                section2[j].AddNormal(normal);
                section2[nextJ].AddNormal(normal);
                //Debug.DrawRay(section2[j].Vertex, normal);
                //Debug.DrawRay(section2[nextJ].Vertex, normal);


            }
        }
        var vCount = vertices.Count;
        if (!MeshConfiguration.FlipNormals)
        {
            CreateCap(_triangulationPath1, sections[0], 0, vertices, triangles);
            CreateCap(_triangulationPath2, sections[sections.Count - 1], sections.Count - 1, vertices, triangles);
        }
        else
        {
            CreateCap(_triangulationPath2, sections[0], 0, vertices, triangles);
            CreateCap(_triangulationPath1, sections[sections.Count - 1], sections.Count - 1, vertices, triangles);
        }
        var vAr = vertices.ToArray();

        for (var i = 0; i < vCount; i++)
        {
            var normal = sections[vertices[i].NodeIndex[0]][vertices[i].NodeIndex[1]].GetAverageNormal();
            vAr[i].Normal = MeshConfiguration.FlipNormals ? normal * (-1) : normal;
            //Debug.DrawRay(vAr[i].Position, vAr[i].Normal);
        }

        if (_meshFilter != null)
        {
            var mesh = _meshFilter.mesh;
            mesh.Clear();
            mesh.vertices = vAr.Select(v => v.Position).ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = vAr.Select(v => v.Uv).ToArray();
            mesh.normals = vAr.Select(v => v.Normal).ToArray();
        }

    }


    private void CreateCap(List<Vector3> triangulationPath, Node[] section, int sectionIndex, List<Vertex> vertices, List<int> triangles)
    {
        // Берем 3 точки из первого треугольника
        var normal = new Plane(section[(int)triangulationPath[0].x - 1].Vertex, section[(int)triangulationPath[0].y - 1].Vertex, section[(int)triangulationPath[0].z - 1].Vertex).normal;
        foreach(var triIndex in triangulationPath)
        {
            var nodeIndex = new int[] { sectionIndex, (int)triIndex.x - 1 };
            vertices.Add(new Vertex(section[(int)triIndex.x - 1].Vertex, nodeIndex, new Vector2(0, 0), normal));
            triangles.Add(vertices.Count - 1);

            nodeIndex = new int[] { sectionIndex, (int)triIndex.y - 1 };
            vertices.Add(new Vertex(section[(int)triIndex.y - 1].Vertex, nodeIndex, new Vector2(0, 0), normal));
            triangles.Add(vertices.Count - 1);

            nodeIndex = new int[] { sectionIndex, (int)triIndex.z - 1 };
            vertices.Add(new Vertex(section[(int)triIndex.z - 1].Vertex, nodeIndex, new Vector2(0, 0), normal));
            triangles.Add(vertices.Count - 1);
        }
    }


    public bool IsProcedural()
    {
        return _meshFilter != null && _rend != null;
    }

    public Vector2 GetUv(float cross, float extend)
    {
        switch (UVLocation)
        {
            case UVLocation.AlongU:
            case UVLocation.ContraU:
                return new Vector2(extend, cross);
            case UVLocation.AlongV:
            case UVLocation.ContraV:
                return new Vector2(cross, extend);
            default:
                return new Vector2(extend, cross);
        }
    }

    public Vector3[] GetBaseProfile()
    {
        var vec1 = _prevVect;
        var vec2 = FrontPiece.BackBandPoint.transform.position - FrontPiece.FrontBandPoint.transform.position;
        var axis = new Plane(vec1, vec2, Vector3.zero).normal;
        var angle = Vector3.Angle(vec1, vec2);
        _rotation = Quaternion.AngleAxis(angle, axis) * _rotation;
        Geometry.RotatePoly(_baseProfile, _workProfile, _rotation);
        var newPoly = new List<Vector3>();
        Array.ForEach(_baseProfile, point => newPoly.Add(point));
        newPoly = Geometry.TranslatePoly(newPoly, FrontPiece.FrontBandPoint.transform.position);
        _prevVect = vec2;
        return _baseProfile;
    }


    public Vector3[] GetWorkProfileClone()
    {
        var points = new List<Vector3>();
        for (var i = 0; i < _workProfile.Length; i++)
        {
            points.Add(_workProfile[i]);
        }
        return points.ToArray();
    }
    #endregion

    private void MainUpdate()
    {
        try
        {
            MergeRope(FrontPiece);
            if (GetPiecesCount() < 30)
                BandRope(FrontPiece);
            AnchoreRope();
            ProcessElastic();
            RelocatePieces(FrontPiece);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }


    protected float GetRopeLength()
    {
        Piece piece = FrontPiece;
        float length = 0;
        do
        {
            length += piece.Length;
            piece = piece.BackPiece;
        }
        while (piece != null);
        return length;

    }


    public void CutRope(float length, Direction dir)
    {
        if (_anchoringMode != AnchoringMode.None)
        {
            if (_initialLength >= length)
                _initialLength = _initialLength - length;
            return;
        }
        if (length < 0)
        {
#if DEBUG
            Debug.Log("Negative value of length is not possible with AnchoringMode.None.");
#endif
            return;
        }
        CutRopeNotAnchoring(length, dir);
    }


    public void CutRopeNotAnchoring(float length, Direction dir = Direction.BackToFront)
    {
        if (dir == Direction.BackToFront)
        {
            var pieceLength = BackPiece.Length;
            if (pieceLength < length)
            {
                if (BackPiece.FrontPiece == null)
                {
                    BackPiece.BackBandPoint.transform.position = BackPiece.FrontBandPoint.transform.position;
                    return;
                }
                else
                {
                    //Сдвигаем задний конец куска к переднему
                    BackPiece.BackBandPoint.transform.position = BackPiece.FrontBandPoint.transform.position;
                    //Назначаем задний конец для следующего куска, как задний конец куска, который удаляется 
                    BackPiece.FrontPiece.BackBandPoint = BackPiece.BackBandPoint;

                    //Задним куском веревки назначаем следующий кусок
                    BackPiece = BackPiece.FrontPiece;

                    //Удаляем бывший передний конец куска
                    Destroy(BackPiece.BackPiece.FrontBandPoint.gameObject);
                    var dist = BackPiece.BackPiece.gameObject;
#if DEBUG
                    Debug.Log(string.Format("PiecUid for cut: {0}", BackPiece.BackPiece.PieceUid));
#endif
                    //Удаляем сам кусок
                    Destroy(dist);
                    BackPiece.BackPiece = null;
                    length = length - pieceLength;
                    CutRopeNotAnchoring(length, dir);
                }
            }
            else
            {
                Vector3 pieceDirection = BackPiece.FrontBandPoint.transform.position - BackPiece.BackBandPoint.transform.position;
                BackPiece.BackBandPoint.transform.position = BackPiece.BackBandPoint.transform.position + pieceDirection.normalized * length;
                BackPiece.Relocate(false);
            }
        }
        else
        {
            var pieceLength = FrontPiece.Length;
            //var pieceLength = Vector3.Distance(FrontPiece.FrontBandPoint.transform.position, FrontPiece.BackBandPoint.transform.position);
            if (pieceLength < length)
            {
                if (FrontPiece.BackPiece == null)
                {
                    FrontPiece.FrontBandPoint.transform.position = FrontPiece.BackBandPoint.transform.position;
                    return;
                }
                else
                {
                    //Сдвигаем передний конец куска к заднему
                    FrontPiece.FrontBandPoint.transform.position = FrontPiece.BackBandPoint.transform.position;
                    //Назначаем передний конец для предыдущего куска, как передний конец куска, который удаляется 
                    FrontPiece.BackPiece.FrontBandPoint = FrontPiece.FrontBandPoint;

                    //Передним куском веревки назначаем предыдущий кусок
                    FrontPiece = FrontPiece.BackPiece;

                    //Удаляем бывший задний конец куска
                    Destroy(FrontPiece.FrontPiece.BackBandPoint.gameObject);
                    var dist = FrontPiece.FrontPiece.gameObject;
#if DEBUG
                    Debug.Log(string.Format("PiecUid for cut: {0}", FrontPiece.FrontPiece.PieceUid));
#endif
                    //Удаляем сам кусок
                    Destroy(dist);
                    FrontPiece.FrontPiece = null;
                    length = length - pieceLength;
                    CutRopeNotAnchoring(length, dir);
                }
            }
            else
            {
                Vector3 pieceDirection = FrontPiece.BackBandPoint.transform.position - FrontPiece.FrontBandPoint.transform.position;
                FrontPiece.FrontBandPoint.transform.position = FrontPiece.FrontBandPoint.transform.position + pieceDirection.normalized * length;
                FrontPiece.Relocate(false);
            }
        }
    }

    protected void AnchoreRope()
    {
        float difLength;
        var length = GetRopeLength();
        if (_anchoringMode == AnchoringMode.None || length < _initialLength)
            return;
        difLength = length - _initialLength;
        HoldLength(difLength);
        ProcessPendulum();
    }


    protected void GetPendulumInfo(out Piece piece, out Vector3 pieceDirection, out Rigidbody weight, out Vector3 weightVelocity)
    {
        if (_anchoringMode == AnchoringMode.ByBackEnd)
        {
            pieceDirection = FrontPiece.BackBandPoint.transform.position - FrontPiece.FrontBandPoint.transform.position;
            weight = FrontPiece.FrontBandPoint.GetComponent<Rigidbody>();
            piece = FrontPiece;
            weightVelocity = FrontPiece.DefineFrontBandPointVelocity();
        }
        else
        {
            pieceDirection = BackPiece.FrontBandPoint.transform.position - BackPiece.BackBandPoint.transform.position;
            weight = BackPiece.BackBandPoint.GetComponent<Rigidbody>();
            piece = BackPiece;
            weightVelocity = BackPiece.DefineBackBandPointVelocity();
        }
    }


    protected void HoldLength(float difLength)
    {
        if (_anchoringMode == AnchoringMode.ByFrontEnd)
        {
            CutRopeNotAnchoring(difLength, Direction.BackToFront);
        }
        else
        {
            CutRopeNotAnchoring(difLength, Direction.FrontToBack);
        }
    }

    protected void ProcessPendulum()
    {
        Vector3 pieceDirection;
        Rigidbody weight;
        Piece endPiece;
        Vector3 weightVeloc;
        GetPendulumInfo(out endPiece, out pieceDirection, out weight, out weightVeloc);
        if (weight == null)
            return;

        var grav = Physics.gravity;
        var force = Vector3.Project(-grav, pieceDirection).magnitude;
        weight.AddForce(-weight.velocity, ForceMode.VelocityChange);
        weight.AddForce(pieceDirection.normalized * force, ForceMode.Acceleration);

        var weightVelocMagn = weightVeloc.magnitude;
        var angle = Vector3.Angle(weightVeloc, pieceDirection);
        if (angle > 85 && angle < 95)
        {
            // Считаем, что если угол между куском и вектором скорости груза близок к 90 град, то маятник в свободном подвесе
            var cross = Vector3.Cross(weightVeloc, pieceDirection);
            // скорость груза направляем перпендикулярно куску
            Vector3.OrthoNormalize(ref pieceDirection, ref cross, ref weightVeloc);
        }
        else
        {
            // иначе считаем что маятник дернули, то есть направление скорости не меняем (вектор скорости направлен в сторону, куда дернули), только нормализуем
            weightVeloc.Normalize();
        }
        weight.AddForce(weightVeloc * weightVelocMagn, ForceMode.VelocityChange);
        //Debug.DrawRay(weight.transform.position, weightVeloc * weightVelocMagn, Color.red);
        //Debug.Log(weightVelocMagn);
    }


    protected void ProcessElastic()
    {
        if (ElasticModulus == 0)
            return;
        var piece = FrontPiece;
        while ((piece != null) && piece.BackPiece != null)
        {
            var rBody = (Rigidbody)piece.BackBandPoint.transform.parent.GetComponent<Rigidbody>();
            if (rBody != null)
            {
                var force = (piece.FrontBandPoint.transform.position - piece.BackBandPoint.transform.position).normalized + (piece.BackPiece.BackBandPoint.transform.position - piece.BackPiece.FrontBandPoint.transform.position).normalized;
                rBody.AddForceAtPosition(force * ElasticModulus, piece.BackBandPoint.transform.position, ForceMode.Impulse);
            }
            piece = piece.BackPiece;
        }
    }


    private void RelocatePieces(Piece piece)
    {
        do
        {
            piece.Relocate();
            piece = piece.BackPiece;
        } while (piece != null);
    }


    private void MergeRope(Piece piece)
    {

        if (piece.BackPiece == null) return;
        if (!piece.IsCurrentlyBanded)
        {
            // Проверяем наличие угла, только если куски двигались (для оптимизации)
            if (IsMoveEndsOfKnee(piece))
            {
                var isKnee = CheckKnee(piece.FrontBandPoint, piece.BackBandPoint, piece.BackPiece.BackBandPoint);
                if (!isKnee)
                {
                    piece.BackBandPoint.transform.parent = null;
                    MergePieces(piece, piece.BackPiece);
                    MergeRope(piece);
                }
            }
        }
        piece.IsCurrentlyBanded = false;
        if (piece.BackPiece != null) MergeRope(piece.BackPiece);
    }

    /// <summary>
    /// Проверяет двигались ли куски для угла, образованного передаваемым куском и соседним  с заду куском
    /// </summary>
    /// <param name="piece">Кусок</param>
    /// <returns></returns>
    private bool IsMoveEndsOfKnee(Piece piece)
    {
        return (piece.FrontBandPoint.transform.parent != piece.BackBandPoint.transform.parent
                && (piece.DefineBackBandPointVelocity() != Vector3.zero
                        || piece.DefineFrontBandPointVelocity() != Vector3.zero))
                ||
                (piece.BackBandPoint.transform.parent != piece.BackPiece.BackBandPoint.transform.parent
                && (piece.DefineBackBandPointVelocity() != Vector3.zero
                        || piece.BackPiece.DefineBackBandPointVelocity() != Vector3.zero));
    }

    private void MergePieces(Piece piece1, Piece piece2)
    {
        try
        {
            piece1.BackBandPoint = piece2.BackBandPoint;
            piece1.PrevBackBandPoint = piece2.PrevBackBandPoint;
            piece1.LastWrapPoint = new WrapPoint { Origin = piece2.FrontBandPoint.transform.position };
            Destroy(piece2.FrontBandPoint);
            if (piece2.BackPiece == null)
            {

                BackPiece = piece1;
                piece1.BackPiece = null;
            }
            else
            {
                piece1.BackPiece = piece2.BackPiece;
                piece1.BackPiece.FrontPiece = piece1;
            }
#if DEBUG
            Debug.Log(string.Format("Merge: PiecUid for distroy: {0}", piece2.PieceUid));
#endif
            Destroy(piece2.gameObject);
            piece1.Relocate(false);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
    }


    private bool CheckKnee(GameObject endPoint1, GameObject bendPoint, GameObject endPoint2)
    {
        if (bendPoint.transform.parent == null) return false;
        Vector3 direct1 = (endPoint1.transform.position - bendPoint.transform.position).normalized;
        Vector3 direct2 = (endPoint2.transform.position - bendPoint.transform.position).normalized;
        var end1 = bendPoint.transform.position;
        var direction1 = (direct2 + direct1).normalized;
        var direction2 = endPoint2.transform.position - endPoint1.transform.position;
        var controlDistance = WrapDistance + 0.03f;
        var ang = (double)(Math.PI * Vector3.Angle(direction1 * controlDistance, direct1) / 180.0);
        var sin = Math.Sin(ang);
        var len = (float)(controlDistance / Math.Sqrt(1 - sin * sin));
        var dir1 = direct1 * len;
        var dir2 = direct2 * len;
        var or1 = end1 + dir1;
        var dir3 = dir2 - dir1;
#if DEBUG
        Debug.DrawRay(or1, dir3, Color.red, 0);
        Debug.DrawRay(or1, dir1, Color.red, 0);
#endif
#if DEBUG
        Debug.DrawRay(end1, direction1, Color.green, 0);
#endif
        HitInfo hitInfo;
        try
        {
            if (!dir3.Equals(Vector3.zero)
                    && Geometry.TryRaycast(new Ray(or1, Vector3.Normalize(dir3)), bendPoint.transform.parent.gameObject, dir3.magnitude, out hitInfo)
                )
                return true;
            if (!direction1.Equals(Vector3.zero)
                    && Geometry.TryRaycast(new Ray(end1, direction1), bendPoint.transform.parent.gameObject, direction1.magnitude * 1.2f, out hitInfo)
                )
                return true;
            if (
                    Geometry.TryRaycast(new Ray(endPoint1.transform.position, direction2.normalized), bendPoint.transform.parent.gameObject, direction2.magnitude, out hitInfo)
                )
            {
                return true;
            }

        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }
        return false;
    }


    private void BandRope(Piece sourcePiece)
    {
        var nextPiece = sourcePiece.BackPiece;
        HitInfo hitInfo;
        bool isIntersect = false;
        var pieceInfo = new PieceInfo(sourcePiece);
        // Запоминаем предыдущее положение конца (оно может измниться для обработки оборачивания)
        var prevFrontBackPoint = sourcePiece.PrevFrontBandPoint;
        if (TryGetIntersect(ref pieceInfo, out hitInfo))
        {
            try
            {
                var wrapper = new GameObjectWraper(pieceInfo, hitInfo);
                var wrapPoints = wrapper.GetWrapPoints();
                var piece = sourcePiece;
                if (wrapPoints.Count > 0)
                {
                    // Проверка на то, что созданные при обертывании куски пересекают объект. Это может быть по причине, например, если неправильно определен путь обертывания
                    isIntersect = IsIntersection(sourcePiece.FrontBandPoint.transform.position, wrapPoints[0], hitInfo.GameObject) ||
                        IsIntersection(sourcePiece.BackBandPoint.transform.position, wrapPoints[wrapPoints.Count - 1], hitInfo.GameObject);
                    if (!isIntersect)
                    {
                        if (wrapPoints.Count > 1)
                        {
                            for (var i = 0; i < wrapPoints.Count - 1; i++)
                            {
                                if (IsIntersection(wrapPoints[i], wrapPoints[i + 1], hitInfo.GameObject))
                                {
                                    isIntersect = true;
                                    break;
                                    Debug.LogWarning("One or more pieces created from wrap path intersect game object. Wrapping will be canceled");

#if DEBUG
//                            Debug.LogWarning("One or more pieces created from wrap path intersect game object. Wrapping will be canceled");
#endif
                                }
                            }
                        }
                        bool cancelWrapping = false;
                        OnObjectWrapping(hitInfo.GameObject, wrapPoints.ToArray(), out cancelWrapping);
                        if (!cancelWrapping && !isIntersect && wrapPoints.Count > 0)
                        {
                            foreach (var point in wrapPoints)
                            {
                                KneePiece(piece, point, hitInfo.GameObject);
                                piece = piece.BackPiece;
                            }
                            nextPiece = piece.BackPiece;
                        }
                    }

                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        sourcePiece.LastWrapPoint = null;
        sourcePiece.PrevFrontBandPoint = prevFrontBackPoint;
        if (nextPiece != null) BandRope(nextPiece);
    }

    private void OnObjectWrapping(GameObject gameObject, Vector3[] wrapPoints, out bool cancel)
    {
        var args = new ObjectWrapEventArgs(gameObject, wrapPoints);
        if (ObjectWrap != null)
            ObjectWrap(this, args);
        cancel = args.Cancel;
    }

    private bool IsIntersection(Vector3 startPoint, Vector3 stopPoint, GameObject gameObject)
    {
        var dir = stopPoint - startPoint;
        Ray ray = new Ray() { origin = startPoint, direction = dir.normalized };
        HitInfo hitInfo;
        return Geometry.TryRaycast(ray, gameObject, dir.magnitude, out hitInfo);
    }

    private void KneePiece(Piece piece, Vector3 point, GameObject gameObject)
    {
        GameObject pointGameObject = new GameObject();
        pointGameObject.transform.position = point;
        pointGameObject.transform.parent = gameObject.transform;
        piece.Knee(pointGameObject);
    }

    private bool TryGetIntersect(ref PieceInfo pieceInfo, out HitInfo hitInfo)
    {
        // Если кусок образовался слиянием двух кусков в предыдущем цикле
        if (pieceInfo.Piece.LastWrapPoint != null)
        {
            pieceInfo.Piece.PrevFrontBandPoint = pieceInfo.Piece.LastWrapPoint.Origin;
            pieceInfo.PrevFrontBandPoint = pieceInfo.Piece.LastWrapPoint.Origin;
        }
        hitInfo = new HitInfo();

        int layer = GetActionLayer();

        var pieceMoveStages = GetPieceMoveStages(pieceInfo);
        foreach (var stage in pieceMoveStages)
        {
#if DEBUG
            Debug.DrawRay(stage[0], stage[1] - stage[0], Color.red, 1f);
#endif
            var needCheckSameObject = pieceInfo.Piece.BackBandPoint.transform.parent != pieceInfo.Piece.FrontBandPoint.transform.parent;
            if ((needCheckSameObject &&
               (
                    (pieceInfo.Piece.BackBandPoint != _backEnd && Geometry.TryRaycast(new Ray(stage[0], stage[1] - stage[0]), pieceInfo.Piece.BackBandPoint.transform.parent.gameObject, (stage[1] - stage[0]).magnitude, out hitInfo))
                    || (pieceInfo.Piece.FrontBandPoint != _frontEnd && Geometry.TryRaycast(new Ray(stage[0], stage[1] - stage[0]), pieceInfo.Piece.FrontBandPoint.transform.parent.gameObject, (stage[1] - stage[0]).magnitude, out hitInfo))
               ))
               ||
               Geometry.Raycast(new Ray(stage[0], stage[1] - stage[0]), out hitInfo, (stage[1] - stage[0]).magnitude, layer)
               )
            {
                // Проверка на нахождение другого конца внутри объекта, с которым ударился кусок
                var ray = new Ray(stage[1], stage[0] - stage[1]);

                HitInfo hitInfo2;
                if (!Geometry.TryRaycast(ray, hitInfo.GameObject, (stage[0] - stage[1]).magnitude, out hitInfo2))
                //if (!hitInfo.collider.Raycast(ray, out hitInfo2, (stage[0] - stage[1]).magnitude))
                {
#if DEBUG
                    Debug.Log(string.Format("Inner Piece: {0}", pieceInfo.Piece));
#endif
                    if (pieceInfo.Piece.BackPiece == null)
                        return false;
                }
                pieceInfo.FrontBandPoint = stage[0];
                pieceInfo.BackBandPoint = stage[1];
                return true;
            }
        }
        return false;
    }


    private List<Vector3[]> GetPieceMoveStages(PieceInfo pieceInfo)
    {
        var stages = new List<Vector3[]>();
        var frontShift = (pieceInfo.PrevFrontBandPoint - pieceInfo.FrontBandPoint).sqrMagnitude;
        var backShift = (pieceInfo.PrevBackBandPoint - pieceInfo.BackBandPoint).sqrMagnitude;
        float i;
        Vector3 frontStep;
        Vector3 backStep;
        var sqrThreshold = Threshold * Threshold;
        if (frontShift > sqrThreshold || backShift > sqrThreshold)
        {
            if (frontShift > backShift)
            {
                i = (float)Math.Sqrt(frontShift) / Threshold;
                frontStep = (pieceInfo.PrevFrontBandPoint - pieceInfo.FrontBandPoint).normalized * Threshold;
                backStep = (pieceInfo.PrevBackBandPoint - pieceInfo.BackBandPoint) / (i);
            }
            else
            {
                i = (float)Math.Sqrt(backShift) / Threshold;
                backStep = (pieceInfo.PrevBackBandPoint - pieceInfo.BackBandPoint).normalized * Threshold;
                frontStep = (pieceInfo.PrevFrontBandPoint - pieceInfo.FrontBandPoint) / (i);
            }

            var frontBandPointStage = pieceInfo.PrevFrontBandPoint;
            var backBandPointStage = pieceInfo.PrevBackBandPoint;
            for (int j = 1; j < (int)i + 1; j++)
            {
                frontBandPointStage = frontBandPointStage - frontStep;
                backBandPointStage = backBandPointStage - backStep;
                stages.Add(new[] { frontBandPointStage, backBandPointStage });
            }
        }
        stages.Add(new[] { pieceInfo.FrontBandPoint, pieceInfo.BackBandPoint });
        return stages;
    }

    #region IgnoreLayer

    public int GetActionLayer()
    {
        string ignoreLayer = GetIgnoreLayerName();
        var layer = 1 << LayerMask.NameToLayer(ignoreLayer);
        return ~layer;
    }


    public int GetIgnoreLayer()
    {
        return LayerMask.NameToLayer(GetIgnoreLayerName());
    }


    private string GetIgnoreLayerName()
    {
        return _ignoreLayer;
    }


    private void CheckAndCorrectIgnoreLayerName()
    {
        bool isCorrect = true;
        string warning = string.Empty;
        if (string.IsNullOrEmpty(_ignoreLayer))
        {
            warning = "IgnoreLayer not setted.";
            isCorrect = false;
        }
        else if (_ignoreLayer == "Default")
        {
            warning = "IgnoreLayer couldn't be 'Default' layer.";
            isCorrect = false;
        }
        else if (LayerMask.NameToLayer(_ignoreLayer) == -1)
        {
            warning = "IgnoreLayer not exists in layers list.";
            isCorrect = false;
        }
        if (!isCorrect)
        {
#if DEBUG
            Debug.LogWarning(string.Format("{0} IgnoreLayer will be setted to 'Ignore Raycast'.", warning ));
#endif
            _ignoreLayer = "Ignore Raycast";
        }
    }
    #endregion

    protected float GetStretchAmount()
    {
        switch (extendAxis)
        {
            case Axis.X:
                return PieceInstanceRatio.x / PieceInstanceRatio.y;
            case Axis.Y:
                return PieceInstanceRatio.y / PieceInstanceRatio.x;
            case Axis.Z:
                return PieceInstanceRatio.z / PieceInstanceRatio.x;
        }
        return 1f;
    }

    #region Texturing
    protected void Texturing()
    {
        switch (TexturingMode)
        {
            case TexturingMode.None:
                ResetTexture();
                break;
            case TexturingMode.Stretched:
                {
                    if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.ContraV)
                        TexturingMode5(UVLocation);
                    else
                        TexturingMode6(UVLocation);
                    break;
                }
            case TexturingMode.TiledFromFrontEnd:
                {
                    if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.ContraV)
                        TexturingMode4(UVLocation);
                    else
                        TexturingMode2(UVLocation);

                    break;
                }
            case TexturingMode.TiledFromBackEnd:
                {
                    if (UVLocation == UVLocation.ContraU || UVLocation == UVLocation.ContraV)
                        TexturingMode3(UVLocation);
                    else
                        TexturingMode1(UVLocation);

                    break;
                }
        }
    }

    private void ResetTexture()
    {
        Piece piece = BackPiece;
        if (piece == null) return;
        do
        {
            piece.TransformTexture(new Vector2(1, 1), new Vector2(0, 0));
            piece = piece.FrontPiece;
        }
        while (piece != null);
    }

    // Fixed - Back, UV - Along U (left to right), Along V (bottom to top)
    private void TexturingMode1(UVLocation uvLocation)
    {
        Piece piece = BackPiece;
        if (piece == null) return;
        float ratio = GetStretchAmount();
        float length = 0;
        var scale = new Vector2();
        var translate = new Vector2();
        do
        {
            if (uvLocation == UVLocation.AlongU)
            {
                scale = new Vector2(piece.Length * ratio, 1);
                translate = new Vector2(length * ratio - (float)Math.Truncate(length * ratio), 0);
            }
            else
            {
                scale = new Vector2(1, piece.Length * ratio);
                translate = new Vector2(0, length * ratio - (float)Math.Truncate(length * ratio));
            }

            piece.TransformTexture(scale, translate);
            length += piece.Length;
            piece = piece.FrontPiece;
        }
        while (piece != null);
    }

    // Fixed - Front, UV - Along U (left to right), Along V (bottom to top)
    private void TexturingMode2(UVLocation uvLocation)
    {
        Piece piece = FrontPiece;
        if (piece == null) return;
        float ratio = GetStretchAmount();
        float length = 0;
        var scale = new Vector2();
        var translate = new Vector2();
        do
        {
            length += piece.Length;
            if (uvLocation == UVLocation.AlongU)
            {
                scale = new Vector2(piece.Length * ratio, 1);
                translate = new Vector2(1 - length * ratio - (float)Math.Truncate(length * ratio), 0);
            }
            else
            {
                scale = new Vector2(1, piece.Length * ratio);
                translate = new Vector2(0, 1 - length * ratio - (float)Math.Truncate(length * ratio));
            }
            piece.TransformTexture(scale, translate);
            piece = piece.BackPiece;
        }
        while (piece != null);
    }

    // Fixed - Back, UV - Contra U (right to left), Contra V (top to bottom)
    private void TexturingMode3(UVLocation uvLocation)
    {
        Piece piece = BackPiece;
        if (piece == null) return;
        float ratio = GetStretchAmount();
        float length = 0;
        var scale = new Vector2();
        var translate = new Vector2();

        do
        {
            length += piece.Length;
            if (uvLocation == UVLocation.ContraU)
            {
                scale = new Vector2(piece.Length * ratio, 1);
                translate = new Vector2(1 - length * ratio - (float)Math.Truncate(length * ratio), 0);
            }
            else
            {
                scale = new Vector2(1, piece.Length * ratio);
                translate = new Vector2(0, 1 - length * ratio - (float)Math.Truncate(length * ratio));
            }
            piece.TransformTexture(scale, translate);
            piece = piece.FrontPiece;
        }
        while (piece != null);
    }


    // Fixed - Front, UV - Contra U (right to left), Contra V (top to bottom)
    private void TexturingMode4(UVLocation uvLocation)
    {
        Piece piece = FrontPiece;
        if (piece == null) return;
        float ratio = GetStretchAmount();
        float length = 0;
        var scale = new Vector2();
        var translate = new Vector2();
        do
        {
            if (uvLocation == UVLocation.ContraU)
            {
                scale = new Vector2(piece.Length * ratio, 1);
                translate = new Vector2(length * ratio - (float)Math.Truncate(length * ratio), 0);
            }
            else
            {
                scale = new Vector2(1, piece.Length * ratio);
                translate = new Vector2(0, length * ratio - (float)Math.Truncate(length * ratio));
            }
            piece.TransformTexture(scale, translate);
            length += piece.Length;
            piece = piece.BackPiece;
        }
        while (piece != null);
    }


    // Contra U, Contra V
    private void TexturingMode5(UVLocation uvLocation)
    {
        Piece piece = BackPiece;
        if (piece == null) return;
        float length = 0;
        float totalLength = GetRopeLength();
        var scale = new Vector2();
        var translate = new Vector2();
        do
        {
            length += piece.Length;

            if (uvLocation == UVLocation.ContraU)
            {
                scale = new Vector2(piece.Length / totalLength, 1);
                translate = new Vector2(1 - length / totalLength, 0);
            }
            else
            {
                scale = new Vector2(1, piece.Length / totalLength);
                translate = new Vector2(0, 1 - length / totalLength);
            }

            piece.TransformTexture(scale, translate);
            piece = piece.FrontPiece;
        }
        while (piece != null);
    }

    private void TexturingMode6(UVLocation uvLocation)
    {
        Piece piece = BackPiece;
        if (piece == null) return;
        float length = 0;
        float totalLength = GetRopeLength();
        var scale = new Vector2();
        var translate = new Vector2();
        do
        {
            if (uvLocation == UVLocation.AlongU)
            {
                scale = new Vector2(piece.Length / totalLength, 1);
                translate = new Vector2(length / totalLength, 0);
            }
            else
            {
                scale = new Vector2(1, piece.Length / totalLength);
                translate = new Vector2(0, length / totalLength);
            }
            piece.TransformTexture(scale, translate);
            length += piece.Length;
            piece = piece.FrontPiece;
        }
        while (piece != null);
    }


    #endregion

    #region Destroying

    void OnDestroy()
    {
        DestroyRope();
    }


    protected void DestroyRope()
    {
        if (_isDestroyed)
            return;
        Piece piece = FrontPiece;
        piece.FrontBandPoint = null;
        do
        {
            if (piece.gameObject != null)
                Destroy(piece.gameObject);
            piece = piece.BackPiece;
            if (piece != null && piece.FrontBandPoint != null)
                Destroy(piece.FrontBandPoint);
        }
        while (piece != null);
        FrontPiece = null;
        if (BackPiece != null)
            BackPiece.BackBandPoint = null;

    }

    void OnApplicationQuit()
    {
        DestroyRope();
        _isDestroyed = true;
    }

    #endregion

}


public enum Direction
    {
        FrontToBack,
        BackToFront

    }


public enum Axis
{
    X,Y,Z
}

public enum TexturingMode
{
    None = 0,
    Stretched = 1,
    TiledFromBackEnd = 2,
    TiledFromFrontEnd = 3
}

public enum UVLocation
{
    AlongU = 0,
    ContraU = 1,
    AlongV = 2,
    ContraV = 3
}


public enum AnchoringMode
{
    None = 0,
    ByFrontEnd = 1,
    ByBackEnd = 2
}