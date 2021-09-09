using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SceneEditor.EntitySystem {
    static class EntityManager {
        public static Entity selected;
        public static List<Entity> entities = new List<Entity>();

        // Callbacks
        public static void Update(GameTime gameTime) {
            foreach (Entity entity in entities) {
                foreach (Component comp in entity.components) {
                    if (comp.enabled) comp.EditorUpdate(gameTime);
                }
            }
        }
        public static void Render(GameTime gameTime, Matrix view, Matrix projection) {
            var gd = MainGame.Instance.GraphicsDevice;

            gd.DepthStencilState = DepthStencilState.Default;
            gd.BlendState = BlendState.Opaque;
            gd.RasterizerState = RasterizerState.CullCounterClockwise;
            gd.SamplerStates[0] = SamplerState.LinearWrap;

            foreach (Entity entity in entities) {
                foreach (Component comp in entity.components) {
                    if (comp.enabled) comp.EditorRender(gameTime, view, projection);
                }
            }

            foreach (Entity entity in entities) {
                foreach (Component comp in entity.components) {
                    if (comp.enabled) comp.EditorUI(gameTime);
                }
            }
        }


        public static void AddEntity(Entity entity) {
            entities.Add(entity);
        }
        public static Entity AddEntity(string name) {
            var entity = new Entity(name);
            AddEntity(entity);
            return entity;
        }


        public static Entity GetEntity(string name) {
            foreach (var entity in entities) {
                if (entity.name == name) return entity;
            }
            return null;
        }
        public static List<T> GetComponentsInScene<T>() where T : class {
            var ret = new List<T>();

            foreach (Entity entity in entities) {
                ret.AddRange(entity.GetComponents<T>());
            }
            return ret;
        }


        public static void Clean() {
            foreach (var entity in entities) {
                foreach (var comp in entity.components) {
                    comp.OnRemove();
                }
                entity.components.Clear();
            }
            entities.Clear();
        }
    }

    class Entity {
        public string name;
        public List<Component> components = new List<Component>();

        public Entity(string name) {
            this.name = name;
        }

        public T GetComponent<T>() where T : class {
            foreach (var comp in components) {
                if (comp is T) return comp as T;
            }
            return null;
        }
        public List<T> GetComponents<T>() where T : class {
            var ret = new List<T>();

            foreach (var comp in components) {
                if (comp is T) ret.Add(comp as T);
            }
            return ret;
        }
    
        public void AttachComponent(Component comp) {
            components.Add(comp);
            comp.entity = this;
            comp.OnAttach();
        }
        public void AttachComponent(IEnumerable<Component> comps) {
            foreach (var comp in comps)
                AttachComponent(comp);
        }
        public void RemoveComponent(Component comp) {
            comp.OnRemove();
            components.Remove(comp);
        }

        private Transform _transform;
        public Transform transform {
            get {
                if (_transform == null) _transform = GetComponent<Transform>();
                return _transform;
            }
        }
    }

    #region Attributes

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        class VisibleField : Attribute {
            public bool isReadOnly = false;
            public string customName;
        }

        [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
        class HiddenField : Attribute {}

        [AttributeUsage(AttributeTargets.Method)]
        internal class RunOnEditor : Attribute {}

        [AttributeUsage(AttributeTargets.Class)]
        internal class Category : Attribute {
            public string name;
            public bool hidden = false;

            public Category(string name) {
                this.name = name;
            }
        }

    #endregion


    // REVIEW: Maybe instead of creating a new class inheriting this one,
    //         I could just make the constructor accept callbacks to be
    //         called when getting and setting the value.
    abstract class ComponentFieldBase {
        public string name {get; protected set;}
        public bool isReadOnly {get; protected set;}
        public Type type {get; protected set;}

        public abstract object Value {get; set;}

        public ComponentFieldBase(string name, Type type, bool isReadOnly) {
            this.name = name;
            this.type = type;
            this.isReadOnly = isReadOnly;
        }

        public T GetValueCasted<T>() {
            return (T)Value;
        }
        public void GetValueCasted<T>(out T value) {
            value = (T)Value;
        }
    }

    abstract class Component {
        public bool enabled = true;
        public Entity entity;

        public virtual void OnAttach() {}
        public virtual void EditorRender(GameTime gameTime, Matrix view, Matrix projection) {}
        public virtual void EditorUI(GameTime gameTime) {}
        public virtual void EditorUpdate(GameTime gameTime) {}
        public virtual void OnRemove() {}
        
        public ComponentFieldBase[] Fields {get; protected set;}
    }


    // Special interfaces
    interface IHoverable {
        bool IsHovered(Matrix view, Matrix projection, Vector2 mousePos);
    }
}