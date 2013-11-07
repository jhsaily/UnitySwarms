using UnityEngine;
using System.Collections;

public class BoidGroupController : MonoBehaviour
{
    
    public GameObject boidPrefab;
    public int boidCount = 100;
    public float minDistance = 2.5f;
    public float maxSubGroupDistance = 15f;
    public float alignmentWeight = 4, cohesionWeight = 0.25f, interGroupSeparationWeight = 1, intraGroupSeparationWeight = 1, crystalFormationWeight = 0.4f, crystalSiteWeight = 0.25f;
    public int crystalSites = 4;
    public float crystalDistance = 10;
    public float maxSpeed = 2.5f;
    public float wallBounceSpeed = 1.5f;
    public int xMin = 0, xMax = 100, yMin = 0, yMax = 100, zMin = 0, zMax = 100;
    public bool Grid2D = false;
    public bool siteLines = false;
    public bool boidLines = false;
    
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