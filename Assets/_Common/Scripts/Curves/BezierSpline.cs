using UnityEngine;
using System.Collections.Generic;
using System;

namespace UnityStandardAssets.Curves
{
	public class BezierSpline : MonoBehaviour
	{
		public bool _showAnchors = true;
		public bool _showHelperAnchors = true;
		public bool _showBreakAnchors = true;
		//public SplineParameters Parameters;
		[SerializeField] public int InterpolationSteps = 10;
		[SerializeField] public List<BezierPoint> SplinePoints = null;// All bezier points as gameobjects with script
		[SerializeField] public List<Vector3> ControlPoints = null;// Controlpoint vector as seen from SplinePoint transform
		[SerializeField] public List<BreakInterval> BreakIntervals =  null;

		public List<float[]> _cumulativeLengthsInSegment = new List<float[]>();

		float AverageLength(int segment)
		{
			if (_cumulativeLengthsInSegment.Count == 0) return 0.0f;
			return _cumulativeLengthsInSegment[segment][_cumulativeLengthsInSegment.Count - 1] / _cumulativeLengthsInSegment.Count;
		}

		//private List<List<float>> _lengthLookupTable = new List<List<float>>();
		//private float LookupLength(float t) { return LookupLength(SplineToSegmentTime(t), GetSegment(t)); }
		//private float LookupLength(float t, int segment)
		//{
		//    int index = (int)((1.0f / InterpolationSteps) * t);
		//    return _lengthLookupTable[segment][index];
		//}

		private List<float> _cumulativeSegmentLengths = new List<float>();
		public List<float> GetCumulativeSegmentLengths() {
			return this._cumulativeSegmentLengths;
		}
		public float GetLength(int segment)
		{
			if (segment >= _cumulativeSegmentLengths.Count || segment < 0) return 0.0f;
			if (segment == 0) return _cumulativeSegmentLengths[0];
			return _cumulativeSegmentLengths[segment] - _cumulativeSegmentLengths[segment - 1];
		}
		public float GetLengthPercentage(int segment)
		{
			if (_cumulativeSegmentLengths.Count == 0) return 0.0f;
			return GetLength(segment) / Length;
		}

		//private List<Vector3> _vectors = new List<Vector3>();

		public float Length
		{
			get
			{
				if (_cumulativeSegmentLengths.Count == 0) return 0.0f;
				return _cumulativeSegmentLengths[_cumulativeSegmentLengths.Count-1];
			}
		}

		private bool _loop;
		public bool Loop
		{
			get { return _loop; }
			set
			{
				if(_loop!=value)
				{
					_loop = value;
					if (_loop == true)
					{
						ControlPoints.Add(-ControlPoints[ControlPoints.Count-1]);
						ControlPoints.Add(-ControlPoints[0]);
						SplinePoints.Add(SplinePoints[0]);
					}
					else
					{
						SplinePoints.RemoveAt(SplinePoints.Count-1);
						ControlPoints.RemoveAt(ControlPoints.Count - 1);
						ControlPoints.RemoveAt(ControlPoints.Count - 1);
					}
					Recalculate();				
				}
			}
		}

		void Start()
		{
			Recalculate();
		}

		void OnValidate()
		{
			// When something has changed, recalculate path
			//Debug.Log("OnValidate");
			Recalculate();
		}

		void OnDrawGizmos()
		{
			Vector3? breakPoint;
			Color prevColor = Color.white;
			Color newColor;
			float width = 10f;
			float nextT;
			int nextS;
			// Draw bezier line
			Gizmos.color = Color.red;
			Gizmos.DrawSphere(GetPointOnSegment(1, SegmentCount - 1), 0.2f);
			float deltaT = 1.0f / (InterpolationSteps - 1);
			for (int s = 0; s < SegmentCount; ++s)
			{
				Gizmos.color = Color.red;
				Gizmos.DrawSphere(GetPointOnSegment(0, s), 0.2f);
				// Draw each segment
				for (int i = 0; i < InterpolationSteps; i++)
				{
					// Draw an interpolated curve
					nextT = i+1;
					nextS = s;
					if (nextT >= InterpolationSteps)
					{
						nextT = 0;
						nextS = s + 1;
					}

					Gizmos.color = (Color)prevColor;
					newColor = this.LineColor(deltaT * i, deltaT * (nextT), s, nextS, out breakPoint, prevColor);
					if (breakPoint != null && prevColor != newColor)
					{
						this.DrawLine (GetPointOnSegment (deltaT * i, s), (Vector3)breakPoint, prevColor == Color.white?1:width);
						Gizmos.color = newColor;
						this.DrawLine ((Vector3)breakPoint, GetPointOnSegment (deltaT * (i + 1), s), newColor == Color.white?1:width);
					}
					else
					{
						Gizmos.color = newColor;
						this.DrawLine (GetPointOnSegment (deltaT * i, s), GetPointOnSegment (deltaT * (i + 1), s), newColor == Color.white?1:width);
					}

					prevColor = newColor;
				}
			}
		}

		private Color LineColor(float t, float nextT, int segment, int nextSegment, out Vector3? breakPoint, Color prevColor)
		{
			float progress = this.SegmentTimeToSpline (t, segment);
			float nextProgress;
			if (nextSegment >= SegmentCount)
				nextProgress = 1;
			else
				nextProgress = this.SegmentTimeToSpline (nextT, nextSegment);
			breakPoint = null;
			if (BreakIntervals != null)
			{
				foreach (BreakInterval breakInterval in BreakIntervals)
				{
					if (breakInterval.InInterval (nextProgress) || breakInterval.InInterval (progress))
					{
						if (prevColor == Color.white)
							breakPoint = breakInterval._PointA;
						else
							breakPoint = breakInterval._PointB;

						if (!breakInterval.InInterval (nextProgress) && breakInterval.InInterval (progress))
							return Color.white;
						else
							return Color.red;
					}
				}
			}
			return Color.white;
		}

		public float adjustSpeed(float progress, float speed)
		{
			foreach (BreakInterval breakInterval in BreakIntervals)
			{
				if (breakInterval.InInterval(progress))
				{
					if (breakInterval._ProgressB - progress < breakInterval._Margin)
						speed = breakInterval._Speed;
					else
						speed = breakInterval._Speed + ((speed - breakInterval._Speed) * ((breakInterval._ProgressB - progress) / (breakInterval._ProgressB - breakInterval._ProgressA)));
				}
			}

			return speed;
		}

		private void DrawLine(Vector3 pointFrom, Vector3 pointTo, float width)
		{
			int count = Mathf.CeilToInt (width);
			if (count == 1)
				Gizmos.DrawLine (pointFrom, pointTo);
			else
			{
				Camera c = Camera.current;
				if (c == null)
					return;
				Vector3 v1 = (pointTo - pointFrom).normalized;
				Vector3 v2 = (c.transform.position - pointFrom).normalized;
				Vector3 n = Vector3.Cross (v1, v2);

				for (int i = 0; i < count; i++)
				{
					Vector3 o = n * ((float)i / (count - 1) - 0.5f)/width;
					Gizmos.DrawLine (pointFrom + o, pointTo + o);
				}
			}
				
		}

		public int SegmentCount { get { return (SplinePoints.Count - 1); } }

		public Vector3 GetPoint(float t) { return GetPointOnSegment(SplineToSegmentTime(t), GetSegment(t)); }            
		public Vector3 GetPointOnSegment(float t, int segment)
		{
			return Bezier.GetPoint(SplinePoints[segment].transform.position, GetControlPointWorldPos(segment + segment), GetControlPointWorldPos(segment + segment + 1), SplinePoints[segment+1].transform.position, t);
		}

		public Vector3 GetScaledPoint(float t)
		{
			t = Mathf.Clamp01(t);
			int cs = GetSegment(t);
			float st = SplineToSegmentTime(t);

			return GetPointOnSegment(st, cs);
		}

		public Vector3 GetDirection(float t) { return GetVelocity(t).normalized; }

		public Vector3 GetVelocity(float t) { return GetVelocityOnSegment(SplineToSegmentTime(t), GetSegment(t)); }
		public Vector3 GetVelocityOnSegment(float t, int segment)
		{
			return Bezier.GetFirstDerivative(SplinePoints[segment].transform.position, GetControlPointWorldPos(segment + segment), GetControlPointWorldPos(segment + segment + 1), SplinePoints[segment+1].transform.position, t);
		}

		public Vector3 GetAcceleration(float t) { return GetAccelerationOnSegment(SplineToSegmentTime(t), GetSegment(t)); }        
		public Vector3 GetAccelerationOnSegment(float t, int segment)
		{
			return Bezier.GetSecondDerivative(SplinePoints[segment].transform.position, GetControlPointWorldPos(segment + segment), GetControlPointWorldPos(segment+segment+1), SplinePoints[segment + 1].transform.position, t);
		}

		public int GetSegment(float t)
		{
			// Calculate the segment we are in
			t = Mathf.Clamp01(t);

			float lengthPercent = 0.0f;
			for (int i = 0; i < _cumulativeSegmentLengths.Count; i++)
			{
				lengthPercent += GetLength(i) / Length;
				// Is our time within this segment length?
				if (lengthPercent >= t) return i;
			}

			return 0;
		}

		public float SplineToSegmentTime(float t)
		{
			if (_cumulativeSegmentLengths.Count == 0) return 0;
			t = Mathf.Clamp01(t);
			int currentSegment = GetSegment(t);

			float formerDistance = 0.0f;
			if (currentSegment > 0) formerDistance = _cumulativeSegmentLengths[currentSegment - 1];
			float segmentTime = ((t * Length) - formerDistance) / GetLength(currentSegment);

			//segmentTime *= f;

			return segmentTime;
		}

		public float SegmentTimeToSpline(float t, int segment)
		{
			if (_cumulativeSegmentLengths.Count == 0) return 0;
			t = Mathf.Clamp01(t);

			float formerDistance = 0.0f;
			if (segment > 0) formerDistance = _cumulativeSegmentLengths[segment - 1];
			float splineTime = (t * (_cumulativeSegmentLengths[segment] - formerDistance) + formerDistance)/Length;
			return splineTime;
		}

		public float GetDistance(float t) { return t * Length; }

		public Vector3? GetPointFromStraightDistance(float sqrDistance, float progress, out float pointProgress)
		{
			float difference = 0.5f;
			float progressInterval = progress;
			float distanceInterval;
			float progressTo = 0;
			Vector3 fromPoint = GetPoint(progress);
			Vector3 toPoint = GetPoint (progressTo);
			float checkDistance = (fromPoint - toPoint).sqrMagnitude;

			if (checkDistance < sqrDistance)
			{
				pointProgress = 0f;
				return null;
				//throw new UnityException ("BezierSpline of " + name + " does not fit for the object riding it");
			}
			do
			{
				progressInterval /= 2;
				progressTo +=  progressInterval * Math.Sign(checkDistance - sqrDistance);
				distanceInterval = Mathf.Pow(Length, 2) * progressInterval;
				toPoint = GetPoint(progressTo);
				checkDistance = (fromPoint - toPoint).sqrMagnitude;
			} while (!(checkDistance > sqrDistance - difference && checkDistance < sqrDistance + difference) && distanceInterval >= difference);

			if (distanceInterval < difference)
			{
				pointProgress = 0f;
				return null;
				//throw new UnityException ("BezierSpline of " + name + " does not fit for the object riding it");
			}
			pointProgress = progressTo;
			return toPoint;
		}

		public void Recalculate()
		{
			float length = 0.0f;
			float totalLength = 0.0f;

			_cumulativeSegmentLengths.Clear();
			_cumulativeLengthsInSegment.Clear();

			float deltaT = 1.0f / (InterpolationSteps - 1);
			for (int s = 0; s < SegmentCount; s++)
			{
				length = 0.0f;
				// Calculate length of each segment
				_cumulativeLengthsInSegment.Add(new float[InterpolationSteps]);
				for (int i = 0; i < InterpolationSteps; i++)
				{
					// Length of a segment
					float dist = (GetPointOnSegment(deltaT * (i + 1), s) - GetPointOnSegment(deltaT * i, s)).magnitude;
					length += dist;

					// Length of a part of a segment
					float[] lastArray = _cumulativeLengthsInSegment[_cumulativeLengthsInSegment.Count - 1];
					lastArray[i] = length;
				}
		
				totalLength += length;
				_cumulativeSegmentLengths.Add(totalLength);
			}

			foreach (BreakInterval breakInterval in BreakIntervals)
			{
				breakInterval.Calculate (this);
			}

			//Debug.Log("Recalculate: Length=" + Length + " Segments=" + SegmentCount + "\nSplineLengthsCount=" + _splineLengths.Count);
			//for (int s = 0; s < SegmentCount; ++s)
			//{
			//    Debug.Log("Segment[" + s + "]: Length=" + _splineLengths[s] + " " + _splineLengths[s] / Length + "%");
			//}
		}

		public bool IsNearSpline(Vector3 pos, float maxDistance)
		{
			// Iterate through all interpolated steps
			// Check if point is near the bezier
			return true;
		}

		public float Sample(float t)
		{
			int count = _cumulativeSegmentLengths.Count;
			if (count == 0)
			{
				Debug.LogError("Unable to sample array - it has no elements");
				return 0;
			}
			if (count == 1) return _cumulativeSegmentLengths[0];
			float iFloat = t * (count - 1);
			int idLower = Mathf.FloorToInt(iFloat);
			int idUpper = Mathf.FloorToInt(iFloat + 1);
			if (idUpper >= count) return _cumulativeSegmentLengths[count - 1];
			if (idLower < 0) return _cumulativeSegmentLengths[0];            
			return Mathf.Lerp(_cumulativeSegmentLengths[idLower], _cumulativeSegmentLengths[idUpper], iFloat - idLower);
		}

		public int GetTangentControlPoint(int controlPoint)
		{
			// point 1 -> 2
			// point 0 -> if loop: count-1 else: -1
			// point 4 -> 3
			if (controlPoint % 2 == 0)
			{
				//if even
				if (controlPoint == 0 && Loop) return -1;
				return controlPoint - 1;
			}
			else
			{
				//if uneven
				if (controlPoint == ControlPoints.Count - 1)
				{
					if (!Loop) return -1;
					else return 0;
				}
				return controlPoint + 1;
			}
		}

		public Vector3 GetControlPointWorldPos(int controlPoint)
		{
			int point = GetControlPointParent(controlPoint);
			return SplinePoints [point].transform.position + ControlPoints[controlPoint];
		}

		public Vector3 GetControlPointInverseWorldPos(Vector3 controlPointPos, int controlPoint)
		{
			int point = GetControlPointParent(controlPoint);
			
			return SplinePoints[point].transform.InverseTransformPoint(controlPointPos);
		}

		public int GetControlPointParent(int controlPoint)
		{
			// C0 -> P0
			// C1 -> P1
			// C2 -> P1
			// C3 -> P2
			// C4 -> P2
			// C5 -> P3
			int point = controlPoint;
			if (controlPoint % 2 == 1) point = controlPoint + 1;
			point = point / 2;
			return point;
		}

		// Adds a new segment (1 point and 2 control points)
		public void AddSegment(bool before = false)
		{
			// Create new Points and ControlPoints
			Vector3 pointPos = SplinePoints[before?0:SplinePoints.Count - 1].transform.localPosition;
			Vector3 dir = GetDirection(1.0f);
			CreateBezierPoint(pointPos + dir * 10, before);

			if (before)
			{
				List<Vector3> newControlPoints = new List<Vector3> ();
				newControlPoints.Add (dir);
				newControlPoints.Add (-dir);
				newControlPoints.AddRange (ControlPoints);
				ControlPoints = newControlPoints;
			}
			else
			{
				ControlPoints.Add (dir);
				ControlPoints.Add (-dir);
			}

			Recalculate();
		}

		public BezierPoint CreateBezierPoint(Vector3 position, bool before = false)
		{
			GameObject point = new GameObject(before?"p0":"p" + SplinePoints.Count);
			point.transform.SetParent(transform);
			point.transform.localPosition = position;

			//Undo.RegisterCreatedObjectUndo(point, "Add BezierPoint");

			BezierPoint bzpoint = point.AddComponent<BezierPoint>();
			if(before)
			{
				List<BezierPoint> newList = new List<BezierPoint> ();
				int count = 1;
				newList.Add (bzpoint);
				foreach(BezierPoint oldPoint in SplinePoints)
				{
					oldPoint.name = "p" + count;
					count++;
					newList.Add (oldPoint);
				}
				SplinePoints = newList;
			}
			else
				SplinePoints.Add(bzpoint);

			return bzpoint;
		}

		public void AddBreakPoint(Camera sceneCamera)
		{
			float closestPoint =  0;
			float closestDistance = 0;
			if (sceneCamera != null)
			{
				for (float i = 0; i <= 1; i = i + 0.01f)
				{
					float splinePoint = i;
					Vector3 point = sceneCamera.WorldToViewportPoint(this.GetPoint (i));
					if (point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1)
					{
						float distance = Mathf.Pow ((point.x - 0.5f), 2) + Mathf.Pow ((point.y - 0.5f), 2);
						if (closestDistance == 0 || distance < closestDistance)
						{
							closestDistance = distance;
							closestPoint = splinePoint;
						}
					}
				}
				if (closestPoint != 0)
				{
					BreakInterval interval = ScriptableObject.CreateInstance<BreakInterval> ();
					interval._ProgressA = closestPoint - 0.01f;
					interval._ProgressB = closestPoint + 0.01f;
					interval.Calculate (this);
					BreakIntervals.Add (interval);
				}
			}
		}
	}
}