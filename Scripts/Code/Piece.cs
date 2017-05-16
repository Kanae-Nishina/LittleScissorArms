#define DEBUG
#undef DEBUG

using Assets.WrappingRope.Scripts;
using Assets.WrappingRope.Scripts.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WrappingRope.Scripts;
using WrappingRope.Scripts.Helpers;

public class Piece : MonoBehaviour
{
    public Piece FrontPiece;

    public Piece BackPiece;

    public GameObject FrontBandPoint;

    public GameObject BackBandPoint;

    public Vector3 PrevFrontBandPoint { get; set; }

    public Vector3 PrevBackBandPoint { get; set; }

    public Rope _rope;

    public Guid PieceUid { get; set; }

    public float Threshold
    {
        get { return _rope.Threshold; }
    }


    public float WrapDistance
    {
        get { return _rope.WrapDistance; }
    }


    public WrapPoint LastWrapPoint { get; set; }


    private float _length;
    public float Length { get { return _length; } }

    private Renderer _rend;

    private GameObject _bendPointInstance;

    protected List<Node[]> _sections = new List<Node[]>();

    protected float _backSectionDistance;
    protected float _maxDistance;

    void Awake()
    {
        _rend = GetComponent<Renderer>();
    }


    public void Init(GameObject fBP, GameObject bBP, Piece fP, Piece bP, Rope rope, bool resetPrevBandPoints = true)
    {
        var layer = rope.GetIgnoreLayer();
        gameObject.layer = layer;
        FrontBandPoint = fBP;
        BackBandPoint = bBP;
        FrontPiece = fP;
        BackPiece = bP;
        _rope = rope;
        if (fP != null)
            _bendPointInstance = _rope.GetBendInstance();
        PieceUid = Guid.NewGuid();
        gameObject.name = String.Format("Piece {0}", PieceUid);
        if (fP != null) fP.BackPiece = this;
        if (bP != null) bP.FrontPiece = this;
        SaveBandPointPositions();
        Relocate(resetPrevBandPoints);
        _baseProfile = _rope.GetWorkProfileClone();
        _helpProfile = _rope.GetWorkProfileClone();
        CreateSections();
    }


    public void CreateSections()
    {
        for (var i = 0; i < 2 + _rope.BendCrossectionsNumber; i++)
        {
            var nodes = new List<Node>();
            for (var j = 0; j < _rope.ProfileLenght(); j++)
            {
                nodes.Add(new Node(6));
            }
            _sections.Add(nodes.ToArray());
        }
    }

    public void Init(GameObject fBP, GameObject bBP, Piece fP, Piece bP, Rope rope, Vector3 pfBP, Vector3 pbBP, bool resetPrevBandPoints = true)
    {
        Init(fBP, bBP, fP, bP, rope, resetPrevBandPoints);
        PrevBackBandPoint = pbBP;
        PrevFrontBandPoint = pfBP;
    }

    public void SaveBandPointPositions()
    {
        var pos = BackBandPoint.transform.position;
        if (BackBandPoint != null)
        {
            PrevBackBandPoint = pos;
        }
        pos = FrontBandPoint.transform.position;
        if (FrontBandPoint != null)
        {
            PrevFrontBandPoint = pos;
        }
    }

    public bool IsCurrentlyBanded { get; set; }


    public void Relocate(bool resetPrevBandPoints = true)
    {
        if (resetPrevBandPoints)
        {
            PrevBackBandPoint = BackBandPoint.transform.position;
            PrevFrontBandPoint = FrontBandPoint.transform.position;
        }

        _length = (BackBandPoint.transform.position - FrontBandPoint.transform.position).magnitude;
    }



    // Update is called once per frame
    void Update()
    {
        Relocate(false);
        Vector3 direction = FrontBandPoint.transform.position - BackBandPoint.transform.position;
        //Vector3 direction =  BackBandPoint.transform.position - FrontBandPoint.transform.position;
        if (_rope.IsProcedural())
            return;
        transform.position = (FrontBandPoint.transform.position + BackBandPoint.transform.position) / 2;
        if (direction == Vector3.zero) return;
        switch (_rope.extendAxis)
        {
            case Axis.X:
                {
                    transform.rotation = Quaternion.LookRotation(direction) * Quaternion.AngleAxis(90f, Vector3.down);
                    transform.localScale = new Vector3(_rope.PieceInstanceRatio.x * _length, _rope.PieceInstanceRatio.y, _rope.PieceInstanceRatio.z);
                    break;
                }
            case Axis.Y:
                {
                    transform.rotation = Quaternion.LookRotation(direction, new Vector3(0, 1, 0)) * Quaternion.AngleAxis(90f, Vector3.right);
                    transform.localScale = new Vector3(_rope.PieceInstanceRatio.x, _rope.PieceInstanceRatio.y * _length, _rope.PieceInstanceRatio.z);
                    break;
                }
            case Axis.Z:
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                    transform.localScale = new Vector3(_rope.PieceInstanceRatio.x, _rope.PieceInstanceRatio.y, _rope.PieceInstanceRatio.z * _length);
                    break;
                }
        }
        // Изменяем размер объекта изгиба
        if (FrontPiece != null && _bendPointInstance != null)
        {
            _bendPointInstance.transform.localScale = new Vector3(_rope.BendInstanceRatio, _rope.BendInstanceRatio, _rope.BendInstanceRatio);
            _bendPointInstance.transform.position = FrontBandPoint.transform.position;
        }

        //SetBaseProfile();
    }


    public void Knee(GameObject point)
    {
        GameObject pieceObject = _rope.GetPieceInstance();
        Piece newPiece = pieceObject.GetComponent("Piece") as Piece;
        if (newPiece == null) return;
        newPiece.Init(point, BackBandPoint, this, BackPiece, _rope, point.transform.position, PrevBackBandPoint, false);
        BackBandPoint = point;
        newPiece.IsCurrentlyBanded = true;
        IsCurrentlyBanded = true;
        if (_rope != null)
            if (newPiece.BackPiece == null) _rope.BackPiece = newPiece;
        Relocate(false);
    }


    public Vector3 DefineBackBandPointVelocity()
    {
        return (BackBandPoint.transform.position - PrevBackBandPoint) / Time.fixedDeltaTime;
    }


    public Vector3 DefineFrontBandPointVelocity()
    {
        return (FrontBandPoint.transform.position - PrevFrontBandPoint) / Time.fixedDeltaTime;
    }



    public void TransformTexture(Vector2 scale, Vector2 translate)
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
            var errorTextureName = "Material of piece instance has not texture named '{0}'. Texturing of rope is possible only with Unity's builtin shaders with common texture names.";
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
            Debug.Log(string.Format(errorTextureName, "_BumpMap"));
#endif
        }

    }


    protected Vector3[] _baseProfile;
    protected Vector3[] _helpProfile;


    public void SetBaseProfile()
    {
        if (FrontPiece == null)
        {
            Array.Copy(_rope.GetBaseProfile(), _baseProfile, _baseProfile.Length);
            return;
        }
        var vec2 = BackBandPoint.transform.position - FrontBandPoint.transform.position; ;
        var vec1 = FrontPiece.BackBandPoint.transform.position - FrontPiece.FrontBandPoint.transform.position;
        var axis = new Plane(vec1, vec2, Vector3.zero).normal;
        var angle = Geometry.Angle(vec1, vec2, axis);
        Geometry.RotatePoly(_baseProfile, FrontPiece._baseProfile, angle, axis);
    }

    public void LocateSections(float ropeLength, ref float lengthBefore)
    {
        // !!! ВНИМАНИЕ: порядок следования методов имеет значение!
        SetBaseProfile(); // сначала устанавливаем базовый профиль у этого куска
        LocateMainSections(ropeLength, lengthBefore); // устанавливаем главные сечения
        SetKneeSections(); // Устанавливаем переходные сечения для предыдущего куска между этим и предыдущим куском
        lengthBefore = lengthBefore + _length;
    }


    public List<Node[]> GetSections()
    {
        var res = new List<Node[]>();
        int sectionsCount = BackPiece == null ? 2 : _sections.Count;
        for (var i = 0; i < sectionsCount; i++)
        {
            res.Add(_sections[i]);
        }
        return res;
    }

    public void DebugDrawSections()
    {
        var newPoly = new List<Vector3>();
        foreach (var section in _sections)
        {
            newPoly.Clear();
            Array.ForEach(section, point => newPoly.Add(point.Vertex));
            DebugDraw.DrawPoly(newPoly, Color.green, 0.1f);
        }
    }


    protected void LocateMainSections(float ropeLength, float lengthBefore)
    {
        var vec2 = BackBandPoint.transform.position - FrontBandPoint.transform.position;
        _length = vec2.magnitude;
        if (FrontPiece != null)
        {
            _frontExtend = (lengthBefore + FrontPiece._backSectionDistance) / ropeLength;
            var frontSectionPosition = FrontBandPoint.transform.position + vec2.normalized * FrontPiece._backSectionDistance;
            TranslateProfileToSection(_sections[0], _baseProfile, frontSectionPosition, _frontExtend);
        }
        else
        {
            _frontExtend = 0;
            TranslateProfileToSection(_sections[0], _baseProfile, FrontBandPoint.transform.position, _frontExtend);
        }
        if (BackPiece == null)
        {
            _backExtend = 1;
            TranslateProfileToSection(_sections[1], _baseProfile, BackBandPoint.transform.position, _backExtend);
            return;
        }
        var vec1 = BackPiece.BackBandPoint.transform.position - BackPiece.FrontBandPoint.transform.position;
        var cross = Vector3.Cross(vec1, vec2);
        var plane = new Plane(vec2, cross, Vector3.zero);
        var projects = new List<float>();
        Array.ForEach(_baseProfile, point => projects.Add(plane.GetDistanceToPoint(point)));  
        _maxDistance = projects.ToArray().Max();
        var middle = Vector3.Lerp(-vec1.normalized, vec2.normalized, 0.5f) * -1;
        var angle = (float)Math.PI * Vector3.Angle(-plane.normal, middle) / 180;
        _backSectionDistance = Mathf.Abs(_maxDistance * Mathf.Tan(angle));
        var backSectionPosition = BackBandPoint.transform.position - vec2.normalized * _backSectionDistance;
        _backExtend = (lengthBefore + _length - _backSectionDistance) / ropeLength;

        TranslateProfileToSection(_sections[1], _baseProfile, backSectionPosition, _backExtend);

        _kneePoint = backSectionPosition + plane.normal * _maxDistance;
        var frontSectionPosition1 = BackBandPoint.transform.position + vec1.normalized * _backSectionDistance;

        _frontBound = backSectionPosition - _kneePoint;
        _backBound = frontSectionPosition1 - _kneePoint;

    }

    protected Vector3 _kneePoint;

    protected Vector3 _frontBound;
    protected Vector3 _backBound;
    protected float _frontExtend;
    protected float _backExtend;

    protected void TranslateProfileToSection(Node[] section, Vector3[] profile, Vector3 direction, float extend)
    {
        for (var i = 0; i < profile.Length; i++)
        {
            section[i].Vertex = profile[i] + direction;
            section[i].Uv = _rope.GetUv((float)i / profile.Length, extend);
            section[i].ResetNormals();
        }
    }


    /// <summary>
    /// Устанавливаем промежуточные сечения в изгибе у предыдущего! куска
    /// </summary>
    protected void SetKneeSections()
    {
        if (FrontPiece == null)
            return;
        for(float i = 2; i < _sections.Count; i++)
        {
            float position = (i - 1) / (_sections.Count - 1);
            SetMiddleSection(FrontPiece._sections[(int)i], position);
        }
    }


    protected void SetMiddleSection(Node[] targetSection, float amount)
    {
        for(var i = 0; i < targetSection.Length; i++)
        {
            var angle = Vector3.Angle(FrontPiece._frontBound, FrontPiece._backBound) * amount;
            var uv = FrontPiece._backExtend + (_frontExtend - FrontPiece._backExtend) * amount;
            var plane = new Plane(FrontPiece._frontBound, FrontPiece._backBound, Vector3.zero);
            Geometry.RotatePoly(_helpProfile, FrontPiece._baseProfile, angle, plane.normal);
            var dir = Vector3.Slerp(FrontPiece._frontBound, FrontPiece._backBound, amount);
            targetSection[i].Vertex = _helpProfile[i] + FrontPiece._kneePoint + dir;
            targetSection[i].Uv = _rope.GetUv((float)i / targetSection.Length, uv);
            targetSection[i].ResetNormals();
        }
    }

    protected void DestroyBendPointInstance()
    {
        Destroy(_bendPointInstance);
    }


    private bool _isDestroyed = false;

    void OnDestroy()
    {
        if (!_isDestroyed)
        {
            // Если это краевой кусок спереди и сзади у него "сосед", то удаляем их объект изгиба
            if (FrontPiece == null && BackPiece != null)
            {
                BackPiece.DestroyBendPointInstance();
            }
            _isDestroyed = true;
            Destroy(_bendPointInstance);
        }
    }

    public override string ToString()
    {
        return string.Format("Piece: {0}", PieceUid);
    }
}
