using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;
using UnityStandardAssets.Utility;
using Random = UnityEngine.Random;

namespace UnityStandardAssets.Characters.FirstPerson
{
	[RequireComponent(typeof (CharacterController))]
	[RequireComponent(typeof (AudioSource))]
	public class FirstPersonController : MonoBehaviour
	{
		[SerializeField] protected bool m_IsWalking;
		[SerializeField] protected float m_WalkSpeed;
		[SerializeField] protected float m_RunSpeed;
		[SerializeField] [Range(0f, 1f)] protected float m_RunstepLenghten;
		[SerializeField] protected float m_JumpSpeed;
		[SerializeField] protected float m_StickToGroundForce;
		[SerializeField] protected float m_GravityMultiplier;
		[SerializeField] protected bool m_IsFlying;
		[SerializeField] protected float m_FlySpeed;
		[SerializeField] protected float m_FlyHeightSpeedInfluenceFactor;
		[SerializeField] protected MouseLook m_MouseLook;
		[SerializeField] protected bool m_UseFovKick;
		[SerializeField] protected FOVKick m_FovKick = new FOVKick();
		[SerializeField] protected bool m_UseHeadBob;
		[SerializeField] protected CurveControlledBob m_HeadBob = new CurveControlledBob();
		[SerializeField] protected LerpControlledBob m_JumpBob = new LerpControlledBob();
		[SerializeField] protected float m_StepInterval;
		[SerializeField] protected AudioClip[] m_FootstepSounds;    // an array of footstep sounds that will be randomly selected from.
		[SerializeField] protected AudioClip m_JumpSound;           // the sound played when character leaves the ground.
		[SerializeField] protected AudioClip m_LandSound;           // the sound played when character touches back on ground.

		protected Camera m_Camera;
		public GameObject Head;
		public GameObject CameraPoint;
		private bool m_Jump;
		private float m_YRotation;
		private Vector2 m_Input;
		private Vector3 m_MoveDir = Vector3.zero;
		protected float m_Speed;
		private CharacterController m_CharacterController;
		private CollisionFlags m_CollisionFlags;
		private bool m_PreviouslyGrounded;
		private Vector3 m_OriginalCameraPosition;
		private float m_StepCycle;
		private float m_NextStep;
		private bool m_Jumping;
		private AudioSource m_AudioSource;

		// Use this for initialization
		protected void Start()
		{
			m_CharacterController = GetComponent<CharacterController>();
			m_Camera = Camera.main;
			m_OriginalCameraPosition = m_Camera.transform.localPosition;
			m_FovKick.Setup(m_Camera);
			m_HeadBob.Setup(m_Camera, m_StepInterval);
			m_StepCycle = 0f;
			m_NextStep = m_StepCycle/2f;
			m_Jumping = false;
			m_AudioSource = GetComponent<AudioSource>();
			m_MouseLook.Init(transform , m_Camera.transform);
		}


		// Update is called once per frame
		protected void Update()
		{
			RotateView();
			// the jump state needs to read here to make sure it is not missed
			if (!m_Jump)
			{
				m_Jump = CrossPlatformInputManager.GetButtonDown("Jump");
			}
   
			if (!m_PreviouslyGrounded && m_CharacterController.isGrounded)
			{
				StartCoroutine(m_JumpBob.DoBobCycle());
				PlayLandingSound();
				m_MoveDir.y = 0f;
				m_Jumping = false;
			}
			if (!m_CharacterController.isGrounded && !m_Jumping && m_PreviouslyGrounded || m_IsFlying)
			{
				m_MoveDir.y = 0f;
			}

			m_PreviouslyGrounded = m_CharacterController.isGrounded;
		}


		private void PlayLandingSound()
		{
			m_AudioSource.clip = m_LandSound;
			m_AudioSource.Play();
			m_NextStep = m_StepCycle + .5f;
		}

		protected void FixedUpdate()
		{
			float speed = 0.0f;
			GetInput(out speed);
			// always move along the camera forward as it is the direction that it being aimed at
			Vector3 desiredMove = transform.forward*m_Input.y + transform.right*m_Input.x;

			// get a normal for the surface that is being touched to move along it
			RaycastHit hitInfo;
			Physics.SphereCast(transform.position, m_CharacterController.radius, Vector3.down, out hitInfo,
							   m_CharacterController.height/2f, ~0, QueryTriggerInteraction.Ignore);
			desiredMove = Vector3.ProjectOnPlane(desiredMove, hitInfo.normal).normalized;

			m_MoveDir.x = desiredMove.x* speed;
			m_MoveDir.z = desiredMove.z* speed;

			//Debug.Log("x " + m_MoveDir.x + "z: " + m_MoveDir.z);
			m_Speed = (m_MoveDir - new Vector3(0, m_MoveDir.y, 0)).magnitude;

			if (m_IsFlying)
			{
				float height = GetHeight();
				if (height == -1 || height > 10) height = 10;//Mathf.Abs(height - 1) < 0.001f
				m_MoveDir.x *= m_FlySpeed * ((height/100) * m_FlyHeightSpeedInfluenceFactor);
				m_MoveDir.z *= m_FlySpeed * ((height/100) * m_FlyHeightSpeedInfluenceFactor);
			}

			if (m_CharacterController.isGrounded)
			{
				m_MoveDir.y = -m_StickToGroundForce;

				if (m_Jump && !m_IsFlying)
				{
					m_MoveDir.y = m_JumpSpeed;
					PlayJumpSound();
					m_Jump = false;
					m_Jumping = true;
				}
			}
			else
			{
				if(!m_IsFlying) m_MoveDir += Physics.gravity*m_GravityMultiplier*Time.fixedDeltaTime;
				else
				{
					if(CrossPlatformInputManager.GetButton("Jump"))
					{
						// Fly up
						m_MoveDir.y = m_JumpSpeed;						
					}
					else if (CrossPlatformInputManager.GetButton("Crouch"))
					{
						// Fly up
						m_MoveDir.y -= m_JumpSpeed;
					}
					if (!m_IsWalking) m_MoveDir.y *= m_RunSpeed;
				}
			}
			m_CollisionFlags = m_CharacterController.Move(m_MoveDir*Time.fixedDeltaTime);

			ProgressStepCycle(speed);
			UpdateCameraPosition(speed);

			m_MouseLook.UpdateCursorLock();
		}

		float GetHeight()
		{
			RaycastHit hit;
			if(Physics.Raycast(new Ray(transform.position, Vector3.down), out hit))
			{
				return hit.distance;
			}            
			return -1;
		}

		private void PlayJumpSound()
		{
			m_AudioSource.clip = m_JumpSound;
			m_AudioSource.Play();
		}

		private void ProgressStepCycle(float speed)
		{
			if (m_CharacterController.velocity.sqrMagnitude > 0 && (m_Input.x != 0 || m_Input.y != 0))
			{
				m_StepCycle += (m_CharacterController.velocity.magnitude + (speed*(m_IsWalking ? 1f : m_RunstepLenghten)))*
							 Time.fixedDeltaTime;
			}

			if (!(m_StepCycle > m_NextStep))
			{
				return;
			}

			m_NextStep = m_StepCycle + m_StepInterval;

			PlayFootStepAudio();
		}

		private void PlayFootStepAudio()
		{
			if (!m_CharacterController.isGrounded)
			{
				return;
			}
			// pick & play a random footstep sound from the array,
			// excluding sound at index 0
			int n = Random.Range(1, m_FootstepSounds.Length);
			if (m_FootstepSounds.Length > 1)
			{
				m_AudioSource.clip = m_FootstepSounds[n];
				m_AudioSource.PlayOneShot(m_AudioSource.clip);
				// move picked sound to index 0 so it's not picked next time
				m_FootstepSounds[n] = m_FootstepSounds[0];
				m_FootstepSounds[0] = m_AudioSource.clip;
			}
		}

		private void UpdateCameraPosition(float speed)
		{
			Vector3 newCameraPosition;
			if (!m_UseHeadBob)
			{
				return;
			}
			if (m_CharacterController.velocity.magnitude > 0 && m_CharacterController.isGrounded)
			{
				/*m_Camera.*/transform.localPosition =
					m_HeadBob.DoHeadBob(m_CharacterController.velocity.magnitude +
									  (speed*(m_IsWalking ? 1f : m_RunstepLenghten)));
				newCameraPosition = /*m_Camera.*/transform.localPosition;
				newCameraPosition.y = /*m_Camera.*/transform.localPosition.y - m_JumpBob.Offset();
			}
			else
			{
				newCameraPosition = /*m_Camera.*/transform.localPosition;
				newCameraPosition.y = m_OriginalCameraPosition.y - m_JumpBob.Offset();
			}
			/*m_Camera.*/transform.localPosition = newCameraPosition;
		}

		private void GetInput(out float speed)
		{
			// Read input
			float horizontal = CrossPlatformInputManager.GetAxis("Horizontal");
			float vertical = CrossPlatformInputManager.GetAxis("Vertical");
			bool waswalking = m_IsWalking;

#if !MOBILE_INPUT
			// On standalone builds, walk/run speed is modified by a key press.
			// keep track of whether or not the character is walking or running
			m_IsWalking = !Input.GetButton("Run");
			if (Input.GetButtonDown("Fly")) m_IsFlying = !m_IsFlying;
#endif
			// set the desired speed to be walking or running
			m_Input = new Vector2(horizontal, vertical);
			speed = m_IsWalking ? m_WalkSpeed : m_RunSpeed;
			speed *= m_Input.magnitude;

			// normalize input if it exceeds 1 in combined length:
			if (m_Input.sqrMagnitude > 1)
			{
				m_Input.Normalize();
			}

			// handle speed change to give an fov kick
			// only if the player is going to a run, is running and the fovkick is to be used
			if (m_IsWalking != waswalking && m_UseFovKick && m_CharacterController.velocity.sqrMagnitude > 0)
			{
				StopAllCoroutines();
				StartCoroutine(!m_IsWalking ? m_FovKick.FOVKickUp() : m_FovKick.FOVKickDown());
			}
		}

		private void RotateView()
		{
			m_MouseLook.LookRotation (transform, m_Camera.transform.parent.parent);
			if (Head != null) Head.transform.rotation = Quaternion.Euler(-m_MouseLook.CameraTargetRot.eulerAngles.x, m_MouseLook.CharacterTargetRot.eulerAngles.y-180, m_MouseLook.CharacterTargetRot.eulerAngles.z);
			if (CameraPoint != null) m_Camera.transform.position = CameraPoint.transform.position;
		}

		private void OnControllerColliderHit(ControllerColliderHit hit)
		{
			Rigidbody body = hit.collider.attachedRigidbody;
			//dont move the rigidbody if the character is on top of it
			if (m_CollisionFlags == CollisionFlags.Below)
			{
				return;
			}

			if (body == null || body.isKinematic)
			{
				return;
			}
			body.AddForceAtPosition(m_CharacterController.velocity*0.1f, hit.point, ForceMode.Impulse);
		}
	}
}
