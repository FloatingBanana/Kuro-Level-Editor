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

        public static Dictionary<string, object> resources = new Dictionary<string, object>();

        public static void AddResource(string name, object resource) {
            resources.Add(name, resource);
        }
        public static void AddResource(IEnumerable<(string, object)> resources) {
            foreach (var resource in resources)
                AddResource(resource.Item1, resource.Item2);
        }

        public static void LoadResource(string name, string filename) {
            AddResource(name, MainGame.Instance.Content.Load<object>(filename));
        }
        public static void LoadResource(IEnumerable<(string, string)> resources) {
            foreach (var resource in resources)
                LoadResource(resource.Item1, resource.Item2);
        }

        public static object GetResource(string name) {
            return resources[name];
        }
        public static T GetResource<T>(string name) {
            return resources[name] is T ? (T)resources[name] : default(T);
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
    abstract class Resource : IDisposable {
        private Resource _parent;
        public Resource parent {
            get => _parent;
            protected set {
                if (_parent != null)
                    _parent.OnRemove -= _selfRemove;
                
                if ((_parent = value) != null) {
                    _parent.OnRemove += _selfRemove;
                }
            }
        }

        public object RawResource {get; protected set;}

        public Resource(object res, Resource parent = null) {
            RawResource = res;
            this.parent = parent;
        }

        public abstract void Dispose();
        private void _selfRemove(Resource sender) {

        }

        public event Action<Resource> OnRemove;
    }

    class ModelResource : Resource {
        public Model model {
            get => RawResource as Model;
            set => RawResource = value;
        }

        public ModelResource(Model model) : base(model) {
            
        }

        public override void Dispose() {

        }
    }
}