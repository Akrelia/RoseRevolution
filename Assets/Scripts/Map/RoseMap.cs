using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityRose.Game
{
    public class RoseMap : MonoBehaviour
    {
        [Header("Data")]
        public string mapName;
        public List<RosePatch> patches = new List<RosePatch>();
        public DirectoryInfo assetDir;
        [Header("Colors")]
        public Color dawn = Color.white;
        public Color noon = Color.white;
        public Color sunset = Color.white;
        public Color night = Color.white;
        [Header("Time")]
        public float timeRate;
        public float time = 12F;

        float lastTick;

        private void Start()
        {
        }

        private void Update()
        {
            if (lastTick + timeRate <= Time.time)
            {
                time += 0.5F;

                time %= 24;

                Shader.SetGlobalColor("_GlobalTintColor", GetTimeOfDayColor(time));

                lastTick = Time.time;
            }
        }

        public Color GetTimeOfDayColor(float hour)
        {
            hour = hour % 24f; // Boucle au cas où

            if (hour >= 5f && hour < 9f) // Dawn → Noon
            {
                float t = Mathf.InverseLerp(5f, 9f, hour);
                return Color.Lerp(dawn, noon, t);
            }
            else if (hour >= 9f && hour < 17f) // Noon → Sunset
            {
                float t = Mathf.InverseLerp(9f, 17f, hour);
                return Color.Lerp(noon, sunset, t);
            }
            else if (hour >= 17f && hour < 21f) // Sunset → Night
            {
                float t = Mathf.InverseLerp(17f, 21f, hour);
                return Color.Lerp(sunset, night, t);
            }
            else // Night → Dawn (21h → 5h)
            {
                float t = hour < 5f
                    ? Mathf.InverseLerp(21f, 29f, hour + 24f) // 21h à 5h devient 21 → 29
                    : Mathf.InverseLerp(21f, 29f, hour);
                return Color.Lerp(night, dawn, t);
            }
        }
    }
}
