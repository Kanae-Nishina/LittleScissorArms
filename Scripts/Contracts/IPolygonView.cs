using Assets.WrappingRope.Contracts.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.WrappingRope.Contracts
{
    public interface IPolygonView
    {
        event Mouse MouseDown;
        event Mouse MouseUp;
        event Mouse MouseMove;
        event Action MakeRegular;
        event Action<bool> InsertPointChanged;
        event Action<bool> DeletePointChanged;

        float Width { get; }
        float Height { get; }

        float Zoom { get; }
        List<Vector2> Polygon { get; }

        List<Symbol> Symbols { get; }
    }
}
