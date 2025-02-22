using System.Collections.Generic;
using MultiCraft.Scripts.Engine.Core.Blocks;
using MultiCraft.Scripts.Engine.Core.Items;
using MultiCraft.Scripts.Engine.Utils;
using UnityEngine;

namespace MultiCraft.Scripts.Engine.Core.MeshBuilders
{
    public static class DropItemMeshBuilder
    {
        private const float Thickness = 1f;

        public static DropItemGeneratedMesh GeneratedMesh(Block block)
        {
            List<GeneratedMeshVertex> vertices = new List<GeneratedMeshVertex>();

            GenerateBlock(vertices, block);

            var mesh = new DropItemGeneratedMesh
            {
                Vertices = vertices.ToArray()
            };

            var boundsSize = new Vector3(1, 1, 1);
            mesh.Bounds = new Bounds(boundsSize / 2, boundsSize);

            return mesh;
        }

        public static DropItemGeneratedMesh GeneratedMesh(Item item)
        {
            List<GeneratedMeshVertex> vertices = new List<GeneratedMeshVertex>();

            GenerateItem(vertices, item);

            var mesh = new DropItemGeneratedMesh
            {
                Vertices = vertices.ToArray()
            };

            var boundsSize = new Vector3(1, 1, 1);
            mesh.Bounds = new Bounds(boundsSize / 2, boundsSize);

            return mesh;
        }

        private static void GenerateItem(List<GeneratedMeshVertex> vertices, Item item)
        {
            var sprite = item.Icon;
            Vector2[] vertices2D = sprite.vertices;
            ushort[] triangles2D = sprite.triangles;

            float halfThickness = Thickness / 2f;

            // Преобразуем вершины спрайта в 3D (передняя и задняя стороны)
            int vertexCount = vertices2D.Length;
            var frontVertices = new Vector3[vertexCount];
            var backVertices = new Vector3[vertexCount];

            for (int i = 0; i < vertexCount; i++)
            {
                var v2 = vertices2D[i];
                frontVertices[i] = new Vector3(v2.x, v2.y, halfThickness); // Передняя сторона
                backVertices[i] = new Vector3(v2.x, v2.y, -halfThickness); // Задняя сторона
            }

            // Добавляем передние вершины в список
            for (int i = 0; i < vertexCount; i++)
            {
                vertices.Add(new GeneratedMeshVertex
                {
                    Position = frontVertices[i],
                    NormalX = 0,
                    NormalY = 0,
                    NormalZ = sbyte.MaxValue,
                    NormalW = 1,
                    Uv = sprite.uv[i]
                });
            }

            // Добавляем задние вершины в список
            for (int i = 0; i < vertexCount; i++)
            {
                vertices.Add(new GeneratedMeshVertex
                {
                    Position = backVertices[i],
                    NormalX = 0,
                    NormalY = 0,
                    NormalZ = sbyte.MinValue,
                    NormalW = 1,
                    Uv = sprite.uv[i]
                });
            }

            // Добавляем боковые стороны
            for (int i = 0; i < vertexCount; i++)
            {
                int nextIndex = (i + 1) % vertexCount;

                // Боковая грань (четырёхугольник, две треугольные грани)
                AddSideFace(vertices, frontVertices[i], frontVertices[nextIndex], backVertices[i],
                    backVertices[nextIndex], sprite.uv[i], sprite.uv[nextIndex]);
            }
        }

        private static void AddSideFace(List<GeneratedMeshVertex> vertices, Vector3 v1, Vector3 v2, Vector3 v3,
            Vector3 v4, Vector2 uv1, Vector2 uv2)
        {
            // Первый треугольник боковой грани
            vertices.Add(new GeneratedMeshVertex
                { Position = v1, NormalX = 0, NormalY = 0, NormalZ = 0, NormalW = 1, Uv = uv1 });
            vertices.Add(new GeneratedMeshVertex
                { Position = v3, NormalX = 0, NormalY = 0, NormalZ = 0, NormalW = 1, Uv = uv1 });
            vertices.Add(new GeneratedMeshVertex
                { Position = v2, NormalX = 0, NormalY = 0, NormalZ = 0, NormalW = 1, Uv = uv2 });

            // Второй треугольник боковой грани
            vertices.Add(new GeneratedMeshVertex
                { Position = v2, NormalX = 0, NormalY = 0, NormalZ = 0, NormalW = 1, Uv = uv2 });
            vertices.Add(new GeneratedMeshVertex
                { Position = v3, NormalX = 0, NormalY = 0, NormalZ = 0, NormalW = 1, Uv = uv1 });
            vertices.Add(new GeneratedMeshVertex
                { Position = v4, NormalX = 0, NormalY = 0, NormalZ = 0, NormalW = 1, Uv = uv2 });
        }

        private static void GenerateBlock(List<GeneratedMeshVertex> vertices, Block block)
        {
            GenerateTopSide(vertices, block);
            GenerateBottomSide(vertices, block);
            GenerateFrontSide(vertices, block);
            GenerateBackSide(vertices, block);
            GenerateRightSide(vertices, block);
            GenerateLeftSide(vertices, block);
        }

        private static void GenerateTopSide(List<GeneratedMeshVertex> vertices, Block block)
        {
            var vertex = new GeneratedMeshVertex
            {
                NormalX = 0,
                NormalY = sbyte.MaxValue,
                NormalZ = 0,
                NormalW = 1
            };

            GetUvs(block, Vector3Int.up, out var uv1, out var uv2, out var uv3, out var uv4);

            vertex.Position = new Vector3(0, 1, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv4;
            vertices.Add(vertex);
        }

        private static void GenerateBottomSide(List<GeneratedMeshVertex> vertices, Block block)
        {
            var vertex = new GeneratedMeshVertex
            {
                Position = default,
                NormalX = 0,
                NormalY = 0,
                NormalZ = 0,
                NormalW = 0
            };
            vertex.NormalX = 0;
            vertex.NormalY = sbyte.MinValue;
            vertex.NormalZ = 0;
            vertex.NormalW = 1;

            GetUvs(block, Vector3Int.down, out var uv1, out var uv2, uv3: out var uv3, out var uv4);

            vertex.Position = new Vector3(0, 0, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 0, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv4;
            vertices.Add(vertex);
        }

        private static void GenerateFrontSide(List<GeneratedMeshVertex> vertices, Block block)
        {
            var vertex = new GeneratedMeshVertex
            {
                Position = default,
                NormalX = 0,
                NormalY = 0,
                NormalZ = 0,
                NormalW = 0
            };
            vertex.NormalX = 0;
            vertex.NormalY = 0;
            vertex.NormalZ = sbyte.MaxValue;
            vertex.NormalW = 1;

            GetUvs(block, Vector3Int.forward, out var uv1, out var uv2, out var uv3, out var uv4);

            vertex.Position = new Vector3(1, 0, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 0, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv4;
            vertices.Add(vertex);
        }

        private static void GenerateBackSide(List<GeneratedMeshVertex> vertices, Block block)
        {
            var vertex = new GeneratedMeshVertex
            {
                NormalX = 0,
                NormalY = 0,
                NormalZ = sbyte.MinValue,
                NormalW = 1
            };

            GetUvs(block, Vector3Int.back, out var uv1, out var uv2, out var uv3, out var uv4);

            vertex.Position = new Vector3(0, 0, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv4;
            vertices.Add(vertex);
        }

        private static void GenerateRightSide(List<GeneratedMeshVertex> vertices, Block block)
        {
            var vertex = new GeneratedMeshVertex
            {
                NormalX = sbyte.MaxValue,
                NormalY = 0,
                NormalZ = 0,
                NormalW = 1
            };

            GetUvs(block, Vector3Int.right, out var uv1, out var uv2, out var uv3, out var uv4);

            vertex.Position = new Vector3(1, 0, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 0, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(1, 1, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv4;
            vertices.Add(vertex);
        }

        private static void GenerateLeftSide(List<GeneratedMeshVertex> vertices, Block block)
        {
            var vertex = new GeneratedMeshVertex
            {
                NormalX = sbyte.MinValue,
                NormalY = 0,
                NormalZ = 0,
                NormalW = 0,
            };

            GetUvs(block, Vector3Int.left, out var uv1, out var uv2, out var uv3, out var uv4);

            vertex.Position = new Vector3(0, 0, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv1;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 1) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv2;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 0, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv3;
            vertices.Add(vertex);
            vertex.Position = new Vector3(0, 1, 0) - new Vector3(0.5f, 0.5f, 0.5f);
            vertex.Uv = uv4;
            vertices.Add(vertex);
        }

        private static void GetUvs(Block block, Vector3Int normal, out Vector2 uv1, out Vector2 uv2,
            out Vector2 uv3, out Vector2 uv4)
        {
            float textureAtlasSize = ResourceLoader.Instance.TextureData.AtlasSize;
            var textureResolution = ResourceLoader.Instance.TextureData.TextureResolution;
            var pixelOffset = block.GetUvsPixelsOffset(normal);

            uv1 = new Vector2(
                (pixelOffset.x * textureResolution) / textureAtlasSize,
                (pixelOffset.y * textureResolution) / textureAtlasSize);
            uv2 = new Vector2(
                (pixelOffset.x * textureResolution) / textureAtlasSize,
                ((pixelOffset.y + 1) * textureResolution) / textureAtlasSize);
            uv3 = new Vector2(
                ((pixelOffset.x + 1) * textureResolution) / textureAtlasSize,
                (pixelOffset.y * textureResolution) / textureAtlasSize);
            uv4 = new Vector2(
                ((pixelOffset.x + 1) * textureResolution) / textureAtlasSize,
                ((pixelOffset.y + 1) * textureResolution) / textureAtlasSize);
        }
    }
}