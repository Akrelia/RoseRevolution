using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityRose.Game
{
    public class RoseMap : MonoBehaviour
    {
        [Header("Data")]
        public int mapID;
        public string mapName;
        public List<SpawnData> spawns;
        //      public List<RosePatch> patches = new List<RosePatch>();
        [Header("Colors")]
        public Color dawn = Color.white;
        public Color noon = Color.white;
        public Color sunset = Color.white;
        public Color night = Color.white;
        [Header("Time")]
        public float timeRate;
        public float time = 12F;

        float lastTick;

        /// <summary>
        /// Start.
        /// </summary>
        private void Start()
        {
        }

        /// <summary>
        /// Update.
        /// </summary>
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

        /// <summary>
        /// Get the color of the time of day based on the hour.
        /// </summary>
        /// <param name="hour">Hour.</param>
        /// <returns>Time of the  day color.</returns>
        public Color GetTimeOfDayColor(float hour)
        {
            hour = hour % 24f;

            if (hour >= 5f && hour < 9f)
            {
                float t = Mathf.InverseLerp(5f, 9f, hour);
                
                return Color.Lerp(dawn, noon, t);
            }

            else if (hour >= 9f && hour < 17f)
            {
                float t = Mathf.InverseLerp(9f, 17f, hour);
                
                return Color.Lerp(noon, sunset, t);
            }

            else if (hour >= 17f && hour < 21f)
            {
                float t = Mathf.InverseLerp(17f, 21f, hour);
             
                return Color.Lerp(sunset, night, t);
            }

            else
            {
                float t = hour < 5f ? Mathf.InverseLerp(21f, 29f, hour + 24f) : Mathf.InverseLerp(21f, 29f, hour);
                return Color.Lerp(night, dawn, t);
            }
        }
    }

}
