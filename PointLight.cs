using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    internal class PointLight
    {

        private Vector3 position;

        private Vector3 ambientCoefficients;
        private Vector3 diffuseCoefficients;
        private Vector3 specularCoefficients;

        public PointLight(
            Vector3 position, 
            Vector3 ambientCoefficients,
            Vector3 diffuseCoefficients,
            Vector3 specularCoefficients
        ) 
        { 
            this.position = position;

            this.ambientCoefficients = ambientCoefficients; 
            this.diffuseCoefficients = diffuseCoefficients;
            this.specularCoefficients = specularCoefficients;
        }

        public Vector3 Position
        {
            get => position;
        }

        public Vector3 AmbientCoefficients
        {
            get => ambientCoefficients;
        }

        public Vector3 DiffuseCoefficients
        {
            get => diffuseCoefficients;
        }

        public Vector3 SpecularCoefficients
        {
            get => specularCoefficients;
        }
    }
}
