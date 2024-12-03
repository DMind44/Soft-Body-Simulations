using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoftbodyController : MonoBehaviour
{
    InputAction impulse;
    float value = 1;

    public List<VertexData> outerVertexData = new List<VertexData>();

    Dictionary<int, int> mergedVertices = new Dictionary<int, int>();
    Dictionary<int, List<int>> mergedVertexSets = new Dictionary<int, List<int>>();
    List<int> mergedVertexIndices = new List<int>();
    List<List<int>> mergedVertexGroups = new List<List<int>>();

    //Threshold for points being close enough
    float threshold = 0.1f;

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

    public void ImpulseSoftbody(Vector3 impulse)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            child.GetComponent<Rigidbody>().AddForce(impulse, ForceMode.Impulse);
        }
    }

    public void ResetMesh()
    {
        GetComponent<MeshFilter>().mesh = ConstructMesh(false);
    }

    public void ConstructMesh()
    {
        GetComponent<MeshFilter>().mesh = ConstructMesh(true);
    }

    Mesh ConstructMesh(bool initial)
    {
        //TODO: Add vector merging to remove gaps in softbody 
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
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    vertices.Add(faceVert);
                    //NOTE: We dummy out normals here, then recalculate once complete
                    normals.Add(Vector3.zero);
                    //NOTE: Until we add textures, uvs are also dummied out
                    uv.Add(Vector2.zero);
                    vertexList.Add(faceVert);

                    int vertIndex = vertexList.Count - 1;

                    faceVertIndexes.Add(vertIndex);

                    if (initial)
                    {
                        //Setup for merge
                        KeyValuePair<Vector3, int> closeVertexData = CloseToPoint(vertexList, faceVert, vertIndex);
                        Vector3 closeVertex = closeVertexData.Key;
                        if (closeVertex != Vector3.negativeInfinity)
                        {
                            int closeIndex = closeVertexData.Value;

                            List<int> mergeList = FindInSublist(mergedVertexGroups, closeIndex);
                            if (mergeList != new List<int>())
                            {
                                mergeList.Add(vertIndex);
                            }
                            else
                            {
                                mergedVertexGroups.Add(new List<int>() { closeIndex, vertIndex });
                            }
                        }
                        else
                        {
                            mergedVertexGroups.Add(new List<int>() { vertIndex });
                        }
                    }
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[2], faceVertIndexes[0] });
                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[3], faceVertIndexes[2] });

            }

            if (directionList[3]) //i == max aka top face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    vertices.Add(faceVert);
                    //NOTE: We dummy out normals here, then recalculate once complete
                    normals.Add(Vector3.zero);
                    //NOTE: Until we add textures, uvs are also dummied out
                    uv.Add(Vector2.zero);
                    vertexList.Add(faceVert);

                    int vertIndex = vertexList.Count - 1;

                    faceVertIndexes.Add(vertIndex);

                    if (initial)
                    {
                        //Setup for merge
                        KeyValuePair<Vector3, int> closeVertexData = CloseToPoint(vertexList, faceVert, vertIndex);
                        Vector3 closeVertex = closeVertexData.Key;
                        if (closeVertex != Vector3.negativeInfinity)
                        {
                            int closeIndex = closeVertexData.Value;

                            List<int> mergeList = FindInSublist(mergedVertexGroups, closeIndex);
                            if (mergeList != new List<int>())
                            {
                                mergeList.Add(vertIndex);
                            }
                            else
                            {
                                mergedVertexGroups.Add(new List<int>() { closeIndex, vertIndex });
                            }
                        }
                        else
                        {
                            mergedVertexGroups.Add(new List<int>() { vertIndex });
                        }
                    }
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[0], faceVertIndexes[2], faceVertIndexes[1] });
                triangles.AddRange(new List<int>() { faceVertIndexes[2], faceVertIndexes[3], faceVertIndexes[1] });

            }

            if (directionList[1]) //j == 0 aka front face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    vertices.Add(faceVert);
                    //NOTE: We dummy out normals here, then recalculate once complete
                    normals.Add(Vector3.zero);
                    //NOTE: Until we add textures, uvs are also dummied out
                    uv.Add(Vector2.zero);
                    vertexList.Add(faceVert);

                    int vertIndex = vertexList.Count - 1;

                    faceVertIndexes.Add(vertIndex);

                    if (initial)
                    {
                        //Setup for merge
                        KeyValuePair<Vector3, int> closeVertexData = CloseToPoint(vertexList, faceVert, vertIndex);
                        Vector3 closeVertex = closeVertexData.Key;
                        if (closeVertex != Vector3.negativeInfinity)
                        {
                            int closeIndex = closeVertexData.Value;

                            List<int> mergeList = FindInSublist(mergedVertexGroups, closeIndex);
                            if (mergeList != new List<int>())
                            {
                                mergeList.Add(vertIndex);
                            }
                            else
                            {
                                mergedVertexGroups.Add(new List<int>() { closeIndex, vertIndex });
                            }
                        }
                        else
                        {
                            mergedVertexGroups.Add(new List<int>() { vertIndex });
                        }
                    }
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[2], faceVertIndexes[0] });
                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[3], faceVertIndexes[2] });

            }

            if (directionList[4]) //j == max aka back face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    vertices.Add(faceVert);
                    //NOTE: We dummy out normals here, then recalculate once complete
                    normals.Add(Vector3.zero);
                    //NOTE: Until we add textures, uvs are also dummied out
                    uv.Add(Vector2.zero);
                    vertexList.Add(faceVert);

                    int vertIndex = vertexList.Count - 1;

                    faceVertIndexes.Add(vertIndex);

                    if (initial)
                    {
                        //Setup for merge
                        KeyValuePair<Vector3, int> closeVertexData = CloseToPoint(vertexList, faceVert, vertIndex);
                        Vector3 closeVertex = closeVertexData.Key;
                        if (closeVertex != Vector3.negativeInfinity)
                        {
                            int closeIndex = closeVertexData.Value;

                            List<int> mergeList = FindInSublist(mergedVertexGroups, closeIndex);
                            if (mergeList != new List<int>())
                            {
                                mergeList.Add(vertIndex);
                            }
                            else
                            {
                                mergedVertexGroups.Add(new List<int>() { closeIndex, vertIndex });
                            }
                        }
                        else
                        {
                            mergedVertexGroups.Add(new List<int>() { vertIndex });
                        }
                    }
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[0], faceVertIndexes[2], faceVertIndexes[1] });
                triangles.AddRange(new List<int>() { faceVertIndexes[2], faceVertIndexes[3], faceVertIndexes[1] });

            }

            if (directionList[2]) //k == 0 aka left face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(-outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    vertices.Add(faceVert);
                    //NOTE: We dummy out normals here, then recalculate once complete
                    normals.Add(Vector3.zero);
                    //NOTE: Until we add textures, uvs are also dummied out
                    uv.Add(Vector2.zero);
                    vertexList.Add(faceVert);

                    int vertIndex = vertexList.Count - 1;

                    faceVertIndexes.Add(vertIndex);

                    if (initial)
                    {
                        //Setup for merge
                        KeyValuePair<Vector3, int> closeVertexData = CloseToPoint(vertexList, faceVert, vertIndex);
                        Vector3 closeVertex = closeVertexData.Key;
                        if (closeVertex != Vector3.negativeInfinity)
                        {
                            int closeIndex = closeVertexData.Value;

                            List<int> mergeList = FindInSublist(mergedVertexGroups, closeIndex);
                            if (mergeList != new List<int>())
                            {
                                mergeList.Add(vertIndex);
                            }
                            else
                            {
                                mergedVertexGroups.Add(new List<int>() { closeIndex, vertIndex });
                            }
                        }
                        else
                        {
                            mergedVertexGroups.Add(new List<int>() { vertIndex });
                        }
                    }
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[2], faceVertIndexes[0] });
                triangles.AddRange(new List<int>() { faceVertIndexes[1], faceVertIndexes[3], faceVertIndexes[2] });

            }

            if (directionList[5]) //k == max aka right face
            {
                List<Vector3> faceVerts = new List<Vector3>();
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, outerVertex.transform.localScale.z / 2));
                faceVerts.Add(outerVertex.transform.localPosition + outerVertex.transform.rotation * new Vector3(outerVertex.transform.localScale.x / 2, -outerVertex.transform.localScale.y / 2, -outerVertex.transform.localScale.z / 2));

                List<int> faceVertIndexes = new List<int>();

                for (int j = 0; j < faceVerts.Count; j++)
                {
                    Vector3 faceVert = faceVerts[j];
                    vertices.Add(faceVert);
                    //NOTE: We dummy out normals here, then recalculate once complete
                    normals.Add(Vector3.zero);
                    //NOTE: Until we add textures, uvs are also dummied out
                    uv.Add(Vector2.zero);
                    vertexList.Add(faceVert);

                    int vertIndex = vertexList.Count - 1;

                    faceVertIndexes.Add(vertIndex);

                    if (initial)
                    {
                        //Setup for merge
                        KeyValuePair<Vector3, int> closeVertexData = CloseToPoint(vertexList, faceVert, vertIndex);
                        Vector3 closeVertex = closeVertexData.Key;
                        if (closeVertex != Vector3.negativeInfinity)
                        {
                            int closeIndex = closeVertexData.Value;

                            List<int> mergeList = FindInSublist(mergedVertexGroups, closeIndex);
                            if (mergeList != new List<int>())
                            {
                                mergeList.Add(vertIndex);
                            }
                            else
                            {
                                mergedVertexGroups.Add(new List<int>() { closeIndex, vertIndex });
                            }
                        }
                        else
                        {
                            mergedVertexGroups.Add(new List<int>() { vertIndex });
                        }
                    }
                }

                triangles.AddRange(new List<int>() { faceVertIndexes[0], faceVertIndexes[2], faceVertIndexes[1] });
                triangles.AddRange(new List<int>() { faceVertIndexes[2], faceVertIndexes[3], faceVertIndexes[1] });

            }
        }
        //TODO: Figure out why merging is not working?!?!?!
        if (initial)
        {
            //Post process for merged vertices
            for (int i = 0; i < mergedVertexGroups.Count; i++)
            {
                //Find the average vertex position of all vertices
                List<int> activeGroup = mergedVertexGroups[i];
                Vector3 averageVertex = new Vector3();
                for (int j = 0; j < activeGroup.Count; j++)
                {
                    averageVertex += vertices[activeGroup[j]];
                }
                averageVertex /= activeGroup.Count;

                vertices.Add(averageVertex);
                normals.Add(Vector3.zero);
                uv.Add(Vector2.zero);
                int index = vertices.Count - 1;

                mergedVertexIndices.Add(index);
                mergedVertexSets[index] = activeGroup;
                for (int j = 0; j < activeGroup.Count; j++)
                {
                    mergedVertices[activeGroup[j]] = index;
                }

                //Now replace the triangle references to old vertices with the new ones.
                for (int j = 0; j < triangles.Count; j++)
                {
                    int subIndex = triangles[j];
                    if (mergedVertices.ContainsKey(subIndex))
                    {
                        triangles[j] = mergedVertices[subIndex];
                    }
                }
            }
        }
        else
        {
            //Update the merged vertices to new averages
            for (int i = 0; i < mergedVertexIndices.Count; i++)
            {
                int index = mergedVertexIndices[i];

                List<int> mergeTargets = mergedVertexSets[index];

                Vector3 averageVertex = new Vector3();
                for (int j = 0; j < mergeTargets.Count; j++)
                {
                    averageVertex += vertices[mergeTargets[j]];
                }
                averageVertex /= mergeTargets.Count;

                vertices[index] = averageVertex;
            }
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.normals = normals.ToArray();
        mesh.uv = uv.ToArray();

        mesh.RecalculateNormals();

        return mesh;
    }

    KeyValuePair<Vector3,int> CloseToPoint(List<Vector3> list, Vector3 target, int skipIndex)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (i == skipIndex)
            {
                continue;
            }
            if ((target - list[i]).magnitude < threshold)
            {
                return new KeyValuePair<Vector3, int>( list[i], i );
            }
        }
        return new KeyValuePair<Vector3, int>(Vector3.negativeInfinity, -1);
    }

    List<int> FindInSublist(List<List<int>> list, int target)
    {
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].Contains(target))
            {
                return list[i];
            }
        }
        return new List<int>();
    }
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