using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityStandardAssets.Utility;

namespace UnityStandardAssets.Curves
{
	[CustomEditor(typeof(BezierSpline))]
	public class BezierSplineInspector : Editor
	{
		static TimeSpan _mouseClick;

		public override void OnInspectorGUI()
		{
			// Keep default properties view
			base.OnInspectorGUI();
			BezierSpline spline = (BezierSpline)target;

			// 10 pixels of space
			GUILayout.Space(10);
		   
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			GUI.enabled = !spline.Loop;
			if (GUILayout.Button("Add Segment Before", GUILayout.Width(200)))
			{
				Undo.RecordObject(spline, "Add Segment Before");
				spline.AddSegment(true);
				EditorUtility.SetDirty(spline);
			}
			if (GUILayout.Button("Add Segment After", GUILayout.Width(200)))
			{
				Undo.RecordObject(spline, "Add Segment After");
				spline.AddSegment();
				EditorUtility.SetDirty(spline);
			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Add Breakpoint", GUILayout.Width(200)))
			{
				Undo.RecordObject(spline, "Add Breakpoint");
				spline.AddBreakPoint(SceneView.GetAllSceneCameras()[0]);
				EditorUtility.SetDirty(spline);
			}
			GUI.enabled = true;
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();

			GUILayout.Space(10);

			bool loop = GUILayout.Toggle(spline.Loop, "Loop");
			
			if(loop != spline.Loop)
			{
				Undo.RecordObject(spline, "Loop");
				spline.Loop = loop;
				EditorUtility.SetDirty(spline);
			}   

			bool show = GUILayout.Toggle(spline._showAnchors, "Show anchors");

			if(show != spline._showAnchors)
			{
				Undo.RecordObject(this, "Show anchors");
				spline._showAnchors = show;
				if (spline._showAnchors == false)
					spline._showHelperAnchors = false;
				EditorUtility.SetDirty(this);
				EditorUtility.SetDirty (spline);
			} 

			show = GUILayout.Toggle(spline._showHelperAnchors, "Show helper anchors");

			if(show != spline._showHelperAnchors)
			{
				Undo.RecordObject(this, "Show helper anchors");
				spline._showHelperAnchors = show;
				if (spline._showHelperAnchors == true)
					spline._showAnchors = true;
				EditorUtility.SetDirty(this);
				EditorUtility.SetDirty (spline);
			}   

			show = GUILayout.Toggle(spline._showBreakAnchors, "Show break anchors");

			if(show != spline._showBreakAnchors)
			{
				Undo.RecordObject(this, "Show break anchors");
				spline._showBreakAnchors = show;
				EditorUtility.SetDirty(this);
				EditorUtility.SetDirty (spline);
			}          

			GUILayout.Space(10);
		}		

		private void OnSceneGUI()
		{
			BezierSpline spline = (BezierSpline)target;
			bool change = false;
			Vector3 oldPos;
			//if (spline.Parameters == null) return;

			// Draw Root
			Handles.color = Color.yellow;
			Handles.CubeCap(0, spline.transform.position, Quaternion.identity, 0.4f);

			// Draw First Point
			if (spline._showAnchors)
			{
				Undo.RecordObject(spline.SplinePoints[0].transform, "Move Point");
				Handles.color = Color.red;
				Handles.SphereCap (0, spline.SplinePoints [0].transform.position, Quaternion.identity, 0.3f);
				oldPos = spline.SplinePoints [0].transform.position;
				spline.SplinePoints [0].transform.position = Handles.PositionHandle (spline.SplinePoints [0].transform.position, Quaternion.identity);
				if ((oldPos - spline.SplinePoints [0].transform.position).magnitude > 0.00001f)
				{
					change = true;

					//spline.ControlPoints[0] += (spline.SplinePoints[0].transform.position - oldPos);
					// Remember to add the loop control point if required
				}
			}

			//int loop = spline.Loop ? 1 : 0;

			for (int s = 0; s < spline.SegmentCount; ++s)// - loop
			{
				// Draw each segment

				if (spline._showAnchors)
				{
					// Draw Point
					Undo.RecordObject (spline.SplinePoints [s + 1].transform, "Move Point");
					Handles.color = Color.red;
					Handles.SphereCap (0, spline.SplinePoints [s + 1].transform.position, Quaternion.identity, 0.3f);
					oldPos = spline.SplinePoints [s + 1].transform.position;
					spline.SplinePoints [s + 1].transform.position = Handles.PositionHandle (spline.SplinePoints [s + 1].transform.position, Quaternion.identity);
					if ((oldPos - spline.SplinePoints [s + 1].transform.position).magnitude > 0.00001f)
					{
						change = true;                   
					}
				}

				if (spline._showHelperAnchors)
				{
					for (int i = 0; i < 2; ++i)
					{
						// Draw Control point
						Undo.RecordObject (spline, "Move Control Point");
						Handles.color = Color.magenta;
						Handles.SphereCap (0, spline.GetControlPointWorldPos (s + s + i), Quaternion.identity, 0.2f);
						oldPos = spline.ControlPoints [s + s + i];

						spline.ControlPoints [s + s + i] = spline.GetControlPointInverseWorldPos (Handles.PositionHandle (spline.GetControlPointWorldPos (s + s + i), Quaternion.identity), s + s + i);
						if ((oldPos - spline.ControlPoints [s + s + i]).magnitude > 0.00001f)
						{
							// If the control point was moved, set the bordering control point to the opposite location
							change = true;
							int iTangent = spline.GetTangentControlPoint (s + s + i);
							if (iTangent != -1)
								spline.ControlPoints [iTangent] = -spline.ControlPoints [s + s + i];
						}

						// Draw control handles
						Handles.color = Color.yellow;
						Handles.DrawLine (spline.SplinePoints [s].transform.position, spline.GetControlPointWorldPos (s + s));
						Handles.DrawLine (spline.SplinePoints [s + 1].transform.position, spline.GetControlPointWorldPos (s + s + 1));
					}
				}

				if (spline._showBreakAnchors)
				{
					if (spline.BreakIntervals != null)
					{
						foreach (BreakInterval breakInterval in spline.BreakIntervals)
						{
							Handles.color = Color.red;
							CustomHandles.DragHandleResult dhResult;
							this.adjustBreakInterval (breakInterval, CustomHandles.DragHandle (breakInterval._PointA, breakInterval._DirectionA, 1f, Handles.ConeCap, Color.yellow, out dhResult), BreakInterval.Points.PointA);
							this.adjustBreakInterval (breakInterval, CustomHandles.DragHandle (breakInterval._PointB, breakInterval._DirectionB, 1f, Handles.ConeCap, Color.yellow, out dhResult), BreakInterval.Points.PointB);
						}
					}
				}
			}     

			//if(spline.Loop)
			//{
			//	// Draw last 2 control handles
			//	Handles.color = Color.yellow;
			//	Handles.DrawLine(spline.SplinePoints[0].transform.position, spline.GetControlPointWorldPos(spline.ControlPoints.Count-1));
			//	//Handles.DrawLine(spline.SplinePoints[spline.SplinePoints.Count-1].transform.position, spline.GetControlPointWorldPos(spline.ControlPoints.Count-2));
			//}

			// Recalculate length and such if the positions has been changed
			if (change) spline.Recalculate();  
		}

		private void adjustBreakInterval(BreakInterval breakInterval, Vector3 newPosition, BreakInterval.Points pointToHandle)
		{
			BezierSpline spline = (BezierSpline)target;
			Vector3 pointToChange = pointToHandle == BreakInterval.Points.PointA ? breakInterval._PointA : breakInterval._PointB;
			float directionChange = (pointToChange - newPosition).x / spline.Length;
			if (pointToHandle == BreakInterval.Points.PointA)
			{
				breakInterval._ProgressA = breakInterval._ProgressA - directionChange;
				if (breakInterval._ProgressA > breakInterval._ProgressB)
					breakInterval._ProgressA = breakInterval._ProgressB;
			}
			else
			{
				breakInterval._ProgressB = breakInterval._ProgressB - directionChange;
				if (breakInterval._ProgressB < breakInterval._ProgressA)
					breakInterval._ProgressB = breakInterval._ProgressA;
			}
			breakInterval.Calculate (spline);
		}

		void Callback(object obj)
		{
			Debug.Log("Selected: " + obj);
		}

		private void OnScene(SceneView sceneview)
		{
			BezierSpline spline = (BezierSpline)target;

			if (spline == null)
			{
				SceneView.onSceneGUIDelegate -= OnScene;
				return;
			}

			// Always draw path
			/*
			for (int s = 0; s < spline.SegmentCount; ++s)
			{
				// Draw each segment
				Handles.color = Color.white;
				Handles.DrawBezier(spline.SplinePoints[s].transform.position, spline.SplinePoints[s + 1].transform.position, spline.GetControlPointWorldPos(s + s), spline.GetControlPointWorldPos(s + s + 1), Color.white, null, 2f);
				//Handles.DrawBezier(spline.SplinePoints[s].transform.position, spline.SplinePoints[s+1].transform.position, spline.SplinePoints[s].transform.position + spline.ControlPoints[s+s], spline.SplinePoints[s+1].transform.position + spline.ControlPoints[s+s+1] - spline.transform.position, Color.white, null, 2f);
			}
			*/
			//if(spline.Loop)
			//{
			//	Handles.DrawBezier(spline.SplinePoints[spline.SplinePoints.Count-1].transform.position, spline.SplinePoints[0].transform.position, spline.GetControlPointWorldPos(spline.ControlPoints.Count-2), spline.GetControlPointWorldPos(spline.ControlPoints.Count - 1), Color.white, null, 2f);
			//}
		}

		void OnEnable()
		{
			BezierSpline spline = (BezierSpline)target;
			SceneView.onSceneGUIDelegate -= OnScene;
			SceneView.onSceneGUIDelegate += OnScene;

			// Initialize the spline
			if (spline.SplinePoints == null || spline.ControlPoints == null) InitializeSpline(spline);
		}

		void InitializeSpline(BezierSpline spline)
		{
			spline.SplinePoints = new List<BezierPoint>();
			spline.ControlPoints = new List<Vector3>();

			// Create first segment
			spline.CreateBezierPoint(new Vector3(0, 0, 0));
			spline.CreateBezierPoint(new Vector3(0, 0, 10));

			spline.ControlPoints.Add(new Vector3(0, 0, 2));
			spline.ControlPoints.Add(new Vector3(0, 0, -2));
		}
	}
}