using UnityEngine;
using System.Collections;

public class BoidController : MonoBehaviour
{
    
    private BoidGroupController parent;      // Object that reigns over entire group
    private float averageDistance = 0.0f;    // Average distance between all sub-group members
    private int nearby = 0;                  // Number of nearby entities (determines sub group size)
    private Vector3 groupCenter;             // Position vector of subgroup center
    private Vector3 groupVelocity;           // Velocity vector of average subgroup velocity
    private Vector3 velocity;                // Velocity vector for this entity
    private Vector3 v4;                      // Velocity vector for boid rule 4
    private Vector3 v5;                      // Velocity vector for boid rule 5
    private Vector3 v6;                      // Velocity vector for boid rule 6
    private Vector3[] crystalSites;          // Position vectors of all crystal sites around boid
    private static Vector3[] calculatedSites;
    private static bool calculated = false;
    
    void Start () 
    {
        parent = transform.parent.GetComponent<BoidGroupController>();
        velocity = new Vector3( Random.Range( -1.0f, 1.0f ), Random.Range( -1.0f, 1.0f ), Random.Range( -1.0f, 1.0f ) );

        /*
         * Declares whether or not boid should be locked to a 2D space
         */
        crystalSites = new Vector3[parent.crystalSites];
        if ( parent.Grid2D ) 
        {
            transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY 
                                                            | RigidbodyConstraints.FreezeRotationX 
                                                            | RigidbodyConstraints.FreezeRotationZ 
                                                            | RigidbodyConstraints.FreezeRotationY;
            if (calculated == false) {
                calculatedSites = calculate2DSites();
                calculated = true;
            }
        }
        else
        {
            if (calculated == false) {
                calculatedSites = calculate3DSites();
                calculated = true;
            }
        }
    }
    
    // Update is called once per frame
    void Update ()
    {
        for ( int i = 0; i < parent.crystalSites; i++ )
        {
            crystalSites[i] = this.transform.position + calculatedSites[i];
        }
        Move ();
    }

    public void Move ()
    {
        v4 = boidRuleFour();
        velocity = velocity + mainRules() + v4;
        if ( parent.Grid2D )
        {
            velocity.y = 0;
        }
        limitVelocity();
        transform.Translate( velocity + v5 );
    }
    
    /*
     * Calculates velocity vector of current boid.
     * 3 Main rules + X extra rules
     * Rule 1) Move boids to center of group
     * Rule 2) Move boids away from eachother if they get too close
     * Rule 3) Set boid velocities based on surrounding boids
     * extras:
     * Rule 4) (different function) keep boids contained within a set boundary
     * Rule 5) Keep groups of different types of boids separate
     * Rule 6) Keep boids in a crystalized formation
     */
    public Vector3 mainRules ()
    {
        /*
         * Check for nearby boids using an OverlapSphere collider
         */
        Collider[] nearBoids = Physics.OverlapSphere( this.transform.position, parent.maxSubGroupDistance );
        BoidController nearestBoid = this;
        groupVelocity = new Vector3();
        groupCenter = new Vector3();
        Vector3 separationVector = new Vector3();
        v5 = new Vector3();
        v6 = new Vector3();
        nearby = 0;
        averageDistance = 0.0f;
        bool first = true;
        
        for ( int i = 0; i < nearBoids.Length; i++ ) 
        {
            BoidController boid = nearBoids[i].GetComponent<BoidController>();
            if (boid != null) 
            {
                if ( boid.getParent() == parent && boid != this )
                {
                    if ( first )
                    {
                        nearestBoid = boid;
                        first = false;
                    }
                    groupVelocity = groupVelocity + boid.getVelocity();
                    nearby++;
                    averageDistance = averageDistance + Vector3.Distance( boid.transform.position, 
                                                                        this.transform.position );
                    groupCenter = groupCenter + boid.transform.position;
                    
                    if ( Vector3.Distance( nearestBoid.transform.position, this.transform.position ) >
                        Vector3.Distance( boid.transform.position, this.transform.position ) )
                    {
                        nearestBoid = boid;
                    }
                    if ( Vector3.Distance( boid.transform.position, this.transform.position ) <
                        parent.minDistance )
                    {
                        separationVector = separationVector - ( boid.transform.position - this.transform.position ); 
                        //separationVector = (separationVector - (boid.transform.position - this.transform.position)) * (parent.minDistance - Vector3.Magnitude(boid.transform.position - this.transform.position))/(parent.minDistance); 
                    }
                } 
                else 
                {
                    if ( Vector3.Distance ( boid.transform.position, this.transform.position ) < 
                        parent.minDistance ) 
                    {
                        v5 = v5 - ( boid.transform.position - this.transform.position );
                    }
                }
            }
        }
        
        v5 = v5*parent.interGroupSeparationWeight;
        
        if ( nearestBoid != null ) 
        {
            Vector3[] nearestSites = nearestBoid.getCrystalSites();
            Vector3 nearestSite = nearestSites[0];
            for ( int i = 1; i < parent.crystalSites; i++ ) 
            {
                if ( Vector3.Distance( nearestSites[i], this.transform.position ) < 
                    Vector3.Distance( nearestSite, this.transform.position ) ) 
                {
                    nearestSite = nearestSites[i];
                }
            }
            v6 = ( nearestSite - transform.position ) * Vector3.Distance( nearestSite, this.transform.position ) / parent.crystalDistance;
        }
        
        v6 = v6*parent.crystalFormationWeight;
        
        if ( nearby == 0 ) 
        {
            return separationVector;
        } 
        else 
        {
            groupVelocity = groupVelocity / nearby;
            groupCenter = groupCenter / nearby;
            averageDistance = averageDistance / nearby;
            return ( ( groupVelocity - this.velocity) / 8 ) * parent.alignmentWeight + 
                    ( ( groupCenter - transform.position ) / 50 ) *parent.cohesionWeight + 
                    separationVector * parent.intraGroupSeparationWeight + v6;
        }
    }
    
    /*
     * Keep boids enclosed in a set boundary space
     */
    public Vector3 boidRuleFour () 
    {
        Vector3 temp = new Vector3();
        if ( this.transform.position.x < parent.xMin ) 
        {
            temp.x = parent.wallBounceSpeed;
        } 
        else if ( this.transform.position.x > parent.xMax ) 
        {
            temp.x = -parent.wallBounceSpeed;
        }
        
        if ( this.transform.position.y < parent.yMin ) 
        {
            temp.y = parent.wallBounceSpeed;
        } 
        else if ( this.transform.position.y > parent.yMax ) 
        {
            temp.y = -parent.wallBounceSpeed;
        }
        
        if ( this.transform.position.z < parent.zMin ) 
        {
            temp.z = parent.wallBounceSpeed;
        } 
        else if ( this.transform.position.z > parent.zMax ) 
        {
            temp.z = -parent.wallBounceSpeed;
        }
        
        return temp;
    }
    
    /*
     * Limit velocity of current boid to a max set value
     */
    private void limitVelocity () 
    {
        if ( Vector3.Magnitude( this.velocity ) > parent.maxSpeed ) 
        {
            this.velocity = ( this.velocity / Vector3.Magnitude( this.velocity ) ) * parent.maxSpeed * Time.deltaTime;
        }
    }
    
    /*
     * Rotates a position vector around the x-axis by some angle
     */
    private Vector3 rotateX ( Vector3 v, float angle )
    {
        float sin = Mathf.Sin( angle );
        float cos = Mathf.Cos( angle );
        float ty = v.y;
        float tz = v.z;
        v.y = ( cos * ty ) - ( sin * tz );
        v.z = ( cos * tz ) + ( sin * ty );
        return v;
    }
    
    /*
     * Rotates a position vector around the y-axis by some angle
     */
    private Vector3 rotateY ( Vector3 v, float angle )
    {
        float sin = Mathf.Sin( angle );
        float cos = Mathf.Cos( angle );
        float tx = v.x;
        float tz = v.z;
        v.x = ( cos * tx ) + ( sin * tz );
        v.z = ( cos * tz ) - ( sin * tx );
        return v;
    }
    
    /*
     * Rotates a position vector around the z-axis by some angle
     */
    private Vector3 rotateZ ( Vector3 v, float angle )
    {
        float sin = Mathf.Sin( angle );
        float cos = Mathf.Cos( angle );
        float tx = v.x;
        float ty = v.y;
        v.x = ( cos * tx ) - ( sin * ty );
        v.y = ( cos * ty ) + ( sin * tx );
        return v;
    }

    private Vector3[] calculate2DSites ()
    {
        int siteNo = parent.crystalSites;
        Vector3[] siteCoords = new Vector3[siteNo];
        float theta = 0;

        for ( int i = 0; i < siteNo; i++ )
        {
            Vector3 rotationPos = new Vector3( parent.crystalDistance, 0f, 0f );
            rotationPos = rotateY( rotationPos, theta );
            siteCoords[i] = rotationPos;
            theta += parent.crystalAngleRad * ( Mathf.PI/180 );
        }

        return siteCoords;
    }

    private Vector3[] calculate3DSites ()
    {
        int siteNo = parent.crystalSites;
        Vector3[] siteCoords = new Vector3[siteNo];
        // Actual algorithm begins here
        float p = 1/2;
        float a = 1 - 2 * p / (siteNo - 3);
        float b = p*(siteNo + 1) / (siteNo - 3);
        float r = 0;
        float theta = Mathf.PI;
        float phi = 0f;
        for(int i = 1; i <= siteNo; i++)
        {
            if (i > 1 && i < siteNo)
            {
                float kPrime = a * i + b;
                float h = -1 + 2*(kPrime - 1) / (siteNo - 1);

                theta = Mathf.Acos(h);
                float temp = r;
                r = Mathf.Sqrt(1 - (h*h));
                double phitemp = (phi + 3.6/Mathf.Sqrt(siteNo)*2/(temp + r)) % (2 * Mathf.PI);
                phi = (float) phitemp;
            }
            else if(i == siteNo)
            {
                theta = 0f;
                phi = 0f;
            }


            siteCoords[i - 1].x = Mathf.Sin(theta) * Mathf.Cos(phi);
            siteCoords[i - 1].y = Mathf.Sin(theta) * Mathf.Sin(phi);
            siteCoords[i - 1].z = Mathf.Cos(theta);

            siteCoords[i - 1] = siteCoords[i - 1] * parent.crystalDistance;
            print (siteCoords[i - 1].x + ", " + siteCoords[i - 1].y + ", " + siteCoords[i - 1].z);
        }
        // Actual algorithm ends here

        return siteCoords;
    }
    
    public Vector3 getVelocity () 
    {
        return velocity;
    }
    
    public BoidGroupController getParent () 
    {
        return parent;
    }
    
    public Vector3[] getCrystalSites ()
    {
        return crystalSites;
    }
}