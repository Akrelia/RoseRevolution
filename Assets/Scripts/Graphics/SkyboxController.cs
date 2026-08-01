using UnityEngine;

namespace UnityRose
{
    /// <summary>
    /// Skybox controller, faking a skybox by moving the mesh to the camera position.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class SkyboxController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;

        /// <summary>
        /// Start.
        /// </summary>
        public void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }
        }

        /// <summary>
        /// Late update.
        /// </summary>
        public void LateUpdate()
        {
            if (targetCamera == null) return;

            transform.position = targetCamera.transform.position;
        }
    }
}