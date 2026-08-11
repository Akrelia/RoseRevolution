using UnityEngine;
using System.Collections;
using System;

namespace UnityRose
{
	[Serializable]
	public class BindPoses : ScriptableObject
	{
		[SerializeField]
		public Matrix4x4[] bindPoses;
		[SerializeField]
		public string[] boneNames;
		[SerializeField]
		public Transform[] boneTransforms;
	}
}