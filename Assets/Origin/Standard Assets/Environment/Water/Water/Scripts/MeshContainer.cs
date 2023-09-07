using System;
using UnityEngine;

namespace UnityStandardAssets.Water
{
    public class MeshContainer
    {
        public Mesh Mesh;
        public Vector3[] Vertices;
        public Vector3[] Normals;


        public MeshContainer(Mesh m)
        {
            Mesh = m;
            Vertices = m.vertices;
            Normals = m.normals;
        }


        public void Update()
        {
            Mesh.vertices = Vertices;
            Mesh.normals = Normals;
        }
    }
}