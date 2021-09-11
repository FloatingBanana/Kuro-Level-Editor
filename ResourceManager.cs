using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended.Input;

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

            foreach (var resource in resources) {
                if (resource.Value.Parent == currResource) {
                    RemoveResource(resource.Key);
                }
            }
        }

        public static Dictionary<string, T> FilterResources<T>() {
            var ret = new Dictionary<string, T>();

            foreach (var resource in resources) {
                if (resource.Value is T value)
                    ret.Add(resource.Key, value);
            }

            return ret;
        }
    }

    /// <summary>A wrapper around a external resource</summary>
    abstract class Resource {
        private Dictionary<string, Resource> resources => ResourceManager.resources;

        public Resource Parent {get; set;}
        public abstract object RawResource {get; set;}

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
    }



    class ModelResource : Resource {
        public Model Model {get; private set;}
        
        public override object RawResource {
            get => Model;
            set => Model = (Model)value;
        }

        public ModelResource(Model model) {
            Model = model;

            foreach (var mesh in model.Meshes) {
                var meshRes = new MeshResource(mesh);

                meshRes.Parent = this;
                ResourceManager.AddResource(mesh.Name, meshRes);
            }
        }

        public ModelResource(string name) : this(MainGame.Instance.Content.Load<Model>(name)) {}
    }

    class MeshResource : Resource {
        public Tuple<Vector3, Vector3, Vector3>[] triangles {get; private set;}

        private ModelMesh _mesh;
        public ModelMesh Mesh {
            get => _mesh;
            private set {
                _mesh = value;
                triangles = value.GetTriangles();
            }
        }


        public override object RawResource {
            get => Mesh;
            set => Mesh = (ModelMesh)value;
        }
    
        public MeshResource(ModelMesh mesh) {
            Mesh = mesh;
        }
    }
}