using UnityEngine;
using System.Collections;

public class BoidGroupController : MonoBehaviour
{
    
    public GameObject boidPrefab;
    public int boidCount = 10;
    public float minDistance = 5.0f;
    public float maxSubGroupDistance = 25.0f;
    public float alignmentWeight = 2, cohesionWeight = 1, interGroupSeparationWeight = 1, intraGroupSeparationWeight = 1, crystalFormationWeight = 1;
    public int crystalSites = 2;
    public float crystalDistance = 5;
    public float crystalSiteWeight = 1;
    public float maxSpeed = 4.0f;
    public float wallBounceSpeed = 0.5f;
    public int xMin = 0, xMax = 100, yMin = 0, yMax = 100, zMin = 0, zMax = 100;
    public bool Grid2D = false;
    
    void Start ()
    {    
        GameObject boid;
        for ( int i = 0; i < boidCount; i++ ) {
            if (Grid2D)
            {
                boid = (GameObject) Instantiate( boidPrefab,
                                                new Vector3( Random.Range( xMin, xMax ),
                                                0.98f,
                                                Random.Range( zMin, zMax ) ),
                                                transform.rotation );
                boid.transform.parent = this.transform;
            }
            else
            {
                boid = (GameObject) Instantiate( boidPrefab,
                                                new Vector3( Random.Range( xMin, xMax ),
                                                Random.Range( yMin, yMax ),
                                                Random.Range( zMin, zMax ) ),
                                                transform.rotation );
                boid.transform.parent = this.transform;
            }
        }    
    }
    
    // Update is called once per frame
    void Update ()
    {
    }
}