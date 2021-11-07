using System;
using System.Drawing;
using System.Numerics;
using System.Runtime.InteropServices;
using Silk.NET.OpenGL.Legacy;
using Silk.NET.Assimp;


namespace Kuro.Renderer {
    
    public abstract class ModelLight : ModelNode {
        public Vector3 Ambient {get; set;} = new(1,1,1);
        public Vector3 Diffuse {get; set;} = new(1,1,1);
        public Vector3 Specular {get; set;} = new(1,1,1);

        protected ModelLight(string name, Matrix4x4 transform, ModelNode parent) : base(name, transform, parent) {
            
        }
    }

    public class ModelDirectionalLight : ModelLight {
        public Vector3 Direction {get; set;}

        public ModelDirectionalLight(string name, Matrix4x4 transform, Vector3 direction, ModelNode parent) : base(name, transform, parent) {
            Direction = direction;
        }
    }

    public class ModelPointLight : ModelLight {
        public Vector3 Position {get; set;}

        public float AttenuationConstant {get; set;}
        public float AttenuationLinear {get; set;}
        public float AttenuationQuadratic {get; set;}

        public ModelPointLight(string name, Matrix4x4 transform, Vector3 position, float constant, float linear, float quadratic, ModelNode parent) : base(name, transform, parent) {
            Position = position;
            
            AttenuationConstant = constant;
            AttenuationLinear = linear;
            AttenuationQuadratic = quadratic;
        }
    }

    public class ModelSpotLight : ModelLight {
        public Vector3 Position {get; set;}
        public Vector3 Direction {get; set;}
        
        public float InnerRadius {get; set;}
        public float OuterRadius {get; set;}

        public ModelSpotLight(string name, Matrix4x4 transform, Vector3 position, Vector3 direction, float innerRadius, float outerRadius, ModelNode parent) : base(name, transform, parent) {
            Position = position;
            Direction = direction;

            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }
    }

    // TODO: Area light
    // TODO: Ambient light (not sure if this one is necessary)

}