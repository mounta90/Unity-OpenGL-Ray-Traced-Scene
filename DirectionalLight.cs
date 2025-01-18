using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets
{
    internal class DirectionalLight
    {

        private Vector3 direction;

        private Vector3 ambientCoefficients;
        private Vector3 diffuseCoefficients;
        private Vector3 specularCoefficients;

        public DirectionalLight(
            Vector3 direction,
            Vector3 ambientCoefficients,
            Vector3 diffuseCoefficients,
            Vector3 specularCoefficients
        )
        {
            this.direction = direction;

            this.ambientCoefficients = ambientCoefficients;
            this.diffuseCoefficients = diffuseCoefficients;
            this.specularCoefficients = specularCoefficients;
        }

        public Vector3 Direction
        {
            get => direction;
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
