using UnityEngine;
using System.Collections;
using UnityRose;

public class RoseNpc : RoseCharacter
{
	public RoseNpcData data;

	private void Start()
	{
		UpdateModels(); // Akima : added here to trigger the generate stuff

		GetComponent<Animation>()?.Play(); // Akima : play the first animation by default (the Idle one seems to be by default)
	}

	public void UpdateModels()
	{
		parts.Clear();

		if (data != null)
		{
			for (var i = 0; i < data.parts.Count; ++i)
			{
				parts.Add(data.parts[i]);
			}

			skeleton = data.skeleton;

			UpdateData();

			var animator = gameObject.GetComponent<Animation>();

			if (animator == null)
			{
				animator = gameObject.AddComponent<Animation>();
				animator.wrapMode = WrapMode.Loop;
				animator.Play();
			}

			for (var i = 0; i < data.animations.Count; ++i)
			{
				var anim = data.animations[i];
				if (anim != null)
				{
					animator.AddClip(anim, anim.name);
					if (animator.clip == null)
					{
						animator.clip = anim;
					}
				}
			}
		}
	}
}