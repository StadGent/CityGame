using UnityEngine;
using System.Collections;
using UnityStandardAssets.Curves;

public class BreakInterval : ScriptableObject
{
	public enum Points
	{
		PointA,
		PointB
	};

	public float _ProgressA;
	public float _ProgressB;
	public Vector3 _PointA;
	public Vector3 _PointB;
	public Quaternion _DirectionA;
	public Quaternion _DirectionB;
	public int _Speed;
	public int _Wait;
	public float _Length;
	public float _Margin = 0.005f;

	public bool InInterval(float progress)
	{
		return _ProgressA <= progress && _ProgressB >= progress;
	}

	public void Calculate(BezierSpline spline)
	{
		this._PointA = spline.GetPoint (this._ProgressA);
		this._DirectionA = Quaternion.LookRotation (spline.GetPoint (this._ProgressA - 0.01f) - this._PointA);

		this._PointB = spline.GetPoint (this._ProgressB);
		this._DirectionB = Quaternion.LookRotation (spline.GetPoint (this._ProgressB + 0.01f) - this._PointB);

		this.CalculateLength (spline);
	}

	public void CalculateLength(BezierSpline spline)
	{
		int segmentA = spline.GetSegment (this._ProgressA);
		float progressSegmentA = spline.SplineToSegmentTime (this._ProgressA);
		int segmentB = spline.GetSegment (this._ProgressB);
		float progressSegmentB = spline.SplineToSegmentTime (this._ProgressB);

		if (segmentA != segmentB)
			this._Length = progressSegmentA * spline.GetLength (segmentA) + progressSegmentB * spline.GetLength (segmentB) + (spline.GetCumulativeSegmentLengths () [segmentB - 1] - spline.GetCumulativeSegmentLengths () [segmentA + 1]);
		else
			this._Length = (progressSegmentB - progressSegmentA) * spline.GetLength (segmentA);
	}
}