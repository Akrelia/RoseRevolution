using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RoseCharPartData : ScriptableObject
{


    public List<Model> models = new List<Model>();

}

[System.Serializable]
public class Model
{
    public Mesh mesh;
    public Material material;
    public short boneIndex; // -1 skinned, 0 - 1000 character bones, 1000+ dummy bones
}