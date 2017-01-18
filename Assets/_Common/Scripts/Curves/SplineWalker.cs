using System.Collections.Generic;
using UnityEngine;

namespace UnityStandardAssets.Curves
{
	namespace Enum { public enum Direction : int { Forward = 1, Backward = -1 } }

	public class SplineWalker : MonoBehaviour
	{
		[SerializeField] public List<Transform> Bones = new List<Transform>();
		[SerializeField] public List<Transform> Straights = new List<Transform> ();
		[SerializeField] public float Speed;
		[SerializeField] public float AdjustedSpeed;
		[SerializeField] public Enum.Direction Direction = Enum.Direction.Forward;
		[SerializeField] public bool hingedBones = false;
		[SerializeField] public BezierSpline Spline;
		[SerializeField] public float Progress = 0.0f;

		private List<float> _distanceBones = new List<float>();
		private List<Vector3> _distanceStraights = new List<Vector3>();
		private List<Quaternion> _offsetStraights = new List<Quaternion>();
		private List<Quaternion> _offsetLocalStraights = new List<Quaternion>();
		private List<Quaternion> _lookDirectionStraights = new List<Quaternion>();
		private List<Quaternion> _offsetBones = new List<Quaternion>();
		private List<Quaternion> _currentRelativeBoneRotation = new List<Quaternion> ();
		private SkinnedMeshRenderer[] _childRenderers;
		private LODGroup[] _lodGroups;
		private bool _renderersVisible;
		private bool _adjustingSpeed = false;

		private float _deltaMove = 0.0f;

		private void Start()
		{
			this.AdjustedSpeed = Speed;
			this._childRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
			this._lodGroups = GetComponentsInChildren<LODGroup> ();
			foreach (SkinnedMeshRenderer renderer in this._childRenderers)
			{
				renderer.enabled = false;
				this._renderersVisible = false;
			}

			CalculateBoneOffsets();
			CalculateStraightsOffsets();
		}

		void CalculateBoneOffsets()
		{
			for (int i = 0; i < Bones.Count; ++i)
			{
				_offsetBones.Add(Bones[i].rotation);
				_currentRelativeBoneRotation.Add(Bones[i].rotation);
				if(i!=0)
					_distanceBones.Add((transform.TransformPoint(Bones[i].position) - transform.TransformPoint(Bones[i-1].position)).sqrMagnitude);
			}
		}

		void CalculateStraightsOffsets()
		{
			for (int i = 0; i < Straights.Count; ++i)
			{
				_distanceStraights.Add(transform.TransformPoint(Straights[i].position) - transform.TransformPoint(Bones[i].position));
				_offsetStraights.Add (Straights [i].rotation);
				_offsetLocalStraights.Add (Straights [i].localRotation);
			}
		}

		private void Update()
		{
			if (Spline == null) return;

			float prevAdjustedSpeed = AdjustedSpeed;
			AdjustedSpeed = Spline.adjustSpeed (Progress, Speed);
			if (AdjustedSpeed != Speed)
				_adjustingSpeed = true;
			else
				_adjustingSpeed = false;
			if (_adjustingSpeed == false)
			{
				AdjustedSpeed = prevAdjustedSpeed;
				Speed = prevAdjustedSpeed;
			}
			
			_deltaMove = AdjustedSpeed * (float)Direction;
			Progress += Time.deltaTime * _deltaMove * 0.01f;

			if (Progress >= 1.0f) Progress -= 1.0f;
			if (Progress < 0.0f) Progress = 1.0f;
			UpdateProgress();       
		}

		void UpdateProgress()
		{
			bool visible = true;

			float boneProgress = Progress + (Mathf.Sqrt(_distanceBones[0]) / Spline.Length);
			float boneToProgress;
			Vector3? position = Spline.GetPoint(boneProgress);
			Bones[0].position = (Vector3)position;
			Bones[0].LookAt(Spline.GetPoint(ForwardPoint(boneProgress)));
			_currentRelativeBoneRotation[0] = Bones [0].rotation;
			Bones[0].rotation = _offsetBones [0] * Bones [0].rotation;

			for (int i = 1; i < Bones.Count; i++)
			{
				position = Spline.GetPointFromStraightDistance (_distanceBones [i - 1], boneProgress, out boneToProgress);
				if (position == null)
				{
					visible = false;
					break;
				}
				Bones [i].position = (Vector3)position;
				Bones [i].LookAt (Spline.GetPoint (ForwardPoint (boneToProgress)));
				_currentRelativeBoneRotation [i] = Bones [i].rotation;
				Bones [i].rotation = _offsetBones [i] * Bones [i].rotation;

				Vector3 direction = (Bones [i - 1].position - (Vector3)position);
				Quaternion rotation = Quaternion.LookRotation (direction);
				if (!hingedBones)
					Straights [i - 1].position = Bones [i - 1].position + (rotation * _distanceStraights [i - 1]);
				else
					Straights [i - 1].position = Bones [i - 1].position + (_currentRelativeBoneRotation [i - 1] * _distanceStraights [i - 1]);
				rotation *= _offsetStraights [i - 1];
				Straights [i - 1].rotation = rotation;

				boneProgress = boneToProgress;
			}

			if (visible != this._renderersVisible)
			{
				foreach (SkinnedMeshRenderer renderer in this._childRenderers)
					renderer.enabled = visible;
				this._renderersVisible = visible;
			}

			foreach(LODGroup lodGroup in _lodGroups)
				lodGroup.RecalculateBounds();
		}

		float ForwardPoint(float t)
		{
			return t + 0.0001f * (int)Direction;
		}      
	}
}