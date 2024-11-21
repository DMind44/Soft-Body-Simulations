using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerController : MonoBehaviour
{
    [SerializeField]
    GameObject softbodyBasePrefab;

    [SerializeField]
    GameObject softbodyVertexPrefab;

    float pointMass = 0.1f;

    enum SoftbodyType
    {
        ROPE, //1 element, length
        SHEET, //2 elements, width x height
        CUBE, //3 elements, length x width x height
        SPHERE, //1 element, radius
        COMPOSITE //COMPLEX, DO LATER
    }

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(DelayedSpawn());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //Spawn Logic:
    //Resolution = max number of divisions for longest dimension
    //Resolution = 2^resolution - BE CAREFUL
    //TODO: Add error checking for misaligned dimensions
    GameObject SpawnSoftbody(SoftbodyType softbodyType, float[] dimensions, int resolution)
    {
        //Create softbody base
        GameObject softbodyBase = Instantiate(softbodyBasePrefab, transform.position, transform.rotation);

        //Setup the arrays for holding verts
        //Find dimensions based on resolution

        //NOTE: Softbodies are instantiated at corners
        //NOTE: We'll have a semi-square softbody - if two points are > the size of 1 segment length but < 2 segment lengths
        //      we'll do an even division between them
        //TODO: Determine whether to do even rectangular vertex mesh or square+ vertex mesh
        //      For the time being, we'll use an even rect mesh

        switch (softbodyType)
        {
            case SoftbodyType.ROPE:
                {
                    ArrayList vertices = GenerateRope(softbodyBase, resolution, dimensions, Vector3.zero);

                    //Now glue the rope together using spring joints
                    //Skip the first point - no gluing will be done
                    for (int i = 1; i < vertices.Count; i++)
                    {
                        GameObject vertex = (GameObject)vertices[i];
                        GameObject previousVertex = (GameObject)vertices[i - 1];

                        //NOTE: Should be safe - but we should be careful as this will lead to multiple spring joings per vertex
                        //Must be careful when using GetComponent on SpringJoints
                        SpringJoint vertexSpring = vertex.AddComponent<SpringJoint>();
                        SpringJoint previousSpring = previousVertex.AddComponent<SpringJoint>();

                        vertexSpring.connectedBody = previousVertex.GetComponent<Rigidbody>();
                        previousSpring.connectedBody = vertex.GetComponent<Rigidbody>();
                        //Just increasing the spring power for a more taut softbody
                        vertexSpring.spring = 50;
                        previousSpring.spring = 50;

                        //TEST: Will changing the tolerance make it more rope-like?
                        vertexSpring.tolerance = 0;
                        previousSpring.tolerance = 0;
                    }
                }
                //TODO: Add mesh rendering - probably a cylinder or something around the verts
                break;
            case SoftbodyType.SHEET:
                {
                    if (dimensions[0] == 0 || dimensions[1] == 0)
                    {
                        Debug.LogError("Cannot do ultra-narrow sheets. Use a rope instead.");
                    }

                    ArrayList vertices = GeneratePlane(softbodyBase, resolution, dimensions, Vector3.zero);

                    //Now that we have a 2d array of vertices in order, we can glue the softbody together.
                    {
                        //Syntax Note:
                        //Previous = same arraylist, -1 index
                        //Above = -1 arraylist, same index

                        //Now glue the first rope together using spring joints
                        //DO NOT SKIP THE FIRST ROPE - MUST STILL BE GLUED
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            //DO NOT SKIP THE FIRST POINT - MUST STILL BE GLUED
                            for (int j = 0; j < ((ArrayList)vertices[i]).Count; j++)
                            {
                                //ONLY FULLY SKIP THE VERY FIRST ONE - SELECTIVELY SKIP THE OTHERS
                                GameObject vertex = (GameObject)((ArrayList)vertices[i])[j];
                                //TEST: Will reducing the mass create a better effect?
                                vertex.GetComponent<Rigidbody>().mass = pointMass;

                                //Glue to previous vertex
                                if (j > 0)
                                {
                                    GameObject previousVertex = (GameObject)((ArrayList)vertices[i])[j - 1];

                                    //NOTE: Should be safe - but we should be careful as this will lead to multiple spring joings per vertex
                                    //Must be careful when using GetComponent on SpringJoints
                                    SpringJoint vertexSpring = vertex.AddComponent<SpringJoint>();
                                    SpringJoint previousSpring = previousVertex.AddComponent<SpringJoint>();

                                    vertexSpring.connectedBody = previousVertex.GetComponent<Rigidbody>();
                                    previousSpring.connectedBody = vertex.GetComponent<Rigidbody>();
                                    //Just increasing the spring power for a more taut softbody
                                    vertexSpring.spring = 50;
                                    previousSpring.spring = 50;

                                    //TEST: Will changing the tolerance make it more rope-like?
                                    vertexSpring.tolerance = 0;
                                    previousSpring.tolerance = 0;

                                    //TEST: Will reducing the mass create a better effect?
                                    previousVertex.GetComponent<Rigidbody>().mass = pointMass;
                                }

                                //Glue to vertex above
                                if (i > 0)
                                {
                                    GameObject aboveVertex = (GameObject)((ArrayList)vertices[i - 1])[j];

                                    //NOTE: Should be safe - but we should be careful as this will lead to multiple spring joings per vertex
                                    //Must be careful when using GetComponent on SpringJoints
                                    SpringJoint vertexSpring = vertex.AddComponent<SpringJoint>();
                                    SpringJoint aboveSpring = aboveVertex.AddComponent<SpringJoint>();

                                    vertexSpring.connectedBody = aboveVertex.GetComponent<Rigidbody>();
                                    aboveSpring.connectedBody = vertex.GetComponent<Rigidbody>();
                                    //Just increasing the spring power for a more taut softbody
                                    vertexSpring.spring = 50;
                                    aboveSpring.spring = 50;

                                    //TEST: Will changing the tolerance make it more rope-like?
                                    vertexSpring.tolerance = 0;
                                    aboveSpring.tolerance = 0;

                                    //TEST: Will reducing the mass create a better effect?
                                    aboveVertex.GetComponent<Rigidbody>().mass = pointMass;
                                }
                            }
                        }
                    }
                }
                break;
            case SoftbodyType.CUBE:
                {
                    if (dimensions[0] == 0 || dimensions[1] == 0 || dimensions[2] == 0)
                    {
                        Debug.LogError("Cannot do ultra-small cubes. Use a rope or sheet instead.");
                    }

                    //NOTE: ArrayLists are untyped - THIS ONE CASTS TO AN ARRAYLIST OF ARRAYLISTS OF GAMEOBJECTS
                    ArrayList vertices = GenerateCube(softbodyBase, resolution, dimensions, Vector3.zero);

                    //Now that we have a 3d array of vertices in order, we can glue the softbody together.
                    {
                        //NOTE:
                        /*
                         * We need to glue to the following vertices:
                         * Same as above + before (different tl arraylist, same sub arraylist and index)
                         * Up diagonals ONLY
                         */

                        //Now glue the first rope together using spring joints
                        //DO NOT SKIP THE FIRST PLANE - MUST STILL BE GLUED
                        for (int i = 0; i < vertices.Count; i++)
                        {
                            ArrayList plane = (ArrayList)vertices[i];
                            //DO NOT SKIP THE FIRST ROPE - MUST STILL BE GLUED
                            for (int j = 0; j < plane.Count; j++)
                            {
                                ArrayList rope = (ArrayList)plane[j];
                                //DO NOT SKIP THE FIRST POINT - MUST STILL BE GLUED
                                for (int k = 0; k < rope.Count; k++)
                                {
                                    //ONLY FULLY SKIP THE VERY FIRST ONE - SELECTIVELY SKIP THE OTHERS
                                    GameObject vertex = (GameObject)rope[k];
                                    //TEST: Will reducing the mass create a better effect?
                                    vertex.GetComponent<Rigidbody>().mass = pointMass;

                                    //Glue to previous vertex
                                    if (k > 0)
                                    {
                                        GameObject previousVertex = (GameObject)rope[k - 1];

                                        GlueVertices(vertex, previousVertex);
                                    }

                                    //Glue to vertex above
                                    if (j > 0)
                                    {
                                        GameObject aboveVertex = (GameObject)((ArrayList)plane[j - 1])[k];

                                        GlueVertices(vertex, aboveVertex);
                                    }

                                    //Glue to vertex before
                                    if (i > 0)
                                    {
                                        GameObject beforeVertex = (GameObject)((ArrayList)((ArrayList)vertices[i - 1])[j])[k];

                                        GlueVertices(vertex, beforeVertex);
                                    }

                                    //ONLY GLUE VERTICES UP
                                    if (i > 0)
                                    {
                                        //Glue to vertex left-forwards
                                        if (k > 0 && j > 0)
                                        {
                                            GameObject lfVertex = (GameObject)((ArrayList)((ArrayList)vertices[i - 1])[j - 1])[k - 1];

                                            GlueVertices(vertex, lfVertex);
                                        }
                                        //Glue to vertex left-backwards
                                        if (k < rope.Count - 1 && j > 0)
                                        {
                                            GameObject lbVertex = (GameObject)((ArrayList)((ArrayList)vertices[i - 1])[j - 1])[k + 1];

                                            GlueVertices(vertex, lbVertex);
                                        }
                                        //Glue to vertex right-forwards
                                        if (k > 0 && j < plane.Count - 1)
                                        {
                                            GameObject rfVertex = (GameObject)((ArrayList)((ArrayList)vertices[i - 1])[j + 1])[k - 1];

                                            GlueVertices(vertex, rfVertex);
                                        }
                                        //Glue to vertex right-backwards
                                        if (k < plane.Count - 1 && j > rope.Count - 1)
                                        {
                                            GameObject rbVertex = (GameObject)((ArrayList)((ArrayList)vertices[i - 1])[j + 1])[k + 1];

                                            GlueVertices(vertex, rbVertex);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                break;
            case SoftbodyType.SPHERE:
                break;
            case SoftbodyType.COMPOSITE:
                break;
            default:
                Debug.LogError("Unrecognized softbody type");
                break;
        }

        return softbodyBase;
    }

    IEnumerator DelayedSpawn()
    {
        float[] dimensions = { 4, 4, 4 };
        yield return new WaitForSeconds(1);
        //GameObject rope0 = SpawnSoftbody(SoftbodyType.ROPE, dimensions, 3);

        //GameObject sheet0 = SpawnSoftbody(SoftbodyType.SHEET, dimensions, 3);

        GameObject cube0 = SpawnSoftbody(SoftbodyType.CUBE, dimensions, 3);
        //sheet0.GetComponent<SoftbodyController>().ImpulseSoftbody(20);
    }

    //Rope always generates in x direction
    ArrayList GenerateRope(GameObject softbodyBase, int resolution, float[] dimensions, Vector3 offset)
    {
        //NOTE: ArrayLists are untyped - BE CAREFUL as I cast out to a GameObject every time
        ArrayList vertices = new ArrayList();
        //Start by adding the endpoints
        GameObject startVertex = Instantiate(softbodyVertexPrefab, softbodyBase.transform.position + offset, Quaternion.identity, softbodyBase.transform);
        GameObject endVertex = Instantiate(softbodyVertexPrefab, softbodyBase.transform.position + new Vector3(dimensions[0], 0, 0) + offset, Quaternion.identity, softbodyBase.transform);
        vertices.Add(startVertex);
        vertices.Add(endVertex);
        //Iterate over resolution to split into resolution
        for (int i = 0; i < resolution; i++)
        {
            for (int j = vertices.Count - 2; j >= 0; j--)
            {
                GameObject leftVertex = (GameObject)vertices[j];
                GameObject rightVertex = (GameObject)vertices[j + 1];
                GameObject newVertex = Instantiate(softbodyVertexPrefab, (leftVertex.transform.position + rightVertex.transform.position) / 2, Quaternion.identity, softbodyBase.transform);
                vertices.Insert(j + 1, newVertex);
            }
        }

        return vertices;
    }

    //Plane always generates in z direction
    ArrayList GeneratePlane(GameObject softbodyBase, int resolution, float[] dimensions, Vector3 offset)
    {
        //NOTE: ArrayLists are untyped - THIS ONE CASTS TO AN ARRAYLIST OF GAMEOBJECTS, NOT TO GAMEOBJECTS
        ArrayList vertices = new ArrayList();
        ArrayList subVertices;
        //Generate the near array
        subVertices = GenerateRope(softbodyBase, resolution, dimensions, Vector3.zero + offset);
        vertices.Add(subVertices);

        //Generate the far array
        subVertices = GenerateRope(softbodyBase, resolution, dimensions, new Vector3(0, 0, dimensions[1]) + offset);
        vertices.Add(subVertices);

        for (int i = 0; i < resolution; i++)
        {
            for (int j = vertices.Count - 2; j >= 0; j--)
            {
                ArrayList leftSubVertex = (ArrayList)vertices[j];
                ArrayList rightSubVertex = (ArrayList)vertices[j + 1];

                float zOffset = (((GameObject)leftSubVertex[0]).transform.localPosition.z + ((GameObject)rightSubVertex[0]).transform.localPosition.z) / 2;

                //Generate the middle array
                ArrayList midVertices = GenerateRope(softbodyBase, resolution, dimensions, new Vector3(0, 0, zOffset) + offset);
                vertices.Insert(j + 1, midVertices);
            }
        }

        return vertices;
    }

    //Plane always generates in z direction
    ArrayList GenerateCube(GameObject softbodyBase, int resolution, float[] dimensions, Vector3 offset)
    {
        //NOTE: ArrayLists are untyped - THIS ONE CASTS TO AN ARRAYLIST OF GAMEOBJECTS, NOT TO GAMEOBJECTS
        ArrayList vertices = new ArrayList();
        ArrayList subVertices;
        //Generate the near plane
        subVertices = GeneratePlane(softbodyBase, resolution, dimensions, Vector3.zero);
        vertices.Add(subVertices);

        //Generate the far plane
        subVertices = GeneratePlane(softbodyBase, resolution, dimensions, new Vector3(0, dimensions[2], 0));
        vertices.Add(subVertices);

        for (int i = 0; i < resolution; i++)
        {
            for (int j = vertices.Count - 2; j >= 0; j--)
            {
                ArrayList leftSubVertex = (ArrayList)((ArrayList)vertices[j])[0];
                ArrayList rightSubVertex = (ArrayList)((ArrayList)vertices[j + 1])[0];

                float yOffset = (((GameObject)leftSubVertex[0]).transform.localPosition.y + ((GameObject)rightSubVertex[0]).transform.localPosition.y) / 2;

                //Generate the middle plane
                ArrayList midVertices = GeneratePlane(softbodyBase, resolution, dimensions, new Vector3(0, yOffset, 0));
                vertices.Insert(j + 1, midVertices);
            }
        }

        return vertices;
    }

    void GlueVertices(GameObject vertex, GameObject target)
    {
        //NOTE: Should be safe - but we should be careful as this will lead to multiple spring joings per vertex
        //Must be careful when using GetComponent on SpringJoints
        SpringJoint vertexSpring = vertex.AddComponent<SpringJoint>();
        SpringJoint targetSpring = target.AddComponent<SpringJoint>();

        vertexSpring.connectedBody = target.GetComponent<Rigidbody>();
        targetSpring.connectedBody = vertex.GetComponent<Rigidbody>();
        //Just increasing the spring power for a more taut softbody
        vertexSpring.spring = 50;
        targetSpring.spring = 50;

        //TEST: Will changing the tolerance make it more rope-like?
        vertexSpring.tolerance = 0;
        targetSpring.tolerance = 0;

        //TEST: Will reducing the mass create a better effect?
        targetSpring.GetComponent<Rigidbody>().mass = pointMass;

        //TODO: Test with debug lines
        Debug.DrawLine(vertex.transform.position, target.transform.position, Color.red, 10);
    }
}
