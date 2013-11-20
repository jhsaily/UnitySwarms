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
    private bool[] siteLock;
    private Vector3 nearestSite;
    private BoidController nSParent;
    private int index = 0;
    private static Vector3[] calculatedSites;
    private static bool calculated = false;
    private static int count = 0;
    private float crystalAngleRad = 0;
    private BoidController nearestBoid;
    private double accumulator = 0;
    private double updateInterval = 1.0d/30.0d;
    void Start () 
    {
        parent = transform.parent.GetComponent<BoidGroupController>();
        velocity = new Vector3( Random.Range( -1.0f, 1.0f ), Random.Range( -1.0f, 1.0f ), Random.Range( -1.0f, 1.0f ) );

        /*
         * Declares whether or not boid should be locked to a 2D space
         */
        if (parent.crystalSites > 0)
        {
            crystalAngleRad = 360 / parent.crystalSites;
        }
        nearestSite = this.transform.position;
        nearestBoid = this;
        if ( parent.Grid2D ) 
        {
            transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY 
                                                            | RigidbodyConstraints.FreezeRotationX 
                                                            | RigidbodyConstraints.FreezeRotationZ 
                                                            | RigidbodyConstraints.FreezeRotationY;
            if ( calculated == false )
            {
                calculatedSites = calculate2DSites();
                calculated = true;
            }
        }
        else
        {
            if ( calculated == false )
            {
                calculatedSites = calculate3DSites();
                calculated = true;
            }
        }

        crystalSites = new Vector3[count];
        siteLock = new bool[count];
        this.gameObject.layer = parent.gameObject.layer;
    }
    
    // Update is called once per frame
    void Update ()
    {
        if ( Time.deltaTime > 0.1 )
        {
            accumulator += 0.1;
        }
        else
        {
            accumulator += Time.deltaTime;
        }
        while ( accumulator >= updateInterval )
        {
            updateVelocity ();
            Move ();
            updateSites ();
            accumulator -= updateInterval;
        }
    }

    void LateUpdate ()
    {
        updateLines ();
    }

    public void Move ()
    {
        transform.Translate( velocity + v5 );
    }

    public void updateVelocity ()
    {
        v4 = boidRuleFour();
        velocity = velocity + mainRules() + v4 + v5 + v6 + boidRuleFive () + boidRuleSix ();
        if ( parent.Grid2D )
        {
            velocity.y = 0;
        }
        limitVelocity();
    }

    public void updateSites()
    {
        for ( int i = 0; i < count; i++ )
        {
            crystalSites[i] = this.transform.position + calculatedSites[i];
        }
    }

    public void updateLines()
    {
        if (parent.sitePoints)
        {
            float tempLength = 0.5f;
            for ( int i = 0; i < count; i++ )
            {
                Debug.DrawLine( crystalSites[i]+ new Vector3(-1f * tempLength, 0f * tempLength, -1f * tempLength), crystalSites[i] + new Vector3( 1f * tempLength, 0f * tempLength, 1f * tempLength ), Color.red, 0, true );
                Debug.DrawLine( crystalSites[i]+ new Vector3(1f * tempLength, 0f * tempLength, -1f * tempLength), crystalSites[i] + new Vector3( -1f * tempLength, 0f * tempLength, 1f * tempLength ), Color.red, 0, true );
            }
        }
        if ( parent.siteLines )
        {
            Debug.DrawLine(this.transform.position, nearestSite, Color.green, 0, true);
        }
        if ( parent.boidLines && nSParent != null )
        {
            Debug.DrawLine(this.transform.position, nSParent.transform.position, Color.magenta, 0, true);
        }
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
            if ( boid != null ) 
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
                    
                    if ( ( Vector3.Distance( nearestBoid.transform.position, this.transform.position ) >
                        Vector3.Distance( boid.transform.position, this.transform.position ) ||
                        nearestBoid == this ) &&
                        boid != this &&
                        boid.hasFreeSites())
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
        
        if ( nearestBoid != null && parent.crystalFormationWeight > 0 && parent.crystalSites > 0)
        {
            Vector3[] nearestSites = nearestBoid.getCrystalSites();
            if (nSParent != null)
            {
                nSParent.unlockSite(index);
            }
            if (nSParent == null || nSParent == this)
            {
                nearestSite = nearestSites[0];
                nSParent = nearestBoid;
                for ( int i = 1; i < count; i++ )
                {
                    if ( Vector3.Distance( nearestSites[i], this.transform.position ) <
                        Vector3.Distance( nearestSite, this.transform.position ) &&
                        nearestBoid.isLocked(i) == false)
                    {
                        nearestSite = nearestSites[i];
                        index = i;
                    }
                }
            }
            else
            {
                nearestSite = nSParent.getCrystalSites()[index];
                bool temp = true;
                for ( int i = 0; i < count; i++ )
                {
                    if ( temp )
                    {
                        if ( parent.crystalSiteWeight * Vector3.Distance( nearestSites[i], this.transform.position ) <
                        Vector3.Distance( nearestSite, this.transform.position ) &&
                        nearestBoid.isLocked(i) == false )
                        {
                            temp = false;
                            nearestSite = nearestSites[i];
                            index = i;
                            nSParent = nearestBoid;
                        }
                    }
                    else
                    {
                        if ( Vector3.Distance( nearestSites[i], this.transform.position ) <
                        Vector3.Distance( nearestSite, this.transform.position ) &&
                        nearestBoid.isLocked(i) == false)
                        {
                            nearestSite = nearestSites[i];
                            index = i;
                            nSParent = nearestBoid;
                        }
                    }
                }
            }

            if ( nSParent != null && nSParent != this )
            {
                nSParent.lockSite(index);
            }

            if ( nSParent != this )
            {
                v6 = ( nearestSite - transform.position ) * Vector3.Distance( nearestSite, this.transform.position ) / parent.crystalDistance;
            }
        }
        if ( parent.generateNoise )
        {
            float tempr = Random.value;
            int rand = 0;
            if ( tempr > parent.noiseAmount )
            {
                rand = 1;
            }
            if ( rand == 1 )
            {
                v6 = v6 * parent.crystalFormationWeight;
            }
            else
            {
                v6 = Vector3.zero;
            }
        } 
        else
        {
            v6 = v6 * parent.crystalFormationWeight;
        }
        
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
                    separationVector * parent.intraGroupSeparationWeight;
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
     * Avoid predators
     */
    private Vector3 boidRuleFive ()
    {
        Collider[] nearBoids = Physics.OverlapSphere( this.transform.position, parent.predatorDistance, parent.predatorLayer );
        Vector3 temp = Vector3.zero;
        for ( int i = 0; i < nearBoids.Length; i++ ) 
        {
            BoidController boid = nearBoids[i].GetComponent<BoidController>();
            temp = temp - ( boid.transform.position - this.transform.position );
        }
        temp = temp * parent.avoidPredatorWeight;
        return temp;
    }

    /*
     * Attack prey
     */
    private Vector3 boidRuleSix ()
    {
        Collider[] nearBoids = Physics.OverlapSphere( this.transform.position, parent.predatorDistance, parent.preyLayer );
        Vector3 temp = Vector3.zero;
        BoidController closest = null;
        if (nearBoids.Length > 0)
        {
            closest = nearBoids[0].GetComponent<BoidController>();
        }
        for ( int i = 1; i < nearBoids.Length; i++ ) 
        {
            BoidController boid = nearBoids[i].GetComponent<BoidController>();
            if (Vector3.Distance(this.transform.position, boid.transform.position) < Vector3.Distance(this.transform.position, closest.transform.position))
            {
                closest = boid;
            }
        }
        if (closest != null)
        {
            temp = closest.transform.position - this.transform.position;
        }
        temp = temp * parent.attackPreyWeight;
        return temp;
    }

    /*
     * Limit velocity of current boid to a max set value
     */
    private void limitVelocity () 
    {
        if ( Vector3.Magnitude( this.velocity ) > parent.maxSpeed ) 
        {
            this.velocity = ( this.velocity / Vector3.Magnitude( this.velocity ) ) * parent.maxSpeed;
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
        count = parent.crystalSites;
        Vector3[] siteCoords = new Vector3[siteNo];
        if ( siteNo == 0 )
        {
            return siteCoords;
        }
        float theta = 0;

        for ( int i = 0; i < siteNo; i++ )
        {
            Vector3 rotationPos = new Vector3( parent.crystalDistance, 0f, 0f );
            rotationPos = rotateY( rotationPos, theta );
            siteCoords[i] = rotationPos;
            theta += crystalAngleRad * ( Mathf.PI/180 );
        }

        return siteCoords;
    }

    private Vector3[] calculate3DSites ()
    {
        int siteNo = parent.crystalSites;
        ArrayList temp = new ArrayList();
        Vector3[] siteCoords;

        if ( siteNo == 0 )
        {
            return new Vector3[0];
        }

        float thetaY = 0f;
        float thetaZ = 0f;
        float CARY = 0f;
        float CARZ = 0f;
        if ( parent.crystalSites % 2 == 0 )
        {
            CARY = 360 / (parent.crystalSites / 2);
            CARZ = 360 / (parent.crystalSites / 2);
        }
        else
        {
            CARY = 360 / ( ( ( parent.crystalSites - 1 ) / 2 + 1 ) );
            CARZ = 360 / ( ( parent.crystalSites - 1 ) / 2 );
        }

        for ( int i = 0; i < siteNo; i++ )
        {
            if ( i%2 == 0 )
            {
                Vector3 rotationPos = new Vector3( parent.crystalDistance, 0f, 0f );
                rotationPos = rotateY( rotationPos, thetaY );
                bool tempBool = false;
                for ( int j = 0; j < temp.Count; j++ )
                {
                    if ( (Vector3) temp[j] == rotationPos )
                    {
                        tempBool = true;
                        break;
                    }
                }
                if ( !tempBool )
                {
                    temp.Add( rotationPos );
                }
                thetaY += CARY * ( Mathf.PI/180 );
            }
            else
            {
                Vector3 rotationPos = new Vector3( parent.crystalDistance, 0f, 0f );
                rotationPos = rotateZ( rotationPos, thetaZ );
                bool tempBool = false;
                for ( int j = 0; j < temp.Count; j++ )
                {
                    if ( (Vector3) temp[j] == rotationPos )
                    {
                        tempBool = true;
                        break;
                    }
                }
                if ( !tempBool )
                {
                    temp.Add(rotationPos);
                }
                thetaZ += CARZ * ( Mathf.PI/180 );
            }
        }
        count = temp.Count;
        siteCoords = new Vector3[temp.Count];
        for ( int i = 0; i < temp.Count; i++ )
        {
            siteCoords[i] = (Vector3) temp[i];
        }

        // Actual algorithm begins here
        /*
        float p = 1/2;
        float a = 1 - 2 * p / (siteNo - 3);
        float b = p*(siteNo + 1) / (siteNo - 3);
        float r = 0;
        float theta = Mathf.PI;
        float phi = 0f;
        if (siteNo == 6)
        {
            siteCoords[0] = new Vector3(1f, 0f, 0f) * parent.crystalDistance;
            siteCoords[1] = new Vector3(0f, 1f, 0f) * parent.crystalDistance;
            siteCoords[2] = new Vector3(0f, 0f, 1f) * parent.crystalDistance;
            siteCoords[3] = new Vector3(-1f, 0f, 0f) * parent.crystalDistance;
            siteCoords[4] = new Vector3(0f, -1f, 0f) * parent.crystalDistance;
            siteCoords[5] = new Vector3(0f, 0f, -1f) * parent.crystalDistance;
        } else if (siteNo == 8)
        {
            siteCoords[0] = (new Vector3(1f, 1f, 0f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[1] = (new Vector3(1f, -1f, 0f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[2] = (new Vector3(1f, 0f, 1f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[3] = (new Vector3(1f, 0f, -1f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[4] = (new Vector3(-1f, 1f, 0f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[5] = (new Vector3(-1f, -1f, 0f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[6] = (new Vector3(-1f, 0f, 1f) / Mathf.Sqrt(2)) * parent.crystalDistance;
            siteCoords[7] = (new Vector3(-1f, 0f, -1f) / Mathf.Sqrt(2)) * parent.crystalDistance;
        }
        else {
        for (int i = 1; i <= siteNo; i++)
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
            else if (i == siteNo)
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
        }
        */
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

    public bool isLocked ( int index )
    {
        return siteLock[index];
    }

    public void lockSite ( int index )
    {
        siteLock[index] = true;
    }

    public void unlockSite ( int index )
    {
        siteLock[index] = false;
    }

    public bool hasFreeSites()
    {
        bool temp = false;
        for ( int i = 0; i < count; i++ )
        {
            if ( isLocked(i) == false )
            {
                temp = true;
                break;
            }
        }
        return temp;
    }
}