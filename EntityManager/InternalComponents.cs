using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace SceneEditor.EntitySystem {
    class InternalComponentField : ComponentFieldBase {
        public InternalComponent component {get; private set;}
        public MemberInfo member {get; private set;}

        public InternalComponentField(InternalComponent component, MemberInfo member, string name, bool isReadOnly) : base(name, null, isReadOnly) {
            this.type = (member as FieldInfo)?.FieldType ??
                        (member as PropertyInfo).PropertyType;

            this.component = component;
            this.member = member;
        }

        public override object Value {
            get => member.MemberType switch {
                MemberTypes.Field => (member as FieldInfo).GetValue(component),
                MemberTypes.Property => (member as PropertyInfo).GetValue(component),

                _ => throw new Exception("Bad member type")
            };

            set {
                // TODO: Cache casted MemberInfo
                switch (member.MemberType) {
                case MemberTypes.Field:
                    (member as FieldInfo).SetValue(component, value);
                    break;
                case MemberTypes.Property:
                    (member as PropertyInfo).SetValue(component, value);
                    break;
                }
            }
        }
    }

    abstract class InternalComponent : Component {
        public InternalComponent() {
            var fields = new List<InternalComponentField>();

            foreach (var member in this.GetType().GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
                // Attributes
                VisibleField visibleAtt = member.GetCustomAttribute<VisibleField>();
                HiddenField hiddenAtt = member.GetCustomAttribute<HiddenField>();
                string name = visibleAtt?.customName ?? member.Name.Stylize();

                // Field member
                if (member.MemberType == MemberTypes.Field) {
                    var field = (FieldInfo)member;
                    bool isReadOnly = field.IsInitOnly || (visibleAtt?.isReadOnly ?? false);
                    
                    if ((field.IsPublic && hiddenAtt == null) || (field.IsPrivate && visibleAtt != null)) {
                        fields.Add(new InternalComponentField(this, member, name, isReadOnly));
                    }
                }
                // Property member
                else if (member.MemberType == MemberTypes.Property) {
                    var property = (PropertyInfo)member;
                    bool isReadOnly = !property.CanWrite || (visibleAtt?.isReadOnly ?? false);

                    if (property.CanRead && ((property.GetMethod.IsPublic && hiddenAtt == null) || (property.GetMethod.IsPrivate && visibleAtt != null))) {
                        fields.Add(new InternalComponentField(this, member, name, isReadOnly));
                    }
                }
            }

            Fields = fields.ToArray();
        }
    }


    // Components
    [Category("", hidden = true)]
    class Transform : InternalComponent {
        public Vector3 Position = new Vector3(1, 1, 1);
        public Vector3 Scale = new Vector3(1, 1, 1);
        
        [VisibleField(customName = "Rotation")]
        public Vector3 EulerRotation = new Vector3();

        [HiddenField]
        public Quaternion Rotation => Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(EulerRotation.Y),
            MathHelper.ToRadians(EulerRotation.X),
            MathHelper.ToRadians(EulerRotation.Z)
        );
        
        // TODO: Find a more elegant way to decompose and recompose the transformation
        [HiddenField]
        public Matrix TransformationMatrix {
            get {
                Span<float> pos = stackalloc float[3] {Position.X, Position.Y, Position.Z};
                Span<float> rot = stackalloc float[3] {EulerRotation.X, EulerRotation.Y, EulerRotation.Z};
                Span<float> scale = stackalloc float[3] {Scale.X, Scale.Y, Scale.Z};

                float[] mat = new float[16];
                ImGuizmoNET.ImGuizmo.RecomposeMatrixFromComponents(ref pos[0], ref rot[0], ref scale[0], ref mat[0]);
                return Utils.CopyArrayToMatrix(mat);
            }
            set {
                Span<float> pos = stackalloc float[3];
                Span<float> rot = stackalloc float[3];
                Span<float> scale = stackalloc float[3];

                Utils.MatrixToArray(value, out var source);
                ImGuizmoNET.ImGuizmo.DecomposeMatrixToComponents(ref source[0], ref pos[0], ref rot[0], ref scale[0]);

                Position = new Vector3(pos[0], pos[1], pos[2]);
                Scale = new Vector3(scale[0], scale[1], scale[2]);
                EulerRotation = Utils.WrapDegrees(new Vector3(rot[0], rot[1], rot[2]));
            }
        }
    }

    [Category("Rendering")]
    class MeshRenderer : InternalComponent, IHoverable {
        private Tuple<Vector3, Vector3, Vector3>[] triangles;

        ModelMesh _mesh;
        public ModelMesh Mesh {
            get => _mesh;
            set {
                _mesh = value;
                triangles = Mesh?.GetTriangles();
            }
        }

        public PlaneIntersectionType intersection = PlaneIntersectionType.Front;

        public override void EditorRender(GameTime gameTime, Matrix view, Matrix projection) {
            foreach (BasicEffect effect in Mesh.Effects) {
                effect.EnableDefaultLighting();
                effect.PreferPerPixelLighting = true;
                effect.Alpha = 1;

                effect.View = view;
                effect.Projection = projection;
                effect.World = entity.transform.TransformationMatrix;
            }

            Mesh.Draw();
        }
    
        public bool IsHovered(Matrix view, Matrix projection, Vector2 mousePos) {
            Matrix world = entity.transform.TransformationMatrix;
            Viewport vp = MainGame.Instance.GraphicsDevice.Viewport;

            // REVIEW: Viewport.Unproject creates the inverse of view and projection matrix,
            //         which is a expensive operation. Calling it twice is a waste, I could
            //         just pre-calculate the inverses and then transform the points manually.
            Vector3 nearMouse = vp.Unproject(new Vector3(mousePos, 0), projection, view, Matrix.Identity);
            Vector3 farMouse = vp.Unproject(new Vector3(mousePos, 1), projection, view, Matrix.Identity);

            Vector3 dir = Vector3.Normalize(farMouse - nearMouse);
            Ray ray = new Ray(nearMouse, dir);

            foreach (var triangle in triangles) {
                var v1 = Vector3.Transform(triangle.Item1, world);
                var v2 = Vector3.Transform(triangle.Item2, world);
                var v3 = Vector3.Transform(triangle.Item3, world);

                ray.Intersects(ref v1, ref v2, ref v3, out var result);
                if (result != null) return true;
            }
            
            return false;
        }
    }
}