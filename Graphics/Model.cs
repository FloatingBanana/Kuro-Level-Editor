using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Silk.NET.Assimp;

using SNVector2 = System.Numerics.Vector2;
using SNVector3 = System.Numerics.Vector3;
using SNVector4 = System.Numerics.Vector4;

using Assimp = Silk.NET.Assimp.Assimp;
using AssScene = Silk.NET.Assimp.Scene;
using AssNode = Silk.NET.Assimp.Node;
using AssMesh = Silk.NET.Assimp.Mesh;
using AssFace = Silk.NET.Assimp.Face;
using AssLight = Silk.NET.Assimp.Light;
using AssCamera = Silk.NET.Assimp.Camera;
using AssTexture = Silk.NET.Assimp.Texture;
using AssMaterial = Silk.NET.Assimp.Material;

namespace Kuro.LevelEditor.Graphics {
    public class KuroModel : IDisposable {
        private static readonly Assimp _assimp = Assimp.GetApi();

        private GraphicsDevice _graphicsDevice;
        private string _directory;

        public Dictionary<string, Texture2D> Textures = new();
        public List<Effect> Materials {get; private set;} = new();
        public List<KuroModelNode> Nodes {get; private set;} = new();
        public KuroModelNode Root {get; private set;}

        // REVIEW: Maybe using LINQ here isn't a good idea
        public KuroModelMesh[] Meshes {
            get => (from mesh in Nodes where mesh is KuroModelMesh select mesh as KuroModelMesh).ToArray();
        }

        public KuroModelCamera[] Cameras {
            get => (from camera in Nodes where camera is KuroModelCamera select camera as KuroModelCamera).ToArray();
        }

        public KuroModelLight[] Lights {
            get => (from light in Nodes where light is KuroModelLight select light as KuroModelLight).ToArray();
        }

        public KuroModel(GraphicsDevice gd, string path) {
            _graphicsDevice = gd;
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

        private unsafe KuroModelNode ProcessNode(AssNode* node, AssScene* scene, KuroModelNode parent) {
            KuroModelNode modelNode = null;

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
            modelNode ??= new KuroModelNode(node->MName, Utils.Convert(node->MTransformation), parent);

            for (uint c = 0; c < node->MNumChildren; c++) {
                KuroModelNode child = ProcessNode(node->MChildren[c], scene, modelNode);
                modelNode.Children.Add(child);
                Nodes.Add(child);
            }

            return modelNode;
        }

        private unsafe KuroModelMesh ProcessMesh(AssNode* node, AssScene* scene, KuroModelNode parent) {
            var parts = new KuroMeshPart[node->MNumMeshes];

            for (uint p = 0; p < node->MNumMeshes; p++) {
                AssMesh* part = scene->MMeshes[node->MMeshes[p]];
                parts[p] = ProcessMeshPart(part);
            }

            var transform = Matrix.Transpose(Utils.Convert(node->MTransformation));
            return new KuroModelMesh(node->MName, transform, parent, parts);
        }

        private unsafe KuroMeshPart ProcessMeshPart(AssMesh* part) {
            var vertices = new Vertex[part->MNumVertices];
            var indices = new List<uint>((int)part->MNumFaces * 3);

            for (uint v = 0; v < part->MNumVertices; v++) {
                vertices[v] = new Vertex {
                    Position = Utils.Convert(part->MVertices[v]),
                    Normal = Utils.Convert(part->MNormals[v]),
                };

                if (part->MTextureCoords[0] != null) {
                    SNVector3 texc = part->MTextureCoords[0][v];
                    vertices[v].TexCoords = new Vector2(texc.X, texc.Y);
                }
            }

            for (uint f = 0; f < part->MNumFaces; f++) {
                AssFace face = part->MFaces[f];

                for (uint i = 0; i < face.MNumIndices; i++)
                    indices.Add(face.MIndices[i]);
            }

            Effect shader = Materials[(int)part->MMaterialIndex];
            return new KuroMeshPart(_graphicsDevice, vertices, indices.ToArray(), shader);
        }

        private static unsafe KuroModelCamera ProcessCamera(AssNode* node, AssCamera* camera, KuroModelNode parent) {
            return new KuroModelCamera(
                node->MName,
                Utils.Convert(node->MTransformation),
                Utils.Convert(camera->MPosition),
                Utils.Convert(camera->MLookAt),
                Utils.Convert(camera->MUp),
                camera->MHorizontalFOV,
                camera->MClipPlaneNear,
                camera->MClipPlaneFar,
                camera->MAspect,
                parent
            );
        }

        private static unsafe KuroModelLight  ProcessLight(AssNode* node, AssLight* light, KuroModelNode parent) {
            KuroModelLight modelLight = null;
            Matrix transformation = Utils.Convert(node->MTransformation);
			Vector3 pos = Utils.Convert(light->MPosition);
			Vector3 dir = Utils.Convert(light->MDirection);

            switch (light->MType) {
            case LightSourceType.LightSourceDirectional:
                modelLight = new KuroModelDirectionalLight(light->MName, transformation, dir, parent);
                break;
                
            case LightSourceType.LightSourcePoint:
                modelLight = new KuroModelPointLight(light->MName, transformation, pos, light->MAttenuationConstant, light->MAttenuationLinear, light->MAttenuationQuadratic, parent);
                break;
            
            case LightSourceType.LightSourceSpot:
                modelLight = new KuroModelSpotLight(light->MName, transformation, pos, dir, light->MAngleInnerCone, light->MAngleOuterCone, parent);
                break;
            
            default:
                Console.WriteLine($"Unsupported light source '{light->MName}' of type '{light->MType}'");
                return null;
            }

            modelLight.AmbientColor = Utils.Convert(light->MColorAmbient);
            modelLight.DiffuseColor = Utils.Convert(light->MColorDiffuse);
            modelLight.SpecularColor = Utils.Convert(light->MColorSpecular);

            return modelLight;
        }

        private unsafe Effect ProcessMaterial(AssMaterial* material) {
            var shader = new BasicEffect(_graphicsDevice);

            if (_assimp.GetMaterialTextureCount(material, TextureType.TextureTypeDiffuse) > 0) {
                AssimpString path;
                TextureMapMode mapMode;
                _assimp.GetMaterialTexture(material, TextureType.TextureTypeDiffuse, 0, &path, null, null, null, null, &mapMode, null);

                if (!Textures.TryGetValue(path, out var texture)) {
                    string texPath = Path.Combine(_directory, path);

                    texture = Texture2D.FromFile(_graphicsDevice, texPath);
                    Textures[texPath] = texture;

                    // texture.WrapMode = mapMode switch {
                    //     TextureMapMode.TextureMapModeClamp => TextureWrap.Clamp,
                    //     TextureMapMode.TextureMapModeDecal => TextureWrap.ClampZero,
                    //     TextureMapMode.TextureMapModeWrap => TextureWrap.Repeat,
                    //     TextureMapMode.TextureMapModeMirror => TextureWrap.MirroredRepeat,
                    //     _ => TextureWrap.Clamp
                    // };
                }

                shader.Texture = texture;
            }

            SNVector4 color;
            switch (_assimp.GetMaterialColor(material, Assimp.MaterialColorDiffuseBase, 0, 0, &color)) {
            case Return.ReturnSuccess:
                shader.DiffuseColor = new Vector3(color.X, color.Y, color.Z);
                break;
            
            case Return.ReturnFailure:
                shader.DiffuseColor = Vector3.One;
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

    public class KuroModelNode {
        public string Name {get; protected set;}
        public KuroModelNode Parent {get; protected set;}
        public Matrix Transform {get; protected set;} = Matrix.Identity;
        public List<KuroModelNode> Children {get; protected set;} = new();

        public Matrix GlobalTransform {
            get => (Parent?.GlobalTransform ?? Matrix.Identity) * Transform;
            // TODO Add setter
        }

        public Matrix ModelTransform {
            get {
                var parentTransform = Parent.Parent == null ? Matrix.Identity : Parent.ModelTransform;
                return parentTransform * Transform;
            }
        }

        public KuroModelNode(string name, Matrix transform, KuroModelNode parent) {
            Name = name;
            Transform = transform;
            Parent = parent;
        }
    }

    public class KuroModelCamera : KuroModelNode {
        public Vector3 Position {get; private set;}
        public Vector3 Target {get; private set;}
        public Vector3 Up {get; private set;}

        public float Far {get; private set;}
        public float Near {get; private set;}
        public float Fov {get; private set;}
        public float AspectRatio {get; private set;}

        // TODO: Refactor this constructor
        public KuroModelCamera(string name, Matrix transform, Vector3 position, Vector3 target, Vector3 up, float fov, float far, float near, float aspectRatio, KuroModelNode parent) : base(name, transform, parent) {
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
