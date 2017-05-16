using UnityEngine;

namespace WrappingRope.Scripts
{
    public class PieceInfo
    {
        public Vector3 FrontBandPoint { get; set; }
        public Vector3 BackBandPoint { get; set; }
        public Vector3 PrevFrontBandPoint { get; set; }
        public Vector3 PrevBackBandPoint { get; set; }
        public Piece Piece { get; set; }


        public PieceInfo()
        {

        }

        public PieceInfo(Piece piece)
        {
            Piece = piece;
            FrontBandPoint = piece.FrontBandPoint.transform.position;
            BackBandPoint = piece.BackBandPoint.transform.position;
            PrevBackBandPoint = piece.PrevBackBandPoint;
            PrevFrontBandPoint = piece.PrevFrontBandPoint;
        }
    }
}
