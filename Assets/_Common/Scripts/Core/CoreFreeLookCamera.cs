using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Cameras;

namespace Core
{
	public class CoreFreeLookCamera : FreeLookCamera
	{
		protected override void HandleRotationMovement()
		{
			if(Time.timeScale < float.Epsilon)
				return;

			// Read the user input
			var x = CrossPlatformInputManager.GetAxis("Mouse X") * 32 * Time.deltaTime;
			var y = CrossPlatformInputManager.GetAxis("Mouse Y") * 32 * Time.deltaTime;

			m_LookAngle += x*m_TurnSpeed;

			m_TransformTargetRot = Quaternion.Euler(0f, m_LookAngle, 0f);

			m_TiltAngle -= y*m_TurnSpeed;

			m_PivotTargetRot = Quaternion.Euler(m_TiltAngle, m_PivotEulers.y , m_PivotEulers.z);

			m_Pivot.localRotation = m_PivotTargetRot;
			transform.localRotation = m_TransformTargetRot;
		}
	}
}
