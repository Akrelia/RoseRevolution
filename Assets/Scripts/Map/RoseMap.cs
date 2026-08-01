using RevolutionShared.Rose.Data;
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
        public MapData data;
        [Header("Colors")]
        public Color dawn = Color.white;
        public Color noon = Color.white;
        public Color sunset = Color.white;
        public Color night = Color.white;
        [Header("Time")]
        public float timeRate = 500;
        public float time = 12F;
        [Header("Skybox")]
        public SkyboxData skyboxData;
        public MeshRenderer skyboxRenderer;

        float lastTick;

        /// <summary>
        /// Start.
        /// </summary>
        private void Start()
        {
            UpdateTimeOfDay();
        }

        /// <summary>
        /// Update.
        /// </summary>
        private void Update()
        {
            if (lastTick + timeRate <= Time.time)
            {
                time += 0.5F;
                time %= 24f;

                UpdateTimeOfDay();

                lastTick = Time.time;
            }
        }

        private float GetSkyboxBlend(float hour)
        {
            hour %= 24f;

            if (hour >= 5f && hour < 9f)
            {
                return Mathf.InverseLerp(5f, 9f, hour);
            }

            if (hour >= 9f && hour < 17f)
            {
                return 1f;
            }

            if (hour >= 17f && hour < 21f)
            {
                return 1f - Mathf.InverseLerp(17f, 21f, hour);
            }

            return 0f;
        }

        private void UpdateTimeOfDay()
        {
            time %= 24f;

            var colors = GetTimeOfDayColors(time);

            skyboxRenderer.sharedMaterial.SetFloat("_Blend", GetSkyboxBlend(time));

            Shader.SetGlobalColor("_GlobalTintColor", colors.background);
            Shader.SetGlobalColor("_CharacterAmbientColor", colors.ambient);
            Shader.SetGlobalColor("_CharacterDiffuseColor", colors.diffuse);
        }

        /// <summary>
        /// Get the color of the time of day based on the hour.
        /// </summary>
        /// <param name="hour">Hour.</param>
        /// <returns>Time of the  day color.</returns>
        private (Color background, Color ambient, Color diffuse) GetTimeOfDayColors(float hour)
        {
            hour %= 24f;

            if (hour >= 5f && hour < 9f)
            {
                var t = Mathf.InverseLerp(5f, 9f, hour);

                return (
                    Color.Lerp(skyboxData.BackgroundColor1, skyboxData.BackgroundColor2, t),
                    Color.Lerp(skyboxData.AmbientCharacter1, skyboxData.AmbientCharacter2, t),
                    Color.Lerp(skyboxData.DiffuseCharacter1, skyboxData.DiffuseCharacter2, t)
                );
            }

            if (hour >= 9f && hour < 17f)
            {
                var t = Mathf.InverseLerp(9f, 17f, hour);

                return (
                    Color.Lerp(skyboxData.BackgroundColor2, skyboxData.BackgroundColor3, t),
                    Color.Lerp(skyboxData.AmbientCharacter2, skyboxData.AmbientCharacter3, t),
                    Color.Lerp(skyboxData.DiffuseCharacter2, skyboxData.DiffuseCharacter3, t)
                );
            }

            if (hour >= 17f && hour < 21f)
            {
                var t = Mathf.InverseLerp(17f, 21f, hour);

                return (
                    Color.Lerp(skyboxData.BackgroundColor3, skyboxData.BackgroundColor4, t),
                    Color.Lerp(skyboxData.AmbientCharacter3, skyboxData.AmbientCharacter4, t),
                    Color.Lerp(skyboxData.DiffuseCharacter3, skyboxData.DiffuseCharacter4, t)
                );
            }

            var nightT = hour < 5f ? Mathf.InverseLerp(21f, 29f, hour + 24f) : Mathf.InverseLerp(21f, 29f, hour);

            return (
                Color.Lerp(skyboxData.BackgroundColor4, skyboxData.BackgroundColor1, nightT),
                Color.Lerp(skyboxData.AmbientCharacter4, skyboxData.AmbientCharacter1, nightT),
                Color.Lerp(skyboxData.DiffuseCharacter4, skyboxData.DiffuseCharacter1, nightT)
            );
        }
    }

}
