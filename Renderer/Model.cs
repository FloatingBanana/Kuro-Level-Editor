using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Assimp;

using Assimp = Silk.NET.Assimp.Assimp;
using AssScene = Silk.NET.Assimp.Scene;
using AssNode = Silk.NET.Assimp.Node;
using AssMesh = Silk.NET.Assimp.Mesh;
using AssFace = Silk.NET.Assimp.Face;
using AssLight = Silk.NET.Assimp.Light;
using AssCamera = Silk.NET.Assimp.Camera;
using AssTexture = Silk.NET.Assimp.Texture;
using AssMaterial = Silk.NET.Assimp.Material;

namespace Kuro.Renderer {
    public class Model : IDisposable {
        private static readonly Assimp _assimp = Assimp.GetApi();
        private static GL _gl => GraphicsRenderer.gl;

        private string _directory;

        public Dictionary<string, Texture2D> Textures = new();
        public List<Shader> Materials {get; private set;} = new();
        public List<ModelNode> Nodes {get; private set;} = new();
        public ModelNode Root {get; private set;}

        // REVIEW: Maybe using LINQ here isn't a good idea
        public ModelMesh[] Meshes {
            get => (from mesh in Nodes where mesh is ModelMesh select mesh as ModelMesh).ToArray();
        }

        public ModelCamera[] Cameras {
            get => (from camera in Nodes where camera is ModelCamera select camera as ModelCamera).ToArray();
        }

        public ModelLight[] Lights {
            get => (from light in Nodes where light is ModelLight select light as ModelLight).ToArray();
        }

        public Model(string path) {
            LoadModel(path);
        }

        private unsafe void LoadModel(string path) {
            var importFlags =
                PostProcessSteps.Triangulate |
                PostProcessSteps.FlipUVs |
                // PostProcessSteps.GenerateUVCoords |
                PostProcessSteps.GenerateSmoothNormals |
                PostProcessSteps.CalculateTangentSpace;
            
            AssScene* scene = _assimp.ImportFile(path, (uint)importFlags);
            _directory = Path.GetDirectoryName(path);

            if (scene == null || scene->MRootNode == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0) {
                throw new ModelLoadingException($"Failed to load model at {path}: {_assimp.GetErrorStringS()}");
            }

            for (int i = 0; i < scene->MNumMaterials; i++) {
                Materials.Add(ProcessMaterial(scene->MMaterials[i]));
            }
            

            Root = ProcessNode(scene->MRootNode, scene, null);
        }

        private unsafe ModelNode ProcessNode(AssNode* node, AssScene* scene, ModelNode parent) {
            ModelNode modelNode = null;

            // Mesh
            if (node->MNumMeshes > 0) {
                modelNode = ProcessMesh(node, scene, parent);
            }

            // Camera
            for (uint c = 0; c < scene->MNumCameras; c++) {
                if (scene->MCameras[c]->MName == node->MName) {
                    modelNode = ProcessCamera(node, scene->MCameras[c], parent);
                    break;
                }
            }

            // Light
            for (uint l = 0; l < scene->MNumLights; l++) {
                if (scene->MLights[l]->MName == node->MName) {
                    modelNode = ProcessLight(node, scene->MLights[l], parent);
                    break;
                }
            }

            // Defaults to an empty node
            modelNode ??= new ModelNode(node->MName, node->MTransformation, parent);

            for (uint c = 0; c < node->MNumChildren; c++) {
                ModelNode child = ProcessNode(node->MChildren[c], scene, modelNode);
                modelNode.Children.Add(child);
                Nodes.Add(child);
            }

            return modelNode;
        }

        private unsafe ModelMesh ProcessMesh(AssNode* node, AssScene* scene, ModelNode parent) {
            var parts = new MeshPart[node->MNumMeshes];

            for (uint p = 0; p < node->MNumMeshes; p++) {
                AssMesh* part = scene->MMeshes[node->MMeshes[p]];
                parts[p] = ProcessMeshPart(part);
            }

            var transform = Matrix4x4.Transpose(node->MTransformation);
            return new ModelMesh(node->MName, transform, parent, parts);
        }

        private unsafe MeshPart ProcessMeshPart(AssMesh* part) {
            var vertices = new Vertex[part->MNumVertices];
            var indices = new List<uint>((int)part->MNumFaces * 3);

            for (uint v = 0; v < part->MNumVertices; v++) {
                vertices[v] = new Vertex {
                    Position = part->MVertices[v],
                    Normal = part->MNormals[v],
                };

                if (part->MTextureCoords[0] != null) {
                    Vector3 texc = part->MTextureCoords[0][v];
                    vertices[v].TexCoords = new Vector2(texc.X, texc.Y);
                }
            }

            for (uint f = 0; f < part->MNumFaces; f++) {
                AssFace face = part->MFaces[f];

                for (uint i = 0; i < face.MNumIndices; i++)
                    indices.Add(face.MIndices[i]);
            }

            Shader shader = Materials[(int)part->MMaterialIndex];
            return new MeshPart(vertices, indices.ToArray(), shader);
        }

        private static unsafe ModelCamera ProcessCamera(AssNode* node, AssCamera* camera, ModelNode parent) {
            return new ModelCamera(
                node->MName,
                node->MTransformation,
                camera->MPosition,
                camera->MLookAt,
                camera->MUp,
                camera->MHorizontalFOV,
                camera->MClipPlaneNear,
                camera->MClipPlaneFar,
                camera->MAspect,
                parent
            );
        }

        private static unsafe ModelLight  ProcessLight(AssNode* node, AssLight* light, ModelNode parent) {
            ModelLight modelLight = null;

            switch (light->MType) {
            case LightSourceType.LightSourceDirectional:
                modelLight = new ModelDirectionalLight(light->MName, node->MTransformation, light->MDirection, parent);
                break;
                
            case LightSourceType.LightSourcePoint:
                modelLight = new ModelPointLight(light->MName, node->MTransformation, light->MPosition, light->MAttenuationConstant, light->MAttenuationLinear, light->MAttenuationQuadratic, parent);
                break;
            
            case LightSourceType.LightSourceSpot:
                modelLight = new ModelSpotLight(light->MName, node->MTransformation, light->MPosition, light->MDirection, light->MAngleInnerCone, light->MAngleOuterCone, parent);
                break;
            
            default:
                Console.WriteLine($"Unsupported light source '{light->MName}' of type '{light->MType}'");
                return null;
            }

            modelLight.AmbientColor = light->MColorAmbient;
            modelLight.DiffuseColor = light->MColorDiffuse;
            modelLight.SpecularColor = light->MColorSpecular;

            return modelLight;
        }

        private unsafe Shader ProcessMaterial(AssMaterial* material) {
            var shader = new BasicDiffuseShader();

            shader.Use();
            if (_assimp.GetMaterialTextureCount(material, TextureType.TextureTypeDiffuse) > 0) {
                AssimpString path;
                TextureMapMode mapMode;
                _assimp.GetMaterialTexture(material, TextureType.TextureTypeDiffuse, 0, &path, null, null, null, null, &mapMode, null);

                if (!Textures.TryGetValue(path, out var texture)) {
                    string texPath = Path.Combine(_directory, path);

                    texture = new Texture2D(texPath);
                    Textures[texPath] = texture;

                    texture.WrapMode = mapMode switch {
                        TextureMapMode.TextureMapModeClamp => TextureWrap.Clamp,
                        TextureMapMode.TextureMapModeDecal => TextureWrap.ClampZero,
                        TextureMapMode.TextureMapModeWrap => TextureWrap.Repeat,
                        TextureMapMode.TextureMapModeMirror => TextureWrap.MirroredRepeat,
                        _ => TextureWrap.Clamp
                    };
                }

                shader.Texture = texture;
            }

            Vector4 color;
            switch (_assimp.GetMaterialColor(material, Assimp.MaterialColorDiffuseBase, 0, 0, &color)) {
            case Return.ReturnSuccess:
                shader.Color = color;
                break;
            
            case Return.ReturnFailure:
                shader.Color = Vector4.One;
                break;
            }

            return shader;
        }

        public void Dispose() {
            foreach (var mesh in Meshes) {
                mesh.Dispose();
            }
        }
    }

    public class ModelNode {
        public string Name {get; protected set;}
        public ModelNode Parent {get; protected set;}
        public Matrix4x4 Transform {get; protected set;} = Matrix4x4.Identity;
        public List<ModelNode> Children {get; protected set;} = new();

        public Matrix4x4 GlobalTransform {
            get => (Parent?.GlobalTransform ?? Matrix4x4.Identity) * Transform;
            // TODO Add setter
        }

        public Matrix4x4 ModelTransform {
            get {
                var parentTransform = Parent.Parent == null ? Matrix4x4.Identity : Parent.ModelTransform;
                return parentTransform * Transform;
            }
        }

        public ModelNode(string name, Matrix4x4 transform, ModelNode parent) {
            Name = name;
            Transform = transform;
            Parent = parent;
        }
    }

    public class ModelCamera : ModelNode {
        public Vector3 Position {get; private set;}
        public Vector3 Target {get; private set;}
        public Vector3 Up {get; private set;}

        public float Far {get; private set;}
        public float Near {get; private set;}
        public float Fov {get; private set;}
        public float AspectRatio {get; private set;}

        // TODO: Refactor this constructor
        public ModelCamera(string name, Matrix4x4 transform, Vector3 position, Vector3 target, Vector3 up, float fov, float far, float near, float aspectRatio, ModelNode parent) : base(name, transform, parent) {
            Position = position;
            Target = target;
            Up = up;
            Far = far;
            Near = near;
            Fov = fov;
            AspectRatio = aspectRatio;
        }
    }

    [Serializable]
    public class ModelLoadingException : Exception {
        public ModelLoadingException() { }
        public ModelLoadingException(string message) : base(message) { }
        public ModelLoadingException(string message, Exception inner) : base(message, inner) { }
        protected ModelLoadingException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}