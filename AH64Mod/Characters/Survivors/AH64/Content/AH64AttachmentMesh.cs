using System.Collections.Generic;
using UnityEngine;

namespace AH64.Survivors
{
    // Small shared mesh vocabulary. Every result has exactly one submesh/material slot.
    internal sealed class AH64AttachmentMesh
    {
        private readonly List<Vector3> vertices = new List<Vector3>();
        private readonly List<int> triangles = new List<int>();

        public void Box(Vector3 center, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            Vector3[] p = {
                center + new Vector3(-h.x,-h.y,-h.z), center + new Vector3(h.x,-h.y,-h.z),
                center + new Vector3(h.x,h.y,-h.z), center + new Vector3(-h.x,h.y,-h.z),
                center + new Vector3(-h.x,-h.y,h.z), center + new Vector3(h.x,-h.y,h.z),
                center + new Vector3(h.x,h.y,h.z), center + new Vector3(-h.x,h.y,h.z) };
            Quad(p[0], p[3], p[2], p[1]); Quad(p[4], p[5], p[6], p[7]);
            Quad(p[0], p[4], p[7], p[3]); Quad(p[1], p[2], p[6], p[5]);
            Quad(p[3], p[7], p[6], p[2]); Quad(p[0], p[1], p[5], p[4]);
        }

        public void Tube(Vector3 center, float radius, float bore, float length)
        {
            const int sides = 12;
            for (int i = 0; i < sides; i++)
            {
                float a = i * Mathf.PI * 2f / sides, b = (i + 1) * Mathf.PI * 2f / sides;
                Vector3 oa = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                Vector3 ob = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f);
                Vector3 front = center + Vector3.forward * length * 0.5f;
                Vector3 back = center - Vector3.forward * length * 0.5f;
                Quad(back + oa * radius, back + ob * radius, front + ob * radius, front + oa * radius);
                Quad(front + oa * bore, front + ob * bore, back + ob * bore, back + oa * bore);
                Quad(front + oa * radius, front + ob * radius, front + ob * bore, front + oa * bore);
                Quad(back + oa * bore, back + ob * bore, back + ob * radius, back + oa * radius);
            }
        }

        public void Hull(float[] stations, float[] radii)
        {
            for (int ring = 1; ring < stations.Length; ring++)
                for (int side = 0; side < 12; side++)
                {
                    float a = side * Mathf.PI / 6f, b = (side + 1) * Mathf.PI / 6f;
                    Vector3 da = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    Vector3 db = new Vector3(Mathf.Cos(b), Mathf.Sin(b), 0f);
                    Quad(da * radii[ring - 1] + Vector3.forward * stations[ring - 1],
                        db * radii[ring - 1] + Vector3.forward * stations[ring - 1],
                        db * radii[ring] + Vector3.forward * stations[ring],
                        da * radii[ring] + Vector3.forward * stations[ring]);
                }
        }

        private void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            int n = vertices.Count;
            vertices.AddRange(new[] { a, b, c, d });
            triangles.AddRange(new[] { n, n + 1, n + 2, n, n + 2, n + 3 });
        }

        public Mesh Finish(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices); mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds();
            return mesh;
        }
    }
}
