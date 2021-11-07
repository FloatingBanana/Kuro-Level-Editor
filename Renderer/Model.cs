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

        private string directory;

        public List<ModelNode> Nodes {get; private set;}
        public ModelNode Root {get; private set;}

        public ModelMesh[] Meshes {
            get => Nodes.Where((ModelNode node) => node is ModelMesh).Cast<ModelMesh>().ToArray();
        }

        public ModelCamera[] Cameras {
            get => Nodes.Where((ModelNode node) => node is ModelCamera).Cast<ModelCamera>().ToArray();
        }

        public ModelLight[] Lights {
            get => Nodes.Where((ModelNode node) => node is ModelLight).Cast<ModelLight>().ToArray();
        }

        public Model(string path) {
            _loadModel(path);
        }

        private unsafe void _loadModel(string path) {
            var importFlags =
                PostProcessSteps.Triangulate |
                PostProcessSteps.FlipUVs |
                PostProcessSteps.GenerateSmoothNormals |
                PostProcessSteps.CalculateTangentSpace;
            
            AssScene* scene = _assimp.ImportFile(path, (uint)importFlags);

            if (scene == null || scene->MRootNode == null || (scene->MFlags & (uint)SceneFlags.Incomplete) != 0) {
                throw new ModelLoadingException($"Failed to load model at {path}: {_assimp.GetErrorStringS()}");
            }

            directory = Path.GetDirectoryName(path);
            Root = _processNode(scene->MRootNode, scene, null);
        }

        private static unsafe ModelNode _processNode(AssNode* node, AssScene* scene, ModelNode parent) {
            ModelNode modelNode = null;

            // Mesh
            if (node->MNumMeshes > 0) {
                modelNode = _processMesh(node, scene, parent);
            }

            // Camera
            for (uint c = 0; c < scene->MNumCameras; c++) {
                if (scene->MCameras[c]->MName == node->MName) {
                    modelNode = _processCamera(node, scene->MCameras[c], parent);
                    break;
                }
            }

            // Light
            for (uint l = 0; l < scene->MNumLights; l++) {
                if (scene->MLights[l]->MName == node->MName) {
                    modelNode = _processLight(node, scene->MLights[l], parent);
                    break;
                }
            }

            // Defaults to an empty node
            modelNode ??= new ModelNode(node->MName, node->MTransformation, parent);

            for (uint c = 0; c < node->MNumChildren; c++) {
                ModelNode child = _processNode(node->MChildren[c], scene, modelNode);
                modelNode.Children.Add(child);
            }

            return modelNode;
        }

        private static unsafe ModelMesh _processMesh(AssNode* node, AssScene* scene, ModelNode parent) {
            var parts = new MeshPart[node->MNumMeshes];
            var shaders = new Dictionary<uint, Shader>((int)node->MNumMeshes);

            for (uint p = 0; p < node->MNumMeshes; p++) {
                AssMesh* part = scene->MMeshes[node->MMeshes[p]];

                var vertices = new Vertex[part->MNumVertices];
                var indices = new List<uint>((int)part->MNumFaces * 3);
                
                for (uint v = 0; v < part->MNumVertices; v++) {
                    vertices[v] = new Vertex {
                        Position = part->MVertices[v],
                        Normal = part->MNormals[v],
                        Tangent = part->MTangents[v],
                    };
                }

                for (uint f = 0; f < part->MNumFaces; f++) {
                    AssFace face = part->MFaces[f];

                    for (uint i = 0; i < face.MNumIndices; i++)
                        indices.Add(face.MIndices[i]);
                }

                

                parts[p] = new MeshPart(vertices, indices.ToArray());
            }

            return new ModelMesh(node->MName, node->MTransformation, parent, parts);
        }

        private static unsafe ModelCamera _processCamera(AssNode* node, AssCamera* camera, ModelNode parent) {
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

        private static unsafe ModelLight  _processLight(AssNode* node, AssLight* light, ModelNode parent) {
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

            modelLight.Ambient = light->MColorAmbient;
            modelLight.Diffuse = light->MColorDiffuse;
            modelLight.Specular = light->MColorSpecular;

            return modelLight;
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
        public Matrix4x4 Transform {get; protected set;}
        public List<ModelNode> Children {get; protected set;}

        public Matrix4x4 ModelTransform {
            get => (Parent?.ModelTransform ?? Matrix4x4.Identity) * Transform;
            // TODO Add setter
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

        // TODO: Refactor constructor
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