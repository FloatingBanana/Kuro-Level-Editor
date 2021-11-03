using System.Text;
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using ImGuiNET;
using ImGuizmoNET;

using SNVector2 = System.Numerics.Vector2;

namespace SceneEditor {
    static class Utils {

        #region Math

            public static void MatrixToArray(Matrix mat, out float[] arr) {
                arr = new[] {
                    mat.M11, mat.M12, mat.M13, mat.M14,
                    mat.M21, mat.M22, mat.M23, mat.M24,
                    mat.M31, mat.M32, mat.M33, mat.M34,
                    mat.M41, mat.M42, mat.M43, mat.M44
                };
            }
            public static void CopyArrayToMatrix(float[] arr, ref Matrix mat) {
                mat.M11 = arr[0]; mat.M21 = arr[4]; mat.M31 = arr[8]; mat.M41 = arr[12];
                mat.M12 = arr[1]; mat.M22 = arr[5]; mat.M32 = arr[9]; mat.M42 = arr[13];
                mat.M13 = arr[2]; mat.M23 = arr[6]; mat.M33 = arr[10]; mat.M43 = arr[14];
                mat.M14 = arr[3]; mat.M24 = arr[7]; mat.M34 = arr[11]; mat.M44 = arr[15];
            }
            public static Matrix CopyArrayToMatrix(float[] arr) {
                Matrix mat = new Matrix();
                CopyArrayToMatrix(arr, ref mat);
                return mat;
            }
        
            public static Vector3 GetEulerAngles(this Quaternion quaternion) {
                quaternion.Deconstruct(out var x, out var y, out var z, out var w);

                double zSqr = z * z;
                double t0 = -2.0 * (zSqr + w * w) + 1.0;
                double t1 = +2.0 * (y * z + x * w);
                double t2 = -2.0 * (y * w - x * z);
                double t3 = +2.0 * (z * w + x * y);
                double t4 = -2.0 * (y * y + zSqr) + 1.0;

                t2 = t2 > 1.0 ? 1.0 : t2;
                t2 = t2 < -1.0 ? -1.0 : t2;

                return new Vector3(
                    (float)Math.Asin(t2), // Pitch
                    (float)Math.Atan2(t1, t0), // Yaw
                    (float)Math.Atan2(t3, t4) // Roll
                );
            }

            public static float WrapDegrees(float deg) {
                return deg >= 0 ? deg % 360.0f : 360.0f - (-deg % 360.0f);
            }
            public static Vector3 WrapDegrees(Vector3 euler) {
                return new Vector3(
                    WrapDegrees(euler.X),
                    WrapDegrees(euler.Y),
                    WrapDegrees(euler.Z)
                );
            }
        
            // Ray intersection against a triangle
            public static void Intersects(this Ray ray, ref Vector3 vertex1, ref Vector3 vertex2, ref Vector3 vertex3, out float? result) {
                Vector3 edge1, edge2;

                Vector3.Subtract(ref vertex2, ref vertex1, out edge1);
                Vector3.Subtract(ref vertex3, ref vertex1, out edge2);

                Vector3 directionCrossEdge2;
                Vector3.Cross(ref ray.Direction, ref edge2, out directionCrossEdge2);

                float determinant;
                Vector3.Dot(ref edge1, ref directionCrossEdge2, out determinant);

                if (determinant > -float.Epsilon && determinant < float.Epsilon) {
                    result = null;
                    return;
                }

                float inverseDeterminant = 1.0f / determinant;

                Vector3 distanceVector;
                Vector3.Subtract(ref ray.Position, ref vertex1, out distanceVector);

                float triangleU;
                Vector3.Dot(ref distanceVector, ref directionCrossEdge2, out triangleU);
                triangleU *= inverseDeterminant;

                if (triangleU < 0 || triangleU > 1) {
                    result = null;
                    return;
                }

                Vector3 distanceCrossEdge1;
                Vector3.Cross(ref distanceVector, ref edge1, out distanceCrossEdge1);

                float triangleV;
                Vector3.Dot(ref ray.Direction, ref distanceCrossEdge1, out triangleV);
                triangleV *= inverseDeterminant;

                if (triangleV < 0 || triangleU + triangleV > 1) {
                    result = null;
                    return;
                }

                float rayDistance;
                Vector3.Dot(ref edge2, ref distanceCrossEdge1, out rayDistance);
                rayDistance *= inverseDeterminant;

                if (rayDistance < 0) {
                    result = null;
                    return;
                }

                result = rayDistance;
            }

        #endregion

        public static Tuple<Vector3, Vector3, Vector3>[] GetTriangles(this ModelMesh mesh) {
            mesh.GetVerticesAndIndices(out var vertices, out var indices);
            var triangles = new Tuple<Vector3, Vector3, Vector3>[indices.Count/3];

            int index = 0;
            for (int i=0; i < indices.Count; i+=3) {
                triangles[index++] = new (
                    vertices[indices[i]],
                    vertices[indices[i+1]],
                    vertices[indices[i+2]]
                );
            }

            return triangles;
        }

        public static void GetVerticesAndIndices(this ModelMesh mesh, out List<Vector3> vertices, out List<int> indices) {
            vertices = new List<Vector3>();
            indices = new List<int>();

            foreach (var meshPart in mesh.MeshParts) {
                int startIndex = vertices.Count;
                var meshPartVertices = new Vector3[meshPart.NumVertices];

                //Grab position data from the mesh part.
                int stride = meshPart.VertexBuffer.VertexDeclaration.VertexStride;
                meshPart.VertexBuffer.GetData(
                        meshPart.VertexOffset * stride,
                        meshPartVertices,
                        0,
                        meshPart.NumVertices,
                        stride);

                vertices.AddRange(meshPartVertices);

                if (meshPart.IndexBuffer.IndexElementSize == IndexElementSize.ThirtyTwoBits) {
                    var meshIndices = new int[meshPart.PrimitiveCount * 3];
                    meshPart.IndexBuffer.GetData(meshPart.StartIndex * 4, meshIndices, 0, meshPart.PrimitiveCount * 3);
                    
                    for (int k = 0; k < meshIndices.Length; k++) {
                        indices.Add(startIndex + meshIndices[k]);
                    }
                }
                else {
                    var meshIndices = new ushort[meshPart.PrimitiveCount * 3];
                    meshPart.IndexBuffer.GetData(meshPart.StartIndex * 2, meshIndices, 0, meshPart.PrimitiveCount * 3);
                    
                    for (int k = 0; k < meshIndices.Length; k++) {
                        indices.Add(startIndex + meshIndices[k]);
                    }
                }
            }
        }
    

        public static void RenderTo(this RenderTarget2D rt, Action func) {
            var gd = rt.GraphicsDevice;
            var _defaultRenderTargets = gd.GetRenderTargets();
			
			gd.SetRenderTarget(rt);
            func();
            gd.SetRenderTargets(_defaultRenderTargets);
        }
    
        public static string Stylize(this string str) {
            var sb = new StringBuilder(str.Length);

            for (int i = 0; i < str.Length; i++) {
                char c = str[i];

                if (i == 0) {
                    sb.Append(char.ToUpper(c));
                }
                else if (char.IsUpper(c)) {
                    sb.Append(" ").Append(char.ToLower(c));
                }
                else {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }
    }


    static class ImGuiUtils {
        public static bool Thumbnail(string label, IntPtr image, SNVector2 size, bool selected) {
            ImGui.BeginGroup();
            ImGui.PushStyleColor(ImGuiCol.Button, selected ? ImGui.GetColorU32(ImGuiCol.ButtonActive) : 0x00000000);
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(selected ? ImGuiCol.ButtonActive : ImGuiCol.ButtonHovered));

            bool clicked = ImGui.ImageButton(image, new SNVector2(size.X));

            ImGui.PopStyleColor(2);
            ImGui.PushTextWrapPos(ImGui.GetCursorPos().X + size.X);
            ImGui.TextWrapped(label);
            ImGui.PopTextWrapPos();

            ImGui.EndGroup();
            return clicked;
        }

        public static bool Thumbnail(string label, IntPtr image, SNVector2 size, ref bool selected) {
            bool clicked = Thumbnail(label, image, size, selected);

            selected = clicked ? selected : !selected;
            return clicked;
        }

        public static bool ButtonDisabled(string label, SNVector2 size) {
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.GetColorU32(ImGuiCol.Button));
			ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.GetColorU32(ImGuiCol.Button));
			ImGui.PushStyleColor(ImGuiCol.Text, ImGui.GetColorU32(ImGuiCol.TextDisabled));

			bool clicked = ImGui.Button(label, size);

			ImGui.PopStyleColor(3);
            return clicked;
        }
    
        public static void TextCentered(string text, float area) {
			float textSize = ImGui.CalcTextSize(text).X;

			ImGui.SameLine((area - textSize) / 2f);
			ImGui.TextWrapped(text);
        }

        public static void TextCentered(string text) {
            TextCentered(text, ImGui.GetContentRegionAvail().X);
        }
    }
}