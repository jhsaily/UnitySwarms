using UnityEngine;
using System.Collections;

public class BoidController : MonoBehaviour {
	
	private Vector3 velocity;
	private BoidGroupController parent;
	private float aDist = 0.0f;
	private int nearby = 0;
	private Vector3 groupCenter;
	private Vector3 groupVelocity;
	private Vector3 v4;
	private Vector3 v5;
	private Vector3 v6;
	public Vector3[] crystalSites;
	
	void Start () {
		parent = transform.parent.GetComponent<BoidGroupController>();
		velocity = new Vector3(Random.Range(-1.0f, 1.0f), 0.0f, Random.Range(-1.0f, 1.0f));
		if (parent.Grid2D) {
			transform.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ | RigidbodyConstraints.FreezeRotationY;
		}
		crystalSites = new Vector3[parent.crystalSites];
		float theta = 0;
		for (int i = 0; i < parent.crystalSites; i++) {
			Vector3 rotationPos = new Vector3(parent.crystalDistance, 0f, 0f);
			rotationPos = RotateY(rotationPos, theta);
			crystalSites[i] = this.transform.position + rotationPos;
			theta += (parent.crystalAngleRad)*Mathf.PI/180;
		}
	}
	
	// Update is called once per frame
	void Update () {
		float theta = 0;
		for (int i = 0; i < parent.crystalSites; i++) {
			Vector3 rotationPos = new Vector3(parent.crystalDistance, 0f, 0f);
			rotationPos = RotateY(rotationPos, theta);
			crystalSites[i] = this.transform.position + rotationPos;
			theta += (parent.crystalAngleRad)*Mathf.PI/180;
		}
		Move ();
	}
	
	public void Move () {
		v4 = boidRuleFour();
		velocity = velocity + mainRules() + v4;
		if (parent.Grid2D) {
			velocity.y = 0;
		}
		limitVelocity();
		transform.Translate(velocity + v5);
	}
	
	public Vector3 mainRules () {
		groupVelocity = new Vector3();
		groupCenter = new Vector3();
		Vector3 separationVector = new Vector3();
		v5 = new Vector3();
		v6 = new Vector3();
		nearby = 0;
		aDist = 0.0f;
		Collider[] nearBoids = Physics.OverlapSphere(this.transform.position, parent.maxSubGroupDistance);
		BoidController nearestBoid = this;
		bool first = true;
		for (int i = 0; i < nearBoids.Length; i++) {
			BoidController boid = nearBoids[i].GetComponent<BoidController>();
			if (boid != null) {
				if (boid.getParent() == parent && boid != this) {
					if (first) {
						nearestBoid = boid;
						first = false;
					}
					groupVelocity = groupVelocity + boid.getVelocity();
					nearby++;
					aDist = aDist + Vector3.Distance(boid.transform.position, this.transform.position);
					groupCenter = groupCenter + boid.transform.position;
					if (Vector3.Distance(nearestBoid.transform.position, this.transform.position) > Vector3.Distance(boid.transform.position, this.transform.position)) {
						nearestBoid = boid;
					}
					if (Vector3.Distance(boid.transform.position, this.transform.position) < parent.minDistance) {
						separationVector = (separationVector - (boid.transform.position - this.transform.position)); 
						//separationVector = (separationVector - (boid.transform.position - this.transform.position)) * (parent.minDistance - Vector3.Magnitude(boid.transform.position - this.transform.position))/(parent.minDistance); 
					}
				} else {
					if (Vector3.Distance(boid.transform.position, this.transform.position) < parent.minDistance) {
						v5 = v5 - (boid.transform.position - this.transform.position);
					}
				}
			}
		}
		v5 = v5*parent.interGroupSeparationWeight;
		
		if (nearestBoid != null) {
			Vector3 nearestSite = nearestBoid.crystalSites[0];
			for (int i = 1; i < parent.crystalSites; i++) {
				if (Vector3.Distance(nearestBoid.crystalSites[i], this.transform.position) < Vector3.Distance(nearestSite, this.transform.position)) {
					nearestSite = nearestBoid.crystalSites[i];
				}
			}
			v6 = (nearestSite - transform.position) * (Vector3.Distance(nearestSite, this.transform.position)/parent.crystalDistance);
		}
		v6 = v6*parent.crystalFormationWeight;
		
		if (nearby == 0) {
			return separationVector;
		} else {
			groupVelocity = groupVelocity / nearby;
			groupCenter = groupCenter / nearby;
			aDist = aDist / nearby;
			return (((groupVelocity - this.velocity) / 8)*parent.alignmentWeight + ((groupCenter - transform.position)/50)*parent.cohesionWeight + separationVector*parent.intraGroupSeparationWeight + v6);
		}
	}
	
	public Vector3 boidRuleFour () {
		Vector3 temp = new Vector3();
		if (this.transform.position.x < parent.xMin) {
			temp.x = parent.wallBounceSpeed;
		} else if (this.transform.position.x > parent.xMax) {
			temp.x = -parent.wallBounceSpeed;
		}
		
		if (this.transform.position.y < parent.yMin) {
			temp.y = parent.wallBounceSpeed;
		} else if (this.transform.position.y > parent.yMax) {
			temp.y = -parent.wallBounceSpeed;
		}
		
		if (this.transform.position.z < parent.zMin) {
			temp.z = parent.wallBounceSpeed;
		} else if (this.transform.position.z > parent.zMax) {
			temp.z = -parent.wallBounceSpeed;
		}
		
		return temp;
	}
	
	private void limitVelocity() {
		if (Vector3.Magnitude(this.velocity) > parent.maxSpeed) {
			this.velocity = ((this.velocity / Vector3.Magnitude(this.velocity)) * (parent.maxSpeed*Time.deltaTime));
		}
	}
	
	public Vector3 getVelocity () {
		return velocity;
	}
	
	public BoidGroupController getParent() {
		return parent;
	}
	
	private Vector3 RotateX(Vector3 v, float angle){
		float sin = Mathf.Sin(angle);
		float cos = Mathf.Cos(angle);
		float ty = v.y;
		float tz = v.z;
		v.y = (cos * ty) - (sin * tz);
		v.z = (cos * tz) + (sin * ty);
		return v;
	}
	
    private Vector3 RotateY(Vector3 v, float angle){
		float sin = Mathf.Sin(angle);
		float cos = Mathf.Cos(angle);
		float tx = v.x;
		float tz = v.z;
		v.x = (cos * tx) + (sin * tz);
		v.z = (cos * tz) - (sin * tx);
		return v;
	}
	
	private Vector3 RotateZ(Vector3 v, float angle){
		float sin = Mathf.Sin(angle);
		float cos = Mathf.Cos(angle);
		float tx = v.x;
		float ty = v.y;
		v.x = (cos * tx) - (sin * ty);
		v.y = (cos * ty) + (sin * tx);
		return v;
	}
}