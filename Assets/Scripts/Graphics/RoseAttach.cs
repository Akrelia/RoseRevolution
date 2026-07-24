using UnityEngine;

namespace UnityRose
{
    public enum RoseAttachPoint
    {
        RightHand = 0,
        LeftHand = 1,
        LeftShield = 2,
        Back = 3,
        Feet = 4,
        Face = 5,
        Head = 6
    }

    public class RoseAttach : MonoBehaviour
    {
        public RoseAttachPoint dummy;
    }
}