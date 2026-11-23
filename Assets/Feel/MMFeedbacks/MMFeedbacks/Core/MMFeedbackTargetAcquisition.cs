using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.Feedbacks
{
	/// <summary>
	/// A class collecting target acquisition settings
	/// </summary>
	[System.Serializable]
	public partial class MMFeedbackTargetAcquisition
	{
		public enum Modes { None, Self, AnyChild, ChildAtIndex, Parent, FirstReferenceHolder, PreviousReferenceHolder, ClosestReferenceHolder, NextReferenceHolder, LastReferenceHolder }
		
		/// the selected mode for target acquisition
		/// None : nothing will happen
		/// Self : the target will be picked on the MMF Player's game object
		/// AnyChild : the target will be picked on any of the MMF Player's child objects
		/// ChildAtIndex : the target will be picked on the child at index X of the MMF Player
		/// Parent : the target will be picked on the first parent where a matching target is found
		/// Various reference holders : the target will be picked on the specified reference holder in the list (either the first one, previous : first one found before this feedback in the list, closest in any direction from this feedback, the next one found, or the last one in the list)   
		[Tooltip("the selected mode for target acquisition\n"+
			"None : nothing will happen\n"+
			"Self : the target will be picked on the MMF Player's game object\n"+
			"AnyChild : the target will be picked on any of the MMF Player's child objects\n"+
			"ChildAtIndex : the target will be picked on the child at index X of the MMF Player\n"+
			"Parent : the target will be picked on the first parent where a matching target is found\n"+
			"Various reference holders : the target will be picked on the specified reference holder in the list " +
			"(either the first one, previous : first one found before this feedback in the list, closest in any direction from this feedback, the next one found, or the last one in the list)")]
		public Modes Mode = Modes.None;

		[MMFEnumCondition("Mode", (int)Modes.ChildAtIndex)]
		public int ChildIndex = 0;

		[Unity.Scripting.LifecycleManagement.AutoStaticsCleanup]
		private static MMF_ReferenceHolder _referenceHolder;

		public static MMF_ReferenceHolder GetReferenceHolder(MMFeedbackTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex)
		{
			switch (settings.Mode)
			{
				case Modes.FirstReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.First, currentFeedbackIndex);
				case Modes.PreviousReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Previous, currentFeedbackIndex);
				case Modes.ClosestReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Closest, currentFeedbackIndex);
				case Modes.NextReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Next, currentFeedbackIndex);
				case Modes.LastReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Last, currentFeedbackIndex);
			}
			return null;
		}

		public static GameObject FindAutomatedTargetGameObject(MMFeedbackTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex)
		{
			if (owner.FeedbacksList[currentFeedbackIndex].ForcedReferenceHolder != null)
			{
				return owner.FeedbacksList[currentFeedbackIndex].ForcedReferenceHolder.GameObjectReference;
			}
			
			_referenceHolder = GetReferenceHolder(settings, owner, currentFeedbackIndex);
			switch (settings.Mode)
			{
				case Modes.Self:
					return owner.gameObject;
				case Modes.ChildAtIndex:
					return owner.transform.GetChild(settings.ChildIndex).gameObject;
				case Modes.AnyChild:
					return owner.transform.GetChild(0).gameObject;
				case Modes.Parent:
					return owner.transform.parent.gameObject;
				case Modes.FirstReferenceHolder: 
				case Modes.PreviousReferenceHolder:
				case Modes.ClosestReferenceHolder:
				case Modes.NextReferenceHolder:
				case Modes.LastReferenceHolder:
					return _referenceHolder?.GameObjectReference;
			}
			return null;
		}

		public static T FindAutomatedTarget<T>(MMFeedbackTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex)
		{
			if (owner.FeedbacksList[currentFeedbackIndex].ForcedReferenceHolder != null)
			{
				return owner.FeedbacksList[currentFeedbackIndex].ForcedReferenceHolder.GameObjectReference.GetComponent<T>();
			}
			_referenceHolder = GetReferenceHolder(settings, owner, currentFeedbackIndex);
			switch (settings.Mode)
			{
				case Modes.Self:
					return owner.GetComponent<T>();
				case Modes.ChildAtIndex:
					return owner.transform.GetChild(settings.ChildIndex).gameObject.GetComponent<T>();
				case Modes.AnyChild:
					for (int i = 0; i < owner.transform.childCount; i++) 
					{
						if (owner.transform.GetChild(i).GetComponent<T>() != null)
						{
							return owner.transform.GetChild(i).GetComponent<T>();
						}
					}
					return owner.GetComponentInChildren<T>();
				case Modes.Parent:
					return owner.transform.parent.GetComponentInParent<T>();
				case Modes.FirstReferenceHolder: 
				case Modes.PreviousReferenceHolder:
				case Modes.ClosestReferenceHolder:
				case Modes.NextReferenceHolder:
				case Modes.LastReferenceHolder:
					return (_referenceHolder != null)
						? _referenceHolder.GameObjectReference.GetComponent<T>()
						: default(T);
			}
			return default(T);
		}



	}

	/// <summary>
	/// A class collecting multiple target acquisition settings, used by feedbacks that support extra targets
	/// (for example the extra renderers of a Flicker feedback) to automatically fill their list of extra targets
	/// </summary>
	[System.Serializable]
	public class MMFeedbackMultipleTargetAcquisition
	{
		public enum Modes { None, Self, Children, SelfAndChildren, ChildAtIndex, Parents, AllReferenceHolders, FirstReferenceHolder, PreviousReferenceHolder, ClosestReferenceHolder, NextReferenceHolder, LastReferenceHolder }
		public enum ListModes { Append, Replace }

		/// the selected mode for multiple target acquisition
		/// None : nothing will happen
		/// Self : targets will be picked on the MMF Player's game object
		/// Children : targets will be picked on all of the MMF Player's children (and their own children), but not on the MMF Player's game object itself
		/// SelfAndChildren : targets will be picked on the MMF Player's game object and all its children
		/// ChildAtIndex : targets will be picked on the child at index X of the MMF Player (and optionally its children)
		/// Parents : targets will be picked on all the parents of the MMF Player
		/// AllReferenceHolders : targets will be picked on the game objects of all the reference holders in the MMF Player's list (and optionally their children)
		/// Various reference holders : targets will be picked on the specified reference holder in the list (and optionally its children)
		/// (either the first one, previous : first one found before this feedback in the list, closest in any direction from this feedback, the next one found, or the last one in the list)
		[Tooltip("the selected mode for multiple target acquisition\n"+
			"None : nothing will happen\n"+
			"Self : targets will be picked on the MMF Player's game object\n"+
			"Children : targets will be picked on all of the MMF Player's children (and their own children), but not on the MMF Player's game object itself\n"+
			"SelfAndChildren : targets will be picked on the MMF Player's game object and all its children\n"+
			"ChildAtIndex : targets will be picked on the child at index X of the MMF Player (and optionally its children)\n"+
			"Parents : targets will be picked on all the parents of the MMF Player\n"+
			"AllReferenceHolders : targets will be picked on the game objects of all the reference holders in the MMF Player's list (and optionally their children)\n"+
			"Various reference holders : targets will be picked on the specified reference holder in the list (and optionally its children) " +
			"(either the first one, previous : first one found before this feedback in the list, closest in any direction from this feedback, the next one found, or the last one in the list)")]
		public Modes Mode = Modes.None;

		/// the index of the child of the MMF Player on which to look for targets
		[Tooltip("the index of the child of the MMF Player on which to look for targets")]
		[MMFEnumCondition("Mode", (int)Modes.ChildAtIndex)]
		public int ChildIndex = 0;

		/// whether or not to also look for targets on the children of the selected child or reference holder(s)
		[Tooltip("whether or not to also look for targets on the children of the selected child or reference holder(s)")]
		[MMFEnumCondition("Mode", (int)Modes.ChildAtIndex, (int)Modes.AllReferenceHolders, (int)Modes.FirstReferenceHolder, (int)Modes.PreviousReferenceHolder,
			(int)Modes.ClosestReferenceHolder, (int)Modes.NextReferenceHolder, (int)Modes.LastReferenceHolder)]
		public bool IncludeChildren = true;

		/// whether or not to also pick targets on inactive game objects when looking through children or parents
		[Tooltip("whether or not to also pick targets on inactive game objects when looking through children or parents")]
		public bool IncludeInactive = false;

		/// how found targets should be added to the feedback's list of extra targets
		/// Append : found targets will be added to the list, skipping the ones already in it
		/// Replace : the list will be cleared before found targets get added to it
		[Tooltip("how found targets should be added to the feedback's list of extra targets\n" +
		         "Append : found targets will be added to the list, skipping the ones already in it\n" +
		         "Replace : the list will be cleared before found targets get added to it")]
		public ListModes ListMode = ListModes.Append;

		/// <summary>
		/// Returns the reference holder matching the specified settings, or null if none was found or if the mode isn't a single reference holder one
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="owner"></param>
		/// <param name="currentFeedbackIndex"></param>
		/// <returns></returns>
		public static MMF_ReferenceHolder GetReferenceHolder(MMFeedbackMultipleTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex)
		{
			switch (settings.Mode)
			{
				case Modes.FirstReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.First, currentFeedbackIndex);
				case Modes.PreviousReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Previous, currentFeedbackIndex);
				case Modes.ClosestReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Closest, currentFeedbackIndex);
				case Modes.NextReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Next, currentFeedbackIndex);
				case Modes.LastReferenceHolder:
					return owner.GetFeedbackOfType<MMF_ReferenceHolder>(MMF_Player.AccessMethods.Last, currentFeedbackIndex);
			}
			return null;
		}

		/// <summary>
		/// Looks for all components of type T matching the specified settings, and adds them to the target list (either appending them or replacing its contents, based on the settings' ListMode).
		/// The excluded target (usually the feedback's main target) and duplicates will never be added to the list.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="owner"></param>
		/// <param name="currentFeedbackIndex"></param>
		/// <param name="targetList">the list to fill, if null a new one will be created</param>
		/// <param name="excludedTarget">a target that should never end up in the list, typically the feedback's main target</param>
		/// <typeparam name="T"></typeparam>
		/// <returns>the filled list</returns>
		public static List<T> FindAutomatedTargets<T>(MMFeedbackMultipleTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex, List<T> targetList, T excludedTarget = default(T))
		{
			List<T> foundTargets = new List<T>();
			CollectTargets<T>(settings, owner, currentFeedbackIndex, foundTargets);
			return MergeTargets(settings, foundTargets, targetList, excludedTarget);
		}

		/// <summary>
		/// Looks for all game objects matching the specified settings, and adds them to the target list (either appending them or replacing its contents, based on the settings' ListMode).
		/// The excluded target (usually the feedback's main target) and duplicates will never be added to the list.
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="owner"></param>
		/// <param name="currentFeedbackIndex"></param>
		/// <param name="targetList">the list to fill, if null a new one will be created</param>
		/// <param name="excludedTarget">a target that should never end up in the list, typically the feedback's main target</param>
		/// <returns>the filled list</returns>
		public static List<GameObject> FindAutomatedTargetGameObjects(MMFeedbackMultipleTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex, List<GameObject> targetList, GameObject excludedTarget = null)
		{
			List<Transform> foundTransforms = new List<Transform>();
			CollectTargets<Transform>(settings, owner, currentFeedbackIndex, foundTransforms);
			List<GameObject> foundTargets = new List<GameObject>(foundTransforms.Count);
			foreach (Transform foundTransform in foundTransforms)
			{
				foundTargets.Add(foundTransform.gameObject);
			}
			return MergeTargets(settings, foundTargets, targetList, excludedTarget);
		}

		/// <summary>
		/// Adds found targets to the target list, based on the settings' list mode, skipping nulls, duplicates and the excluded target
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="foundTargets"></param>
		/// <param name="targetList"></param>
		/// <param name="excludedTarget"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		protected static List<T> MergeTargets<T>(MMFeedbackMultipleTargetAcquisition settings, List<T> foundTargets, List<T> targetList, T excludedTarget)
		{
			if (targetList == null)
			{
				targetList = new List<T>();
			}
			if (settings.ListMode == ListModes.Replace)
			{
				targetList.Clear();
			}

			EqualityComparer<T> comparer = EqualityComparer<T>.Default;
			foreach (T foundTarget in foundTargets)
			{
				if ((foundTarget == null) || comparer.Equals(foundTarget, excludedTarget) || targetList.Contains(foundTarget))
				{
					continue;
				}
				targetList.Add(foundTarget);
			}
			return targetList;
		}

		/// <summary>
		/// Collects all components of type T matching the specified settings into the results list
		/// </summary>
		/// <param name="settings"></param>
		/// <param name="owner"></param>
		/// <param name="currentFeedbackIndex"></param>
		/// <param name="results"></param>
		/// <typeparam name="T"></typeparam>
		protected static void CollectTargets<T>(MMFeedbackMultipleTargetAcquisition settings, MMF_Player owner, int currentFeedbackIndex, List<T> results)
		{
			if ((settings == null) || (owner == null) || (settings.Mode == Modes.None))
			{
				return;
			}

			MMF_ReferenceHolder forcedReferenceHolder = (currentFeedbackIndex >= 0) && (currentFeedbackIndex < owner.FeedbacksList.Count)
				? owner.FeedbacksList[currentFeedbackIndex].ForcedReferenceHolder
				: null;
			if (forcedReferenceHolder != null)
			{
				CollectFromGameObject(forcedReferenceHolder.GameObjectReference, settings.IncludeChildren, settings.IncludeInactive, results);
				return;
			}

			switch (settings.Mode)
			{
				case Modes.Self:
					results.AddRange(owner.GetComponents<T>());
					break;
				case Modes.Children:
					T[] selfComponents = owner.GetComponents<T>();
					foreach (T component in owner.GetComponentsInChildren<T>(settings.IncludeInactive))
					{
						if (System.Array.IndexOf(selfComponents, component) < 0)
						{
							results.Add(component);
						}
					}
					break;
				case Modes.SelfAndChildren:
					results.AddRange(owner.GetComponentsInChildren<T>(settings.IncludeInactive));
					break;
				case Modes.ChildAtIndex:
					if ((settings.ChildIndex >= 0) && (settings.ChildIndex < owner.transform.childCount))
					{
						CollectFromGameObject(owner.transform.GetChild(settings.ChildIndex).gameObject, settings.IncludeChildren, settings.IncludeInactive, results);
					}
					break;
				case Modes.Parents:
					if (owner.transform.parent != null)
					{
						results.AddRange(owner.transform.parent.GetComponentsInParent<T>(settings.IncludeInactive));
					}
					break;
				case Modes.AllReferenceHolders:
					foreach (MMF_Feedback feedback in owner.FeedbacksList)
					{
						if (feedback is MMF_ReferenceHolder referenceHolder)
						{
							CollectFromGameObject(referenceHolder.GameObjectReference, settings.IncludeChildren, settings.IncludeInactive, results);
						}
					}
					break;
				case Modes.FirstReferenceHolder:
				case Modes.PreviousReferenceHolder:
				case Modes.ClosestReferenceHolder:
				case Modes.NextReferenceHolder:
				case Modes.LastReferenceHolder:
					MMF_ReferenceHolder holder = GetReferenceHolder(settings, owner, currentFeedbackIndex);
					if (holder != null)
					{
						CollectFromGameObject(holder.GameObjectReference, settings.IncludeChildren, settings.IncludeInactive, results);
					}
					break;
			}
		}

		/// <summary>
		/// Collects all components of type T on the specified game object (and optionally its children) into the results list
		/// </summary>
		/// <param name="target"></param>
		/// <param name="includeChildren"></param>
		/// <param name="includeInactive"></param>
		/// <param name="results"></param>
		/// <typeparam name="T"></typeparam>
		protected static void CollectFromGameObject<T>(GameObject target, bool includeChildren, bool includeInactive, List<T> results)
		{
			if (target == null)
			{
				return;
			}
			results.AddRange(includeChildren ? target.GetComponentsInChildren<T>(includeInactive) : target.GetComponents<T>());
		}
	}
}