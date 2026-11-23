using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace MoreMountains.Feedbacks
{
	#if MM_PHYSICS2D
	/// <summary>
	/// This feedback applies an explosion force to a set of 2D rigidbodies, or to all 2D rigidbodies found within a radius.
	/// Unity has no AddExplosionForce for 2D physics, so this feedback mimics the 3D one : the force pushes rigidbodies away from the center, and fades out with distance
	/// </summary>
	[AddComponentMenu("")]
	[FeedbackHelp("This feedback applies an explosion force to 2D rigidbodies, pushing them away from the explosion center, like Rigidbody.AddExplosionForce does in 3D.")]
	[MovedFrom(false, null, "MoreMountains.Feedbacks")]
	[System.Serializable]
	[FeedbackPath("Physics/Explosion Force 2D")]
	public partial class MMF_ExplosionForce2D : MMF_Feedback
	{
		/// a static bool used to disable all feedbacks of this type at once
		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		public static bool FeedbackTypeAuthorized = true;
		/// sets the inspector color for this feedback
		#if UNITY_EDITOR
		public override Color FeedbackColor { get { return MMFeedbacksInspectorColors.GameObjectColor; } }
		public override bool EvaluateRequiresSetup() { return (TargetMode == TargetModes.Rigidbodies) && (TargetRigidbody2D == null) && ((ExtraTargetRigidbodies2D == null) || (ExtraTargetRigidbodies2D.Count == 0)); }
		public override string RequiredTargetText { get { return (TargetMode == TargetModes.Overlap) ? "Overlap " + Radius : (TargetRigidbody2D != null ? TargetRigidbody2D.name : ""); } }
		public override string RequiresSetupText { get { return "In Rigidbodies mode, this feedback requires that a TargetRigidbody2D (or ExtraTargetRigidbodies2D) be set to be able to work properly. You can set one below, or switch to Overlap mode."; } }
		#endif
		public override bool HasRandomness => true;
		public override bool HasAutomatedTargetAcquisition => true;
		protected override void AutomateTargetAcquisition() => TargetRigidbody2D = FindAutomatedTarget<Rigidbody2D>();
		public override bool HasAutomatedMultipleTargetAcquisition => true;
		protected override void AutomateMultipleTargetAcquisition() => ExtraTargetRigidbodies2D = FindAutomatedTargets<Rigidbody2D>(ExtraTargetRigidbodies2D, TargetRigidbody2D);

		/// the possible ways to pick the rigidbodies affected by the explosion
		/// - Rigidbodies : the TargetRigidbody2D and ExtraTargetRigidbodies2D
		/// - Overlap : every 2D rigidbody with a collider within Radius of the explosion center, on the selected layers
		public enum TargetModes { Rigidbodies, Overlap }

		[MMFInspectorGroup("Targets", true, 61, true)]
		/// whether to apply the explosion to a set list of rigidbodies, or to every rigidbody found within Radius
		[Tooltip("whether to apply the explosion to a set list of rigidbodies (Rigidbodies), or to every 2D rigidbody with a collider found within Radius of the explosion center (Overlap)")]
		public TargetModes TargetMode = TargetModes.Rigidbodies;
		/// in Rigidbodies mode, the rigidbody to apply the explosion force to
		[Tooltip("in Rigidbodies mode, the rigidbody to apply the explosion force to")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Rigidbodies)]
		public Rigidbody2D TargetRigidbody2D;
		/// in Rigidbodies mode, a list of extra rigidbodies to apply the explosion force to
		[Tooltip("in Rigidbodies mode, a list of extra rigidbodies to apply the explosion force to")]
		public List<Rigidbody2D> ExtraTargetRigidbodies2D;
		/// in Overlap mode, the layers on which to look for rigidbodies
		[Tooltip("in Overlap mode, the layers on which to look for rigidbodies")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Overlap)]
		public LayerMask OverlapLayerMask = ~0;
		/// in Overlap mode, whether or not trigger colliders should be detected
		[Tooltip("in Overlap mode, whether or not trigger colliders should be detected")]
		[MMFEnumCondition("TargetMode", (int)TargetModes.Overlap)]
		public bool OverlapTriggers = false;

		[MMFInspectorGroup("Explosion", true, 62)]
		/// the transform at the center of the explosion. If left empty, the position passed to the feedback's Play method will be used
		[Tooltip("the transform at the center of the explosion. If left empty, the position passed to the feedback's Play method will be used")]
		public Transform ExplosionCenter;
		/// an offset to apply to the explosion center, in world space
		[Tooltip("an offset to apply to the explosion center, in world space")]
		public Vector2 ExplosionCenterOffset = Vector2.zero;
		/// the minimum force of the explosion, the actual force will be randomized between min and max
		[Tooltip("the minimum force of the explosion, the actual force will be randomized between min and max")]
		public float MinForce = 10f;
		/// the maximum force of the explosion, the actual force will be randomized between min and max
		[Tooltip("the maximum force of the explosion, the actual force will be randomized between min and max")]
		public float MaxForce = 10f;
		/// the radius of the explosion. Rigidbodies further than that from the center aren't affected. If 0, the full force is applied regardless of distance (Rigidbodies mode only)
		[Tooltip("the radius of the explosion. Rigidbodies further than that from the center aren't affected. If 0, the full force is applied regardless of distance (Rigidbodies mode only)")]
		public float Radius = 5f;
		/// the curve used to scale the force based on distance : x is the distance to the center divided by Radius (0 at the center, 1 at Radius), y is the force multiplier
		[Tooltip("the curve used to scale the force based on distance : x is the distance to the center divided by Radius (0 at the center, 1 at Radius), y is the force multiplier")]
		public AnimationCurve Falloff = AnimationCurve.Linear(0f, 1f, 1f, 0f);
		/// adjusts the apparent position of the explosion to make it seem to lift objects : the force direction is computed as if the explosion was this many units below its actual position
		[Tooltip("adjusts the apparent position of the explosion to make it seem to lift objects : the force direction is computed as if the explosion was this many units below its actual position")]
		public float UpwardsModifier = 0f;
		/// the force mode to use to apply the explosion
		[Tooltip("the force mode to use to apply the explosion")]
		public ForceMode2D AppliedForceMode = ForceMode2D.Impulse;
		/// the minimum torque to apply to each affected rigidbody, scaled by the falloff. The actual torque will be randomized between min and max
		[Tooltip("the minimum torque to apply to each affected rigidbody, scaled by the falloff. The actual torque will be randomized between min and max")]
		public float MinTorque = 0f;
		/// the maximum torque to apply to each affected rigidbody, scaled by the falloff. The actual torque will be randomized between min and max
		[Tooltip("the maximum torque to apply to each affected rigidbody, scaled by the falloff. The actual torque will be randomized between min and max")]
		public float MaxTorque = 0f;
		/// if this is true, the velocity of each affected rigidbody will be reset before applying the explosion
		[Tooltip("if this is true, the velocity of each affected rigidbody will be reset before applying the explosion")]
		public bool ResetVelocityOnPlay = false;
		/// if this is true, the angular velocity of each affected rigidbody will be reset before applying the explosion
		[Tooltip("if this is true, the angular velocity of each affected rigidbody will be reset before applying the explosion")]
		public bool ResetAngularVelocityOnPlay = false;
		/// if this is true, kinematic and static rigidbodies will be ignored
		[Tooltip("if this is true, kinematic and static rigidbodies will be ignored")]
		public bool IgnoreKinematic = true;

		[MMFInspectorGroup("Gizmos", true, 63)]
		/// whether or not to draw the explosion radius when the MMF Player is selected
		[Tooltip("whether or not to draw the explosion radius when the MMF Player is selected")]
		public bool DrawGizmos = true;
		/// the color of the explosion radius gizmo
		[Tooltip("the color of the explosion radius gizmo")]
		public Color GizmosColor = new Color(1f, 0.4f, 0f, 1f);

		protected List<Collider2D> _overlapResults = new List<Collider2D>();
		protected HashSet<Rigidbody2D> _affectedRigidbodies = new HashSet<Rigidbody2D>();
		protected ContactFilter2D _contactFilter;
		protected float _force;
		protected float _intensity;
		protected Vector2 _explosionPosition;

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
			_intensity = ComputeIntensity(feedbacksIntensity, position);
			_force = Random.Range(MinForce, MaxForce) * _intensity;
			_affectedRigidbodies.Clear();

			if (TargetMode == TargetModes.Rigidbodies)
			{
				ApplyExplosion(TargetRigidbody2D);
				if (ExtraTargetRigidbodies2D != null)
				{
					foreach (Rigidbody2D rb in ExtraTargetRigidbodies2D)
					{
						ApplyExplosion(rb);
					}
				}
			}
			else
			{
				_contactFilter = new ContactFilter2D();
				_contactFilter.SetLayerMask(OverlapLayerMask);
				_contactFilter.useTriggers = OverlapTriggers;
				_overlapResults.Clear();
				Physics2D.OverlapCircle(_explosionPosition, Radius, _contactFilter, _overlapResults);
				foreach (Collider2D collider in _overlapResults)
				{
					ApplyExplosion(collider.attachedRigidbody);
				}
			}
		}

		/// <summary>
		/// Returns the explosion's center, from the ExplosionCenter transform if set, or the play position otherwise, plus the offset
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		protected virtual Vector2 ComputeExplosionPosition(Vector3 position)
		{
			Vector2 center = (ExplosionCenter != null) ? (Vector2)ExplosionCenter.position : (Vector2)position;
			return center + ExplosionCenterOffset;
		}

		/// <summary>
		/// Applies the explosion force to the specified rigidbody, making sure each rigidbody only gets it once per play
		/// </summary>
		/// <param name="rb"></param>
		protected virtual void ApplyExplosion(Rigidbody2D rb)
		{
			if ((rb == null) || (IgnoreKinematic && (rb.bodyType != RigidbodyType2D.Dynamic)) || !_affectedRigidbodies.Add(rb))
			{
				return;
			}

			Vector2 targetPosition = rb.worldCenterOfMass;
			float distance = Vector2.Distance(targetPosition, _explosionPosition);
			if ((Radius > 0f) && (distance > Radius))
			{
				return;
			}
			float falloff = (Radius > 0f) ? Falloff.Evaluate(distance / Radius) : 1f;

			// like in 3D, the upwards modifier only moves the center used to compute the direction, not the one used for the falloff
			Vector2 direction = targetPosition - (_explosionPosition + Vector2.down * UpwardsModifier);
			direction = (direction.sqrMagnitude > 0.0001f) ? direction.normalized : Vector2.up;

			if (ResetVelocityOnPlay)
			{
				rb.linearVelocity = Vector2.zero;
			}
			if (ResetAngularVelocityOnPlay)
			{
				rb.angularVelocity = 0f;
			}

			rb.AddForce(direction * (_force * falloff), AppliedForceMode);

			if ((MinTorque != 0f) || (MaxTorque != 0f))
			{
				rb.AddTorque(Random.Range(MinTorque, MaxTorque) * falloff * _intensity, AppliedForceMode);
			}
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
			Vector2 center = ComputeExplosionPosition(Owner.transform.position);
			Gizmos.DrawWireSphere(new Vector3(center.x, center.y, Owner.transform.position.z), Radius);
		}
	}
	#endif
}
