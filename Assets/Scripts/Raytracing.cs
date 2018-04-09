using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum RaytracingMode
{
    RandomColors,
    ResolutionRate
}

public class Raytracing : MonoBehaviour
{
    public RaytracingMode raytracingMode;

    public GameObject target;
    public RenderTexture result;

    public int pixelWidth, pixelHeight;
    public int textureWidth, textureHeight;

    public Color backgroundColor;
    public Color minColor;
    public Color maxColor;

    private List<Color> triangleColors;

    public float meanLower, meanUpper;

    public float[] pixelTextureArea;
    public int[] pixelResolutionByTriangle;
    public float[] pixelResolutionRate;
    public float[] pixelResolutionRateNormalized;

    [HideInInspector]
    public string filePath;

    public void RaytraceUsingRandomColors()
    {
        double initTime = EditorApplication.timeSinceStartup;

        Mesh mesh = target.GetComponent<MeshFilter>().sharedMesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        int totalTriangles = triangles.Length / 3;
        triangleColors = new List<Color>();
        for (int i = 0; i < totalTriangles; i++)
        {
            triangleColors.Add(
                new Color(Random.Range(0, 255)*1.0f/255f, 
                          Random.Range(0, 255)*1.0f/255f, 
                          Random.Range(0, 255)*1.0f/255f, 
                          1f)
                );
        }   

        Camera camera = Camera.main;

        Vector3 cameraPos = camera.transform.position;
        Vector3 nearplanePos = cameraPos + camera.transform.forward * camera.nearClipPlane;

        float heightSize = 2f * camera.nearClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.PI / 180f);
        float widthSize = (pixelWidth * 1.0f / pixelHeight) * heightSize;

        Debug.Log("GilLog - Raytracing::Raytrace - heightSize " + heightSize + "  - widthSize " + widthSize + " ");

        Texture2D raytracedTexture = new Texture2D(pixelWidth, pixelHeight);
        // result = new RenderTexture(pixelWidth, pixelHeight, 24, RenderTextureFormat.ARGB32);

        Ray ray;

        for (int x = 0; x < pixelWidth; x++)
        {
            for (int y = 0; y < pixelHeight; y++)
            {
                Vector3 pixelPos = ((x - 0.5f * pixelWidth)/pixelWidth) * widthSize * camera.transform.right +
                    ((y - 0.5f * pixelHeight)/pixelHeight) * heightSize * camera.transform.up +
                    nearplanePos;

                ray = new Ray(cameraPos, pixelPos - cameraPos);

                Color pixelColor = backgroundColor;

                for (int i = 0; i < totalTriangles; i++)
                {
                    Vector3 v1 = vertices[triangles[3*i]];
                    Vector3 v2 = vertices[triangles[3*i + 1]];
                    Vector3 v3 = vertices[triangles[3*i + 2]];

                    v1 = target.transform.TransformPoint(v1);
                    v2 = target.transform.TransformPoint(v2);
                    v3 = target.transform.TransformPoint(v3);

                    if (Intersect(v1, v2, v3, ray)) {
                        pixelColor = triangleColors[i];
                        break;
                    }
                }

                raytracedTexture.SetPixel(x,y, pixelColor);
            }
        }
        raytracedTexture.Apply();

        byte[] textureBytes = raytracedTexture.EncodeToJPG();
        System.IO.File.WriteAllBytes(filePath, textureBytes);

        Graphics.Blit(raytracedTexture, result);

        Debug.Log("GilLog - Raytracing::Raytrace - elapsed: " + (EditorApplication.timeSinceStartup - initTime));
    }

    public void RaytraceForResolutionRate()
    {
        double initTime = EditorApplication.timeSinceStartup;

        Mesh mesh = target.GetComponent<MeshFilter>().sharedMesh;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        Vector2[] uvs = mesh.uv;

        for (int v = 0; v < vertices.Length; v++)
        {
            vertices[v] = target.transform.TransformPoint(vertices[v]);
        }

        int totalTriangles = triangles.Length / 3;
        triangleColors = new List<Color>();
        for (int i = 0; i < totalTriangles; i++)
        {
            triangleColors.Add(
                new Color(Random.Range(0, 255)*1.0f/255f, 
                          Random.Range(0, 255)*1.0f/255f, 
                          Random.Range(0, 255)*1.0f/255f, 
                          1f)
                );
        }

        pixelResolutionRate = new float[totalTriangles];
        pixelResolutionRateNormalized = new float[totalTriangles];
        pixelResolutionByTriangle = new int[totalTriangles];
        for (int i = 0; i < totalTriangles; i++)
        {
            pixelResolutionByTriangle[i] = 0;
        }

        Vector3 size = new Vector3(textureWidth * 1.0f, textureHeight * 1.0f, 0f);

        pixelTextureArea = new float[totalTriangles];
        for (int i = 0; i < totalTriangles; i++)
        {
            Vector2 uv1 = uvs[triangles[3*i]];
            Vector2 uv2 = uvs[triangles[3*i + 1]];
            Vector2 uv3 = uvs[triangles[3*i + 2]];

            Vector3 va = new Vector3(uv2.x - uv1.x, uv2.y - uv1.y, 0f);
            Vector3 vb = new Vector3(uv3.x - uv1.x, uv3.y - uv1.y, 0f);

            va = Vector3.Scale(va, size);
            vb = Vector3.Scale(vb, size);

            pixelTextureArea[i] = 0.5f * Vector3.Cross(va, vb).magnitude;
        }

        Camera camera = Camera.main;

        Vector3 cameraPos = camera.transform.position;
        Vector3 nearplanePos = cameraPos + camera.transform.forward * camera.nearClipPlane;

        float heightSize = 2f * camera.nearClipPlane * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.PI / 180f);
        float widthSize = (pixelWidth * 1.0f / pixelHeight) * heightSize;

        Debug.Log("GilLog - Raytracing::Raytrace - heightSize " + heightSize + "  - widthSize " + widthSize + " ");

        Texture2D raytracedTexture = new Texture2D(pixelWidth, pixelHeight);
        // result = new RenderTexture(pixelWidth, pixelHeight, 24, RenderTextureFormat.ARGB32);

        Ray ray;

        for (int x = 0; x < pixelWidth; x++)
        {
            for (int y = 0; y < pixelHeight; y++)
            {
                Vector3 pixelPos = ((x - 0.5f * pixelWidth)/pixelWidth) * widthSize * camera.transform.right +
                    ((y - 0.5f * pixelHeight)/pixelHeight) * heightSize * camera.transform.up +
                    nearplanePos;

                ray = new Ray(cameraPos, pixelPos - cameraPos);

                for (int i = 0; i < totalTriangles; i++)
                {
                    Vector3 v1 = vertices[triangles[3*i]];
                    Vector3 v2 = vertices[triangles[3*i + 1]];
                    Vector3 v3 = vertices[triangles[3*i + 2]];

                    if (Intersect(v1, v2, v3, ray)) {
                        pixelResolutionByTriangle[i] = pixelResolutionByTriangle[i] + 1;
                        break;
                    }
                }
            }
        }

        float minLower = 1, maxLower = float.MinValue;
        float minUpper = float.MaxValue, maxUpper = 1;

        float meanLowerLower = 0f, meanLowerUpper = 0f;
        int totalLowerLower = 0, totalLowerUpper = 0; 

        int totalUpper = 0, totalLower = 0;

        // Mean Lower 0.3344944

        for (int i = 0; i < totalTriangles; i++)
        {
            if (pixelResolutionByTriangle[i] != 0f)
            {
                pixelResolutionRate[i] = pixelTextureArea[i]/pixelResolutionByTriangle[i];
                if (pixelResolutionRate[i] <= 1f)
               {
                    if (pixelResolutionRate[i] < 0.3344944f)
                    {
                        meanLowerLower += pixelResolutionRate[i];
                        totalLowerLower++;

                    } else {
                        meanLowerUpper += pixelResolutionRate[i];
                        totalLowerUpper++;
                    }
                    
                    totalLower++;

                    if (pixelResolutionRate[i] < minLower)
                    {
                        minLower = pixelResolutionRate[i];
                    }

                    if (pixelResolutionRate[i] > maxLower)
                    {
                        maxLower = pixelResolutionRate[i];
                    }
               } else {
                    meanUpper += pixelResolutionRate[i];
                    totalUpper++;

                    if (pixelResolutionRate[i] < minUpper)
                    {
                        minUpper = pixelResolutionRate[i];
                    }

                    if (pixelResolutionRate[i] > maxUpper)
                    {
                        maxUpper = pixelResolutionRate[i];
                    }
               }
            }
        }

        meanLower = 0.3344944f;

        meanLowerLower = meanLowerLower / totalLowerLower;
        meanLowerUpper = meanLowerUpper / totalLowerUpper;

        meanUpper = meanUpper / totalUpper;

        float lowerInterval = 0.2f;
        float upperInterval = 0.8f;

        for (int i = 0; i < totalTriangles; i++)
        {
            if (pixelResolutionByTriangle[i] == 0)
            {
                pixelResolutionRateNormalized[i] = 0f;
            } else {
                if (pixelResolutionRateNormalized[i] <= meanLowerLower)
                {
                    pixelResolutionRateNormalized[i] = 0.25f*lowerInterval*(pixelResolutionRate[i] - minLower)/(meanLowerLower - minLower);
                } else if (pixelResolutionRateNormalized[i] <= meanLower) 
                {
                    pixelResolutionRateNormalized[i] = 0.25f*lowerInterval + 0.25f*lowerInterval*(pixelResolutionRate[i] - meanLowerLower)/(meanLower - meanLowerLower);
                } else if (pixelResolutionRateNormalized[i] <= meanLowerUpper)
                {
                    pixelResolutionRateNormalized[i] = 0.5f*lowerInterval + 0.25f*lowerInterval*(pixelResolutionRate[i] - meanLower)/(meanLowerUpper - meanLower);
                } else if (pixelResolutionRateNormalized[i] <= 1f) 
                {
                    pixelResolutionRateNormalized[i] = 0.75f*lowerInterval + 0.25f*lowerInterval*(pixelResolutionRate[i] - meanLowerUpper)/(maxLower - meanLowerUpper);
                } else if (pixelResolutionRateNormalized[i] <= meanUpper)
                {
                    pixelResolutionRateNormalized[i] = lowerInterval + 0.5f*upperInterval*(pixelResolutionRate[i] - minUpper)/(meanUpper - minUpper);
                } else {
                    pixelResolutionRateNormalized[i] = lowerInterval + 0.5f*upperInterval + 
                        0.5f*upperInterval*(pixelResolutionRate[i] - meanUpper)/(maxUpper - meanUpper);
                }
            }       
        }

        for (int x = 0; x < pixelWidth; x++)
        {
            for (int y = 0; y < pixelHeight; y++)
            {
                Vector3 pixelPos = ((x - 0.5f * pixelWidth)/pixelWidth) * widthSize * camera.transform.right +
                    ((y - 0.5f * pixelHeight)/pixelHeight) * heightSize * camera.transform.up +
                    nearplanePos;

                ray = new Ray(cameraPos, pixelPos - cameraPos);

                Color pixelColor = backgroundColor;

                for (int i = 0; i < totalTriangles; i++)
                {
                    Vector3 v1 = vertices[triangles[3*i]];
                    Vector3 v2 = vertices[triangles[3*i + 1]];
                    Vector3 v3 = vertices[triangles[3*i + 2]];

                    if (Intersect(v1, v2, v3, ray)) {
                        pixelColor = Color.Lerp(minColor, maxColor, pixelResolutionRateNormalized[i]);
                        break;
                    }
                }

                raytracedTexture.SetPixel(x,y, pixelColor);
            }
        }
        raytracedTexture.Apply();

        byte[] textureBytes = raytracedTexture.EncodeToJPG();
        System.IO.File.WriteAllBytes(filePath, textureBytes);

        Graphics.Blit(raytracedTexture, result);

        Debug.Log("GilLog - Raytracing::Raytrace - elapsed: " + (EditorApplication.timeSinceStartup - initTime));
    }

    /// <summary>
     /// Checks if the specified ray hits the triagnlge descibed by p1, p2 and p3.
     /// Möller–Trumbore ray-triangle intersection algorithm implementation.
     /// </summary>
     /// <param name="p1">Vertex 1 of the triangle.</param>
     /// <param name="p2">Vertex 2 of the triangle.</param>
     /// <param name="p3">Vertex 3 of the triangle.</param>
     /// <param name="ray">The ray to test hit for.</param>
     /// <returns><c>true</c> when the ray hits the triangle, otherwise <c>false</c></returns>
     public static bool Intersect(Vector3 p1, Vector3 p2, Vector3 p3, Ray ray)
     {
         // Vectors from p1 to p2/p3 (edges)
         Vector3 e1, e2;  
 
         Vector3 p, q, t;
         float det, invDet, u, v;
 
 
         //Find vectors for two edges sharing vertex/point p1
         e1 = p2 - p1;
         e2 = p3 - p1;
 
         // calculating determinant 
         p = Vector3.Cross(ray.direction, e2);
 
         //Calculate determinat
         det = Vector3.Dot(e1, p);
 
         //if determinant is near zero, ray lies in plane of triangle otherwise not
         if (det > -Mathf.Epsilon && det < Mathf.Epsilon) { return false; }
         invDet = 1.0f / det;
 
         //calculate distance from p1 to ray origin
         t = ray.origin - p1;
 
         //Calculate u parameter
         u = Vector3.Dot(t, p) * invDet;
 
         //Check for ray hit
         if (u < 0 || u > 1) { return false; }
 
         //Prepare to test v parameter
         q = Vector3.Cross(t, e1);
 
         //Calculate v parameter
         v = Vector3.Dot(ray.direction, q) * invDet;
 
         //Check for ray hit
         if (v < 0 || u + v > 1) { return false; }
 
         if ((Vector3.Dot(e2, q) * invDet) > Mathf.Epsilon)
         { 
             //ray does intersect
             return true;
         }
 
         // No hit at all
         return false;
     }

     public void SaveJPGImage()
     {
        Camera cameraComponent = Camera.main;

        Texture2D screenShot = new Texture2D(pixelWidth, pixelHeight, TextureFormat.RGB24, false); //Create new texture
        RenderTexture.active = result;
        screenShot.ReadPixels(new Rect(0, 0, pixelWidth, pixelHeight), 0, 0); //Apply pixels from camera onto Texture2D
        byte[] textureBytes = screenShot.EncodeToJPG();
        System.IO.File.WriteAllBytes(filePath, textureBytes);
     }
}

#if UNITY_EDITOR
[CustomEditor(typeof(Raytracing))]
public class RaytracingEditor : Editor {

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
    
        Raytracing editorObj = target as Raytracing;
    
        if (editorObj == null) return;

        if (GUILayout.Button("Raytrace"))
        {
            if (editorObj.raytracingMode == RaytracingMode.RandomColors)
            {
                editorObj.RaytraceUsingRandomColors();
            } else {
                editorObj.RaytraceForResolutionRate();
            }   
            
        }

        editorObj.filePath = EditorGUILayout.TextField("Path:", editorObj.filePath);

        if (GUILayout.Button("Save JPG"))
        {
            editorObj.SaveJPGImage();
        }
    }

}
#endif