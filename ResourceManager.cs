using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Input;
using ImGuiNET;

namespace SceneEditor.Resources {
    // TODO: Find a better way to manage contents
    static class ResourceManager {
        public static Dictionary<string, Resource> resources = new Dictionary<string, Resource>();

        public static void AddResource(string name, Resource resource) {
            resources.Add(name, resource);
            resource.Name = name;
        }
        public static void AddResource(IEnumerable<(string, Resource)> resources) {
            foreach (var resource in resources)
                AddResource(resource.Item1, resource.Item2);
        }

        public static Resource GetResource(string name) {
            return resources[name];
        }

        public static void RemoveResource(string name) {
            resources.Remove(name, out var currResource);

            currResource.OnRemove();
            currResource.thumbnail?.Dispose();
            MainGame.Instance.imguiRenderer.UnbindTexture(currResource.ImGuiThumbPointer);

            foreach (var resource in resources) {
                if (resource.Value.Parent == currResource) {
                    RemoveResource(resource.Key);
                }
            }

        }

        public static Dictionary<string, T> FilterResources<T>() where T : Resource {
            var ret = new Dictionary<string, T>();

            foreach (var resource in resources) {
                if (resource.Value is T value)
                    ret.Add(resource.Key, value);
            }

            return ret;
        }
    }

    /// <summary>A wrapper around an external resource</summary>
    abstract class Resource {
        private Dictionary<string, Resource> resources => ResourceManager.resources;

        public Resource Parent {get; set;}
        public abstract object RawResource {get; set;}
        public IntPtr ImGuiThumbPointer {get; private set;}

        private Texture2D _thumbnail;
        public Texture2D thumbnail {
            get => _thumbnail;
            protected set {
                var imguiRenderer = MainGame.Instance.imguiRenderer;

                if (_thumbnail != null) {
                    imguiRenderer.UnbindTexture(ImGuiThumbPointer);
                    _thumbnail.Dispose();
                }

                _thumbnail = value;
                ImGuiThumbPointer = imguiRenderer.BindTexture(value);
            }
        }

        private string _name;
        public string Name {
            get => _name;
            set {
                if (_name != null)
                    resources.Remove(_name);
                
                resources[value] = this;
                _name = value;
            }
        }

        public virtual void OnRemove() {}

    }



    class ModelResource : Resource {
        public Model Model {get; private set;}
        
        public override object RawResource {
            get => Model;
            set => Model = (Model)value;
        }

        public ModelResource(Model model) {
            Model = model;

            // TODO: Generate thumbnail
            thumbnail = MainGame.Instance.blankTexture;

            foreach (var mesh in model.Meshes) {
                var meshRes = new MeshResource(mesh);

                meshRes.Parent = this;
                ResourceManager.AddResource(mesh.Name, meshRes);
            }
        }

        // TODO: Create a custon way to load models without the content pipeline
        public ModelResource(string name) : this(MainGame.Instance.Content.Load<Model>(name)) {
            
        }
    }

    class MeshResource : Resource {
        public Tuple<Vector3, Vector3, Vector3>[] triangles {get; private set;}

        private ModelMesh _mesh;
        public ModelMesh Mesh {
            get => _mesh;
            private set {
                _mesh = value;
                triangles = value.GetTriangles();

                _generateThumbnail();
            }
        }

        public override object RawResource {
            get => Mesh;
            set => Mesh = (ModelMesh)value;
        }
    
        public MeshResource(ModelMesh mesh) {
            var gd = MainGame.Instance.GraphicsDevice;
            thumbnail = new RenderTarget2D(gd, 100, 100, false, SurfaceFormat.Color, DepthFormat.Depth24);
            
            Mesh = mesh;
        }
    
        private void _generateThumbnail() {
            _mesh.ParentBone.ModelTransform.Decompose(out var meshScale, out _, out _);
            Matrix world = Matrix.CreateScale(meshScale) * Matrix.CreateFromYawPitchRoll(0, MathF.PI * 1.5f, 0);

            float cameraDist = _mesh.BoundingSphere.Transform(world).Radius * 2;
            Vector3 camPos = Vector3.Normalize(new Vector3(0, 1, -1)) * cameraDist;

            Matrix view = Matrix.CreateLookAt(camPos, Vector3.Zero, Vector3.Up);
            Matrix proj = Matrix.CreatePerspectiveFieldOfView(MathF.PI/2f, 1, 0.1f, 10000);

            var thumb = thumbnail as RenderTarget2D;
            thumb.RenderTo(delegate {
                thumb.GraphicsDevice.Clear(Color.CornflowerBlue);

                var gd = thumb.GraphicsDevice;
                gd.RasterizerState = RasterizerState.CullCounterClockwise;
                gd.DepthStencilState = DepthStencilState.Default;
                gd.BlendState = BlendState.Opaque;
                gd.SamplerStates[0] = SamplerState.LinearWrap;

                foreach (BasicEffect effect in _mesh.Effects) {
                    effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;

                    effect.World = world;
                    effect.View = view;
                    effect.Projection = proj;
                }

                _mesh.Draw();
            });
        }
    }
}