using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.ParticleSystem;

namespace Assets
{
    internal class Sphere
    {
        // This matrix contains the position and radius of the sphere...
        private Matrix4x4 inverseTransformationMatrix;

        private Vector3 position;

        private Color color;

        private Vector3 ambientCoefficients;
        private Vector3 diffuseCoefficients;
        private Vector3 specularCoefficients;

        private int specularShine;

        public Sphere(
            Vector3 spherePosition,
            Vector3 sphereRadius,
            Color color,
            Vector3 ambientCoefficients,
            Vector3 diffuseCoefficients,
            Vector3 specularCoefficients,
            int specularShine
        )
        {

            this.position = spherePosition;

            this.inverseTransformationMatrix =
                Matrix4x4.identity *
                Matrix4x4.Scale(
                    new Vector3(1f / sphereRadius.x, 1f / sphereRadius.y, 1f / sphereRadius.z)
                ) *
                Matrix4x4.Translate(-spherePosition);



            this.color = color;

            this.ambientCoefficients = ambientCoefficients;
            this.diffuseCoefficients = diffuseCoefficients;
            this.specularCoefficients = specularCoefficients;

            this.specularShine = specularShine;
        }

        public Matrix4x4 InverseTransformationMatrix
        {
            get => inverseTransformationMatrix;
        }

        public Vector3 Position
        {
            get => position;
        }

        public Color Color
        {
            get => color;
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

        public float SpecularShine
        {
            get => specularShine;
        }

    }
}
