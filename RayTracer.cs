using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Assets;
using Unity.VisualScripting;
using UnityEngine.UIElements;
using System;
using static UnityEngine.UI.Image;

public class RayTracer : MonoBehaviour
{

    Shader shader;
    Texture2D pixTex;
    Color32[] pixmap1D;

    public GameObject plane;
    public Material currentMaterial;

    private Vector4 backgroundColor = new Vector4(0.0f, 0.0f, 0.0f, 1.0f);

    private Sphere sphere1, sphere2, sphere3;
    private Sphere[] sphereList = new Sphere[3];

    private DirectionalLight sun;
    private PointLight lightBulb;

    private Vector3 eye, lookAtPoint;

    private Vector3 n, u, v;

    private float aspectRatio;
    private float viewAngle;

    private float nearPlaneHeight, nearPlaneWidth;

    private Vector3 nearPlane;
    private float nearPlaneDistanceFromCamera;

    private int pixelBlockSize;
    private int nRows, nColumns;

    public Boolean isAmbientLightingIncluded = true;
    public Boolean isDiffuseLightingIncluded = true;
    public Boolean isSpecularLightingIncluded = true;

    public Boolean isShadowingIncluded = false;

    // Sometimes, when a ray needs to be reflected off a surface, the origin (hit point) may be computed as being "below" the hit surface...
    // This is due to floating-point precision errors...
    // More is explained when this variable is used (in the "ComputePixelColor" and "TraceRay" functions).
    public float precisionBias = 0.0001f;

    // Provides a limit on how many times a ray is reflected (when generating secondary reflected rays).
    public int reflectionDepthLimit = 3;

    public Vector3 sunDirection = new Vector3(-1.0f, -1.0f, 1.0f);
    public Vector3 lightBulbPosition = new Vector3(-1.0f, -10.0f, 0.25f);

    public Vector3 sphere1Position = new Vector3(-0.1f, 0.2f, -0.3f);
    public Vector3 sphere2Position = new Vector3(0, -0.2f, -0.8f);
    public Vector3 sphere3Position = new Vector3(0.3f, 0.3f, -1.1f);

    private void SetTexturePlane()
    {

        pixmap1D = new Color32[nColumns * nRows];
        pixTex = new Texture2D(nColumns, nRows, TextureFormat.RGBA32, true);

        plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "Near Plane";

        float heightCam = (float)Camera.main.orthographicSize * 2.0f;
        float widthCam = heightCam * Screen.width / Screen.height;

        Camera.main.transform.position = new Vector3(0, 0, -8.6f);
        plane.transform.position = new Vector3(0, 0, -1.0f); // assuming your camera is at 0,0,-1 near plan is 1 unit.
        plane.transform.localScale = new Vector3(widthCam / 10, 1, heightCam / 10);

        plane.transform.Rotate(90, 180, 0, Space.Self);
        plane.GetComponent<MeshFilter>().gameObject.GetComponent<MeshRenderer>().material = currentMaterial;

    }

    private void SetObjects()
    {

        sphere1 = new Sphere(
            sphere1Position, // position
            new Vector3(0.075f, 0.075f, 0.075f), // radius
            new Color(1.0f, 0, 0), // color
            new Vector3(0.5f, 0, 0), // ambient coefficients
            new Vector3(0.5f, 0, 0), // diffuse coefficients
            new Vector3(0, 0, 0), // specular coefficients
            0 // specular shininess
        );

        sphere2 = new Sphere(
            sphere2Position,
            new Vector3(0.15f, 0.15f, 0.15f),
            new Color(0, 0, 1.0f),
            new Vector3(0, 0, 0.2f),
            new Vector3(0, 0, 0.2f),
            new Vector3(0, 0, 0.9f),
            10
        );

        sphere3 = new Sphere(
            sphere3Position,
            new Vector3(0.3f, 0.3f, 0.3f),
            new Color(0, 1.0f, 0),
            new Vector3(0, 0.5f, 0),
            new Vector3(0, 0.75f, 0),
            new Vector3(0, 0.25f, 0),
            100
        );

        sphereList[0] = sphere1;
        sphereList[1] = sphere2;
        sphereList[2] = sphere3;

    }

    private void SetLights()
    {

        sun = new DirectionalLight(
            sunDirection, // direction
            new Vector3(0.8f, 0.8f, 0.2f), // ambient coefficients
            new Vector3(0.8f, 0.8f, 0.2f), // diffuse coefficients
            new Vector3(0.8f, 0.8f, 0.2f) // specular coefficients
        );

        lightBulb = new PointLight(
            lightBulbPosition, // position
            new Vector3(0.4f, 0.4f, 0.8f), // ambient coefficients
            new Vector3(0.4f, 0.4f, 0.8f), // diffuse coefficients
            new Vector3(0.4f, 0.4f, 0.8f) // specular coefficients
        );

    }

    private void SetPerspectiveProjection()
    {
        eye = new Vector3(0, 0, 1.0f);
        lookAtPoint = new Vector3(0, 0, 0);

        n = (eye - lookAtPoint).normalized;

        Vector3 up = new Vector3(0, 1.0f, 0);
        u = Vector3.Cross(up, n).normalized;

        v = Vector3.Cross(n, u).normalized;

        // ---------------------------------------
        // ---------------------------------------

        aspectRatio = (float)Screen.width / (float)Screen.height;

        viewAngle = 30.0f;
        float theta = ((viewAngle / 2.0f) * Mathf.PI) / 180f;

        nearPlane = new Vector3(0, 0, 0.5f);
        nearPlaneDistanceFromCamera = Mathf.Abs(eye.z - nearPlane.z);

        nearPlaneHeight = nearPlaneDistanceFromCamera * Mathf.Tan(theta);
        nearPlaneWidth = nearPlaneHeight * aspectRatio;

        pixelBlockSize = 1;

        nRows = (Screen.height / pixelBlockSize) + 1;
        nColumns = (Screen.width / pixelBlockSize) + 1;
    }

    private Vector3 GenerateRayDirection(int row, int column)
    {
        // -------------------------------------------------------------------
        // (BEGINNING) Create the ray...
        // r(t) = eye + direction * t
        // direction = -N*n + rayEndU * u + rayEndV * v
        // -------------------------------------------------------------------
        float rayEndU = -nearPlaneWidth + nearPlaneWidth * 2f * (float)column / (float)nColumns;
        float rayEndV = -nearPlaneHeight + nearPlaneHeight * 2f * (float)row / (float)nRows;

        nearPlaneDistanceFromCamera = nearPlane.z;

        Vector3 rayDirection = (-nearPlaneDistanceFromCamera * n + rayEndU * u + rayEndV * v).normalized;
        // -------------------------------------------------------------------
        // (ENDING) Create the ray...
        // -------------------------------------------------------------------

        return rayDirection;
    }

    // Used for shadow rays and checking their intersection with any object in their path to a light source.
    private Boolean DoesPointSeeLightSource(Vector3 shadowRayOrigin, Vector3 shadowRayDirection)
    {

        // If user specifies that they dont want shadowing,
        // then the point will be counted as seeing the light source and "true" is returned directly...
        if (!isShadowingIncluded)
            return true;

        // -------------------------------------------------------------------
        // (BEGINNING) Convert to homogeneous ray and origin...
        // -------------------------------------------------------------------
        Vector4 homogeneousOrigin = new Vector4(
            shadowRayOrigin.x,
            shadowRayOrigin.y,
            shadowRayOrigin.z,
            1.0f
        );

        Vector4 homogeneousRayDirection = new Vector4(
            shadowRayDirection.x,
            shadowRayDirection.y,
            shadowRayDirection.z,
            0.0f
        );
        // -------------------------------------------------------------------
        // (ENDING) Convert to homogeneous ray and origin...
        // -------------------------------------------------------------------

        for (int i = 0; i < sphereList.Length; i++)
        {
            // -------------------------------------------------------------------
            // (BEGINNING) Convert to transformed ray and origin...
            // -------------------------------------------------------------------
            Vector4 transformedHomogeneousOrigin = sphereList[i].InverseTransformationMatrix * homogeneousOrigin;
            Vector4 transformedHomogeneousRayDirection = sphereList[i].InverseTransformationMatrix * homogeneousRayDirection;

            // S'
            Vector3 transformedOrigin = new Vector3(
                transformedHomogeneousOrigin.x,
                transformedHomogeneousOrigin.y,
                transformedHomogeneousOrigin.z
            );

            // c'
            Vector3 transformedRayDirection = new Vector3(
                transformedHomogeneousRayDirection.x,
                transformedHomogeneousRayDirection.y,
                transformedHomogeneousRayDirection.z
            );
            // -------------------------------------------------------------------
            // (ENDING) Convert to transformed ray and origin...
            // -------------------------------------------------------------------

            // -------------------------------------------------------------------
            // (BEGINNING) Check if transformed ray intersects...
            // -------------------------------------------------------------------
            float a = transformedRayDirection.magnitude * transformedRayDirection.magnitude;
            float b = Vector3.Dot(transformedOrigin, transformedRayDirection);
            float c = transformedOrigin.magnitude * transformedOrigin.magnitude - 1;

            float discriminant = b * b - a * c;
            if (discriminant >= 0.0f)
            {
                // Whether there is one (discriminant = 0) or two ray intersections (discriminant > 0)...
                // the following calculation of t will get us the single time that we need to get the first hit point:
                float t = (-b / a) - (Mathf.Sqrt(discriminant) / a);

                if (t >= 0.0f)
                {
                    // The ray intersects some object, before reaching the light source...
                    // therefore "false" is returned to signal that the point does not see the light source...
                    return false;
                }
                  
            }
            // -------------------------------------------------------------------
            // (ENDING) Check if transformed ray intersects...
            // -------------------------------------------------------------------
        }

        // The ray reaches the light source, without intersecting any object, therefore "true" is returned to signal that point sees the light source...
        return true;
    }

    private Vector4 TraceRay(Vector3 origin, Vector3 rayDirection, int reflectionDepth)
    {

        // -------------------------------------------------------------------
        // (BEGINNING) Convert to homogeneous ray and origin...
        // -------------------------------------------------------------------
        Vector4 homogeneousOrigin = new Vector4(
            origin.x,
            origin.y,
            origin.z,
            1.0f
        );

        Vector4 homogeneousRayDirection = new Vector4(
            rayDirection.x,
            rayDirection.y,
            rayDirection.z,
            0.0f
        );
        // -------------------------------------------------------------------
        // (ENDING) Convert to homogeneous ray and origin...
        // -------------------------------------------------------------------

        int sphereIndex = -1;
        float minHitTime = Mathf.Infinity;
        for (int i = 0; i < sphereList.Length; i++)
        {

            // -------------------------------------------------------------------
            // (BEGINNING) Convert to transformed ray and origin...
            // -------------------------------------------------------------------
            Vector4 transformedHomogeneousOrigin = sphereList[i].InverseTransformationMatrix * homogeneousOrigin;
            Vector4 transformedHomogeneousRayDirection = sphereList[i].InverseTransformationMatrix * homogeneousRayDirection;

            // S'
            Vector3 transformedOrigin = new Vector3(
                transformedHomogeneousOrigin.x,
                transformedHomogeneousOrigin.y,
                transformedHomogeneousOrigin.z
            );

            // c'
            Vector3 transformedRayDirection = new Vector3(
                transformedHomogeneousRayDirection.x,
                transformedHomogeneousRayDirection.y,
                transformedHomogeneousRayDirection.z
            );
            // -------------------------------------------------------------------
            // (ENDING) Convert to transformed ray and origin...
            // -------------------------------------------------------------------

            // -------------------------------------------------------------------
            // (BEGINNING) Check if transformed ray intersects...
            // -------------------------------------------------------------------
            float a = transformedRayDirection.magnitude * transformedRayDirection.magnitude;
            float b = Vector3.Dot(transformedOrigin, transformedRayDirection);
            float c = transformedOrigin.magnitude * transformedOrigin.magnitude - 1;

            float discriminant = b * b - a * c;
            if (discriminant >= 0.0f)
            {
                // Whether there is one (discriminant = 0) or two (discriminant > 0) ray intersections...
                // the following calculation of t will get us the single closest time that we need to get the first hit point:
                float t = (-b / a) - (Mathf.Sqrt(discriminant) / a);


                // NOTE : we dont want t values that are negative...
                // this is because we dont want to "see" anything behind us (the negative direction of the ray)...
                // This affects reflection if not restricted.
                if (t > 0.0f && t < minHitTime)
                {
                    minHitTime = t;
                    sphereIndex = i;
                }
            }
            // -------------------------------------------------------------------
            // (ENDING) Check if transformed ray intersects...
            // -------------------------------------------------------------------

        }

        Vector4 pixelColor = backgroundColor;
        if (sphereIndex > -1)
        {
            Sphere currentSphere = sphereList[sphereIndex];

            Vector3 hitPoint = origin + rayDirection * minHitTime;

            pixelColor = ComputePixelColor(hitPoint, sphereIndex);
            
            // IF object is shiny enough (shininess parameter > 0), reflect ray
            // ELSE dont...
            if (reflectionDepth < reflectionDepthLimit && currentSphere.SpecularShine > 0)
            {
                Vector3 sphereCenter = currentSphere.Position;

                Vector3 surfaceNormal = hitPoint - sphereCenter;
                surfaceNormal.Normalize();

                // Need to offset the hit point position by a "bias"...
                // Due to a possibility of floating point precision loss, the hit point may be "below" the surface...
                // thus, when we follow a ray from the hit point, we may end up hitting the inside of the surface, leading to false results.
                // Source for the bias:
                // Introducing the Ray Tracing Pipeline // Ray Tracing series (YouTube video by "The Cherno"... go to 25:20 minute mark in the video)

                Vector3 reflectedRayDirection = Vector3.Reflect(rayDirection, surfaceNormal);
                Vector3 newOrigin = hitPoint + reflectedRayDirection * precisionBias;
                int newReflectionDepth = reflectionDepth++;

                return Vector4.Min(
                    pixelColor + TraceRay(newOrigin, reflectedRayDirection, newReflectionDepth),
                    new Vector4(1.0f, 1.0f, 1.0f, 1.0f)
                );

            }
           
        }

        return pixelColor;

    }

    private Vector4 ComputePixelColor(Vector3 hitPoint, int sphereIndex)
    {

        // *********************************************************************************************
        // (Beginning) Ambient Portion of the Pixel Color
        // *********************************************************************************************

        // In case of color oversaturation, clamp the RGB values...
        // this is done in the rest of the color calculations, too.
        Vector3 ambientColor = Vector3.Min(
             Vector3.Scale(sphereList[sphereIndex].AmbientCoefficients, sun.AmbientCoefficients)
             +
             Vector3.Scale(sphereList[sphereIndex].AmbientCoefficients, lightBulb.AmbientCoefficients),
             new Vector3(1.0f, 1.0f, 1.0f)
        );

        // *********************************************************************************************
        // (Ending) Ambient Portion of the Pixel Color
        // *********************************************************************************************

        Vector3 sphereCenter = sphereList[sphereIndex].Position;

        Vector3 normal = hitPoint - sphereCenter;
        normal.Normalize();

        Vector3 pointToEye = eye - hitPoint;
        pointToEye.Normalize();

        Vector3 pointToSunLight = -sun.Direction.normalized;

        Vector3 pointToLightBulb = lightBulb.Position - hitPoint;
        pointToLightBulb.Normalize();

        // ******************************************************************************************************************
        // (Beginning) Sun
        // ******************************************************************************************************************
        Vector3 sunLightDiffuseColor = Vector3.zero;
        Vector3 sunLightSpecularColor = Vector3.zero;

        Boolean doesPointSeeSun = DoesPointSeeLightSource(hitPoint + pointToSunLight * precisionBias, pointToSunLight);
        if (doesPointSeeSun)
        { 

            float sunLightIntensity = Mathf.Max(Vector3.Dot(normal, pointToSunLight), 0.0f);

            // ---------------------------------------------------------
            // (Diffuse) Sun Light
            // ---------------------------------------------------------

            sunLightDiffuseColor = Vector3.Min(
                Vector3.Scale(sphereList[sphereIndex].DiffuseCoefficients, sun.DiffuseCoefficients) * sunLightIntensity,
                new Vector3(1.0f, 1.0f, 1.0f)
            );

            // ---------------------------------------------------------
            // (Diffuse) Sun Light
            // ---------------------------------------------------------

            // ---------------------------------------------------------
            // (Specular) Sun Light
            // ---------------------------------------------------------

            // If the sun light vector is perpindicular or not facing the surface normal, then there should be no specular lighting...
            if (sunLightIntensity > 0.0f)
            {
                Vector3 sunHalfway = (pointToEye + pointToSunLight) / 2;
                sunHalfway.Normalize();

                float sunSpecularIntensity = Mathf.Max(
                    Vector3.Dot(normal, sunHalfway),
                    0.0f
                );

                sunLightSpecularColor = Vector3.Min(
                    Vector3.Scale(sphereList[sphereIndex].SpecularCoefficients, sun.SpecularCoefficients)
                    *
                    Mathf.Pow(sunSpecularIntensity, sphereList[sphereIndex].SpecularShine),
                    new Vector3(1.0f, 1.0f, 1.0f)
                );
            }

            // ---------------------------------------------------------
            // (Specular) Sun Light
            // ---------------------------------------------------------
        }

        // ******************************************************************************************************************
        // (Ending) Sun
        // ******************************************************************************************************************

        // ******************************************************************************************************************
        // (Beginning) Light Bulb
        // ******************************************************************************************************************
        Vector3 lightBulbDiffuseColor = Vector3.zero;
        Vector3 lightBulbSpecularColor = Vector3.zero;

        Boolean doesPointSeeLightBulb = DoesPointSeeLightSource(hitPoint + pointToLightBulb * precisionBias, pointToLightBulb);
        if (doesPointSeeLightBulb)
        {

            float lightBulbIntensity = Mathf.Max(
                Vector3.Dot(normal, pointToLightBulb),
                0.0f
            );

            // ---------------------------------------------------------
            // (Diffuse) Light Bulb
            // ---------------------------------------------------------

            lightBulbDiffuseColor = Vector3.Min(
                Vector3.Scale(sphereList[sphereIndex].DiffuseCoefficients, lightBulb.DiffuseCoefficients) * lightBulbIntensity,
                new Vector3(1.0f, 1.0f, 1.0f)
            );

            // ---------------------------------------------------------
            // (Diffuse) Light Bulb
            // ---------------------------------------------------------

            // ---------------------------------------------------------
            // (Specular) Light Bulb
            // ---------------------------------------------------------

            // If the light bulb vector is perpindicular or not facing the surface normal, then there should be no specular lighting...
            if (lightBulbIntensity > 0.0f)
            {
                Vector3 lightBulbHalfway = (pointToEye + pointToLightBulb) / 2;
                lightBulbHalfway.Normalize();

                float lightBulbSpecularIntensity = Mathf.Max(
                    Vector3.Dot(normal, lightBulbHalfway),
                    0.0f
                );

                lightBulbSpecularColor = Vector3.Min(
                    Vector3.Scale(sphereList[sphereIndex].SpecularCoefficients, lightBulb.SpecularCoefficients)
                    *
                    Mathf.Pow(lightBulbSpecularIntensity, sphereList[sphereIndex].SpecularShine),
                    new Vector3(1.0f, 1.0f, 1.0f)
                );
            }

            // ---------------------------------------------------------
            // (Specular) Light Bulb
            // ---------------------------------------------------------
        }

        // ******************************************************************************************************************
        // (Ending) Light Bulb
        // ******************************************************************************************************************

        // *********************************************************************************************
        // *********************************************************************************************

        Vector3 diffuseColor = Vector3.Min(
            sunLightDiffuseColor + lightBulbDiffuseColor,
            new Vector3(1.0f, 1.0f, 1.0f)
        );

        Vector3 specularColor = Vector3.Min(
            sunLightSpecularColor + lightBulbSpecularColor,
            new Vector3(1.0f, 1.0f, 1.0f)
        );

        // *********************************************************************************************
        // *********************************************************************************************

        Vector3 computedColor = 
            ambientColor * (float)Convert.ToInt16(isAmbientLightingIncluded) + 
            diffuseColor * (float)Convert.ToInt16(isDiffuseLightingIncluded) + 
            specularColor * (float)Convert.ToInt16(isSpecularLightingIncluded);

        Vector3 totalColor = Vector3.Min(computedColor, new Vector3(1.0f, 1.0f, 1.0f));

        Vector4 pixelColor = new Color(
            totalColor.x,
            totalColor.y,
            totalColor.z,
            1.0f
        );

        return pixelColor;
    }

    private void Render()
    {
        for (int column = 0; column < nColumns; column++)
        {
            for (int row = 0; row < nRows; row++)
            {

                // -------------------------------------------------------------------
                // Formula for a ray:
                // r(t) = rayOrigin + rayDirection * t
                // -------------------------------------------------------------------

                // We need the primary ray's origin (which is the viewing eye), and we need to generate the ray's direction:
                Vector3 rayOrigin = eye;
                Vector3 rayDirection = GenerateRayDirection(row, column);

                // -------------------------------------------------------------------
                // Trace the generated ray, and return the color at which it hits its destination point:
                // -------------------------------------------------------------------
                int reflectionDepth = 0;
                Vector4 pixelColor = TraceRay(rayOrigin, rayDirection, reflectionDepth);
                // -------------------------------------------------------------------

                pixmap1D[nColumns * row + column].r = (byte)(pixelColor.x * 255);
                pixmap1D[nColumns * row + column].g = (byte)(pixelColor.y * 255);
                pixmap1D[nColumns * row + column].b = (byte)(pixelColor.z * 255);
                pixmap1D[nColumns * row + column].a = (byte)(pixelColor.w * 255);

            }
        }

        // Set the pixel colors of the pix map from the generated pixel buffer to draw the final ray-traced image...
        pixTex.SetPixels32(pixmap1D);
        pixTex.Apply();
        plane.GetComponent<MeshFilter>().gameObject.GetComponent<MeshRenderer>().material.mainTexture = pixTex;

    }

    void Start()
    {

 	    shader = Shader.Find("Unlit/Texture");  
   	   
		if (shader == null)
        {
			Debug.Log("shader not loaded");
		}
        else 
        {
			Debug.Log("shader loaded");
		}
 
    	currentMaterial = new Material(shader);

        // ********************************************
        
        SetObjects();

        SetLights();

        SetPerspectiveProjection();

        SetTexturePlane();

        Render();

    }


    void Update()
    {

    }

}