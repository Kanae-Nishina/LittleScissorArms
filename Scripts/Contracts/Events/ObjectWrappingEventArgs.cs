using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.WrappingRope.Contracts.Events
{
    public class ObjectWrapEventArgs : CancelEventArgs
    {
        private readonly Vector3[] _wrapPoints;
        private readonly GameObject _target;


        public Vector3[] WrapPoints
        {
            get { return _wrapPoints; }
        }

        public GameObject Target
        {
            get { return _target; }
        }


        public ObjectWrapEventArgs(GameObject target, Vector3[] wrapPoints)
        {
            _target = target;
            _wrapPoints = wrapPoints;
        }
    }

    public delegate void ObjectWrapEventHandler(Rope sender, ObjectWrapEventArgs args);
}
