using System;
using Microsoft.Xna.Framework;

namespace Kuro.LevelEditor.Graphics {
    public abstract class KuroModelLight : KuroModelNode {
        public Vector3 AmbientColor {get; set;} = new(1,1,1);
        public Vector3 DiffuseColor {get; set;} = new(1,1,1);
        public Vector3 SpecularColor {get; set;} = new(1,1,1);

        protected KuroModelLight(string name, Matrix transform, KuroModelNode parent) : base(name, transform, parent) {
            
        }
    }

    public class KuroModelDirectionalLight : KuroModelLight {
        public Vector3 Direction {get; set;}

        public KuroModelDirectionalLight(string name, Matrix transform, Vector3 direction, KuroModelNode parent) : base(name, transform, parent) {
            Direction = direction;
        }
    }

    public class KuroModelPointLight : KuroModelLight {
        public Vector3 Position {get; set;}

        public float AttenuationConstant {get; set;}
        public float AttenuationLinear {get; set;}
        public float AttenuationQuadratic {get; set;}

        public KuroModelPointLight(string name, Matrix transform, Vector3 position, float constant, float linear, float quadratic, KuroModelNode parent) : base(name, transform, parent) {
            Position = position;
            
            AttenuationConstant = constant;
            AttenuationLinear = linear;
            AttenuationQuadratic = quadratic;
        }
    }

    public class KuroModelSpotLight : KuroModelLight {
        public Vector3 Position {get; set;}
        public Vector3 Direction {get; set;}
        
        public float InnerRadius {get; set;}
        public float OuterRadius {get; set;}

        public KuroModelSpotLight(string name, Matrix transform, Vector3 position, Vector3 direction, float innerRadius, float outerRadius, KuroModelNode parent) : base(name, transform, parent) {
            Position = position;
            Direction = direction;

            InnerRadius = innerRadius;
            OuterRadius = outerRadius;
        }
    }

    // TODO: Area light
    // TODO: Ambient light (not sure if this one is necessary)

}
