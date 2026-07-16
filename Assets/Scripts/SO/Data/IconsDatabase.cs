using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "IconsDatabase", menuName = "GameData/IconsDatabase")]
public class IconsDatabase : ScriptableObject
{
    public List<Texture2D> icons;
}