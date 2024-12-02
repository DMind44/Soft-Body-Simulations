using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoftbodyController : MonoBehaviour
{
    InputAction impulse;
    float value = 1;

    //TODO: Update ropes and sheets for OVD rather than OV
    public List<GameObject> outerVertices = new List<GameObject>();
    public List<VertexData> outerVertexData = new List<VertexData>();

    // Start is called before the first frame update
    void Start()
    {
        impulse = InputSystem.actions.FindAction("Impulse");
    }

    // Update is called once per frame
    void Update()
    {
        if (impulse.WasPressedThisFrame())
        {
            ImpulseSoftbody();
        }

        //Step 1: Calculate average global position of all child objects
        Vector3 averagePos = Vector3.zero;
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            averagePos += child.transform.position;
        }
        averagePos /= transform.childCount;

        //Get the difference between the positions
        Vector3 difference = transform.position - averagePos;

        //Update all objects so that parent is always roughly centered around children
        transform.position -= difference;

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.transform.position += difference;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.GetComponent<MeshRenderer>().forceRenderingOff = true;
        }

        //NOTE: Null checking to ensure no errors on frame 1
        if (GetComponent<MeshFilter>().mesh != null)
        {
            ResetMesh();
        }
    }

    public void ImpulseSoftbody()
    {
        Vector3 impulse = new Vector3(Random.value * 2 - 1, Random.value, Random.value * 2 - 1);
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.GetComponent<Rigidbody>().AddForce(impulse * value, ForceMode.Impulse);
        }
    }

    public void ImpulseSoftbody(float customValue)
    {
        Vector3 impulse = new Vector3(Random.value * 2 - 1, Random.value, Random.value * 2 - 1);
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.GetComponent<Rigidbody>().AddForce(impulse * customValue, ForceMode.Impulse);
        }
    }

    public void ResetMesh()
    {
        GetComponent<MeshFilter>().mesh = ConstructMesh();
    }

    Mesh ConstructMesh()
    {
        //TODO: MAJOR BUG: RECTS NOT ROTATING TO PROPERLY FACE ROTATIONS - likely due to missing rotational component, can fix 


        Mesh mesh = new Mesh();

        //IMPORTANT: CURRENT IMPLEMENTATION CAN BE INEFFICIENT DUE TO FLOATING POINT IMPRECISION
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();

        List<Vector3> vertexList = new List<Vector3>();

        for (int i = 0; i < outerVertexData.Count; i++)
        {
            GameObject outerVertex = outerVertexData[i].gameObject;
            List<bool> directionList = outerVertexData[i].directionList;

            if (directionList[0]) //i == 0 aka bottom face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    if (!vertexList.Contains(faceVert))
                    {
                        vertices.Add(faceVert);
                        //NOTE: We dummy out normals here, then recalculate once complete
                        normals.Add(Vector3.zero);
                        //NOTE: Until we add textures, uvs are also dummied out
                        uv.Add(Vector2.zero);
                        vertexList.Add(faceVert);
                    }
                    faceVertIndexes.Add(vertexList.IndexOf(faceVert));
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[2], faceVertIndexes[0] });
                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[3], faceVertIndexes[2] });

            }

            if (directionList[3]) //i == max aka top face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    if (!vertexList.Contains(faceVert))
                    {
                        vertices.Add(faceVert);
                        //NOTE: We dummy out normals here, then recalculate once complete
                        normals.Add(Vector3.zero);
                        //NOTE: Until we add textures, uvs are also dummied out
                        uv.Add(Vector2.zero);
                        vertexList.Add(faceVert);
                    }
                    faceVertIndexes.Add(vertexList.IndexOf(faceVert));
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[0], faceVertIndexes[2], faceVertIndexes[1] });
                triangles.AddRange(new List<int>() { faceVertIndexes[2], faceVertIndexes[3], faceVertIndexes[1] });

            }

            if (directionList[1]) //j == 0 aka front face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    if (!vertexList.Contains(faceVert))
                    {
                        vertices.Add(faceVert);
                        //NOTE: We dummy out normals here, then recalculate once complete
                        normals.Add(Vector3.zero);
                        //NOTE: Until we add textures, uvs are also dummied out
                        uv.Add(Vector2.zero);
                        vertexList.Add(faceVert);
                    }
                    faceVertIndexes.Add(vertexList.IndexOf(faceVert));
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[2], faceVertIndexes[0] });
                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[3], faceVertIndexes[2] });

            }

            if (directionList[4]) //j == max aka back face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    if (!vertexList.Contains(faceVert))
                    {
                        vertices.Add(faceVert);
                        //NOTE: We dummy out normals here, then recalculate once complete
                        normals.Add(Vector3.zero);
                        //NOTE: Until we add textures, uvs are also dummied out
                        uv.Add(Vector2.zero);
                        vertexList.Add(faceVert);
                    }
                    faceVertIndexes.Add(vertexList.IndexOf(faceVert));
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[0], faceVertIndexes[2], faceVertIndexes[1] });
                triangles.AddRange(new List<int>() { faceVertIndexes[2], faceVertIndexes[3], faceVertIndexes[1] });

            }

            if (directionList[2]) //k == 0 aka left face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    if (!vertexList.Contains(faceVert))
                    {
                        vertices.Add(faceVert);
                        //NOTE: We dummy out normals here, then recalculate once complete
                        normals.Add(Vector3.zero);
                        //NOTE: Until we add textures, uvs are also dummied out
                        uv.Add(Vector2.zero);
                        vertexList.Add(faceVert);
                    }
                    faceVertIndexes.Add(vertexList.IndexOf(faceVert));
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[2], faceVertIndexes[0] });
                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[3], faceVertIndexes[2] });

            }

            if (directionList[5]) //k == max aka right face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    if (!vertexList.Contains(faceVert))
                    {
                        vertices.Add(faceVert);
                        //NOTE: We dummy out normals here, then recalculate once complete
                        normals.Add(Vector3.zero);
                        //NOTE: Until we add textures, uvs are also dummied out
                        uv.Add(Vector2.zero);
                        vertexList.Add(faceVert);
                    }
                    faceVertIndexes.Add(vertexList.IndexOf(faceVert));
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[0], faceVertIndexes[2], faceVertIndexes[1] });
                triangles.AddRange(new List<int>() { faceVertIndexes[2], faceVertIndexes[3], faceVertIndexes[1] });

            }
        }

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }
}

public class Triangle {
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> order = new List<int>();
    public List<Vector3> normals = new List<Vector3>();
    //Not using textures so skip uvs - dummy out when constructing mesh
}

public class VertexData
{
    public GameObject gameObject;
    public List<bool> directionList;

    public VertexData(GameObject gameObject, List<bool> directionList)
    {
        this.gameObject = gameObject;
        this.directionList = directionList;
    }
}