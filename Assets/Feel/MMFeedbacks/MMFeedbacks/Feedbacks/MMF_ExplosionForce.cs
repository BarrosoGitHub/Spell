using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// This feedback applies an explosion force (Rigidbody.AddExplosionForce) to a set of rigidbodies, or to all rigidbodies found within a radius
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback applies an explosion force (Rigidbody.AddExplosionForce) to rigidbodies.")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Physics/Explosion Force")]
	public partial class MMF_ExplosionForce : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetMode == TargetModes.Rigidbodies) && (TargetRigidbody == null) && ((ExtraTargetRigidbodies == null) || (ExtraTargetRigidbodies.Count == 0)); }
		public override string RequiredTargetText { get { return (TargetMode == TargetModes.Overlap) ? "Overlap " + Radius : (TargetRigidbody != null ? TargetRigidbody.name : ""); } }
		public override string RequiresSetupText { get { return "In Rigidbodies mode, this feedback requires that a TargetRigidbody (or ExtraTargetRigidbodies) be set to be able to work properly. You can set one below, or switch to Overlap mode."; } }
		public override bool HasCustomInspectors => true; 
		#endif
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetRigidbody = FindAutomatedTarget<Rigidbody>();
		public override bool HasAutomatedMultipleTargetAcquisition => true;
		protected override void AutomateMultipleTargetAcquisition() => ExtraTargetRigidbodies = FindAutomatedTargets<Rigidbody>(ExtraTargetRigidbodies, TargetRigidbody);

		/// the possible ways to pick the rigidbodies affected by the explosion
		/// - Rigidbodies : the TargetRigidbody and ExtraTargetRigidbodies
		/// - Overlap : every rigidbody with a collider within Radius of the explosion center, on the selected layers
		public enum TargetModes { Rigidbodies, Overlap }

		[MMFInspectorGroup("Targets", true, 61, true)]
		/// whether to apply the explosion to a set list of rigidbodies, or to every rigidbody found within Radius
		[Tooltip("whether to apply the explosion to a set list of rigidbodies (Rigidbodies), or to every rigidbody with a collider found within Radius of the explosion center (Overlap)")]
		public TargetModes TargetMode = TargetModes.Rigidbodies;
		/// in Rigidbodies mode, the rigidbody to apply the explosion force to
		[Tooltip("in Rigidbodies mode, the rigidbody to apply the explosion force to")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Rigidbodies)]
		public Rigidbody TargetRigidbody;
		/// in Rigidbodies mode, a list of extra rigidbodies to apply the explosion force to
		[Tooltip("in Rigidbodies mode, a list of extra rigidbodies to apply the explosion force to")]
		public List<Rigidbody> ExtraTargetRigidbodies;
		/// in Overlap mode, the layers on which to look for rigidbodies
		[Tooltip("in Overlap mode, the layers on which to look for rigidbodies")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Overlap)]
		public LayerMask OverlapLayerMask = ~0;
		/// in Overlap mode, whether or not trigger colliders should be detected
		[Tooltip("in Overlap mode, whether or not trigger colliders should be detected")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Overlap)]
		public QueryTriggerInteraction OverlapTriggerInteraction = QueryTriggerInteraction.Ignore;
		/// in Overlap mode, the maximum number of colliders that can be detected in one explosion
		[Tooltip("in Overlap mode, the maximum number of colliders that can be detected in one explosion")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Overlap)]
		public int MaxOverlapColliders = 64;

		[MMFInspectorGroup("Explosion", true, 62)]
		/// the transform at the center of the explosion. If left empty, the position passed to the feedback's Play method will be used
		[Tooltip("the transform at the center of the explosion. If left empty, the position passed to the feedback's Play method will be used")]
		public Transform ExplosionCenter;
		/// an offset to apply to the explosion center, in world space
		[Tooltip("an offset to apply to the explosion center, in world space")]
		public Vector3 ExplosionCenterOffset = Vector3.zero;
		/// the minimum force of the explosion, the actual force will be randomized between min and max
		[Tooltip("the minimum force of the explosion, the actual force will be randomized between min and max")]
		public float MinForce = 10f;
		/// the maximum force of the explosion, the actual force will be randomized between min and max
		[Tooltip("the maximum force of the explosion, the actual force will be randomized between min and max")]
		public float MaxForce = 10f;
		/// the radius of the explosion. The force fades out linearly to zero at that distance. If 0, the full force is applied regardless of distance (Rigidbodies mode only)
		[Tooltip("the radius of the explosion. The force fades out linearly to zero at that distance. If 0, the full force is applied regardless of distance (Rigidbodies mode only)")]
		public float Radius = 5f;
		/// adjusts the apparent position of the explosion to make it seem to lift objects : the force direction is computed as if the explosion was this many units below its actual position
		[Tooltip("adjusts the apparent position of the explosion to make it seem to lift objects : the force direction is computed as if the explosion was this many units below its actual position")]
		public float UpwardsModifier = 0f;
		/// the force mode to use to apply the explosion
		[Tooltip("the force mode to use to apply the explosion")]
		public ForceMode AppliedForceMode = ForceMode.Impulse;
		/// if this is true, the velocity of each affected rigidbody will be reset before applying the explosion
		[Tooltip("if this is true, the velocity of each affected rigidbody will be reset before applying the explosion")]
		public bool ResetVelocityOnPlay = false;
		/// if this is true, the angular velocity of each affected rigidbody will be reset before applying the explosion
		[Tooltip("if this is true, the angular velocity of each affected rigidbody will be reset before applying the explosion")]
		public bool ResetAngularVelocityOnPlay = false;
		/// if this is true, kinematic rigidbodies will be ignored
		[Tooltip("if this is true, kinematic rigidbodies will be ignored")]
		public bool IgnoreKinematic = true;

		[MMFInspectorGroup("Gizmos", true, 63)]
		/// whether or not to draw the explosion radius when the MMF Player is selected
		[Tooltip("whether or not to draw the explosion radius when the MMF Player is selected")]
		public bool DrawGizmos = true;
		/// the color of the explosion radius gizmo
		[Tooltip("the color of the explosion radius gizmo")]
		public Color GizmosColor = new Color(1f, 0.4f, 0f, 1f);

		protected Collider[] _overlapResults;
		protected HashSet<Rigidbody> _affectedRigidbodies = new HashSet<Rigidbody>();
		protected float _force;
		protected Vector3 _explosionPosition;

		/// <summary>
		/// On play, computes the explosion center and force, and applies it to the selected rigidbodies
		/// </summary>
		/// <param name="position"></param>
		/// <param name="feedbacksIntensity"></param>
		protected override void CustomPlayFeedback(Vector3 position, float feedbacksIntensity = 1.0f)
		{
			if (!Active || !FeedbackTypeAuthorized)
			{
				return;
			}

			_explosionPosition = ComputeExplosionPosition(position);
			_force = Random.Range(MinForce, MaxForce) * ComputeIntensity(feedbacksIntensity, position);
			_affectedRigidbodies.Clear();

			if (TargetMode == TargetModes.Rigidbodies)
			{
				ApplyExplosion(TargetRigidbody);
				if (ExtraTargetRigidbodies != null)
				{
					foreach (Rigidbody rb in ExtraTargetRigidbodies)
					{
						ApplyExplosion(rb);
					}
				}
			}
			else
			{
				if ((_overlapResults == null) || (_overlapResults.Length != MaxOverlapColliders))
				{
					_overlapResults = new Collider[Mathf.Max(1, MaxOverlapColliders)];
				}
				int count = Physics.OverlapSphereNonAlloc(_explosionPosition, Radius, _overlapResults, OverlapLayerMask, OverlapTriggerInteraction);
				for (int i = 0; i < count; i++)
				{
					ApplyExplosion(_overlapResults[i].attachedRigidbody);
				}
			}
		}

		/// <summary>
		/// Returns the explosion's center, from the ExplosionCenter transform if set, or the play position otherwise, plus the offset
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		protected virtual Vector3 ComputeExplosionPosition(Vector3 position)
		{
			Vector3 center = (ExplosionCenter != null) ? ExplosionCenter.position : position;
			return center + ExplosionCenterOffset;
		}

		/// <summary>
		/// Applies the explosion force to the specified rigidbody, making sure each rigidbody only gets it once per play
		/// </summary>
		/// <param name="rb"></param>
		protected virtual void ApplyExplosion(Rigidbody rb)
		{
			if ((rb == null) || (IgnoreKinematic && rb.isKinematic) || !_affectedRigidbodies.Add(rb))
			{
				return;
			}

			if (ResetVelocityOnPlay)
			{
				rb.linearVelocity = Vector3.zero;
			}
			if (ResetAngularVelocityOnPlay)
			{
				rb.angularVelocity = Vector3.zero;
			}

			rb.AddExplosionForce(_force, _explosionPosition, Radius, UpwardsModifier, AppliedForceMode);
		}

		/// <summary>
		/// Draws the explosion radius when the MMF Player is selected
		/// </summary>
		public override void OnDrawGizmosSelectedHandler()
		{
			if (!DrawGizmos || (Owner == null) || (Radius <= 0f))
			{
				return;
			}
			Gizmos.color = GizmosColor;
			Gizmos.DrawWireSphere(ComputeExplosionPosition(Owner.transform.position), Radius);
		}
	}
}
