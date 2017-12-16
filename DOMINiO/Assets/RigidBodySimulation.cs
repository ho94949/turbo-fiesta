using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RigidBodySimulation : MonoBehaviour
{
    private float penaltyConstant = 5500.0f;
    private float gravityConstant = 9.8f;
    private Vector2 gravityVector;
    private float mass = 1.0f;
    private float inertia;

    private float linearDrag = 0.5f;
    private float angularDrag = 0.5f;

    private float linearDampingCoeffY = 35f; //damping term
    private float linearDampingCoeffX = 0.25f; //friction term
    private float angularDampingCoeff = 2.5f;

    private Vector2 linearVelocity;
    private float angularVelocity;
    private float height;
    private float width;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public float ColliderWidth;
    public float ColliderHeight;
    public bool IsStatic;
    public bool IsDragable;

    public bool IsEndDomino;

    private bool underSimulation;

    private TargetCube tc;
    private bool dragging;
    private Vector3 prevMousePosition;

    // For less memory allocation/garbage collecting
    private Vector2[] pointArray;

    public void StartSimulation()
    {
        if (underSimulation == true)
            return;
        underSimulation = true;

        initialPosition = this.transform.position;
        initialRotation = this.transform.rotation;
    }
    public void StopSimulation()
    {
        underSimulation = false;
    }
    public void ResetSimulation()
    {
        underSimulation = false;
        this.transform.position = initialPosition;
        this.transform.rotation = initialRotation;

        linearVelocity = Vector2.zero;
        angularVelocity = 0;
    }

    public Vector2[] ToPointArray()
    {
        if (pointArray == null)
            pointArray = new Vector2[4];

        float height = ColliderHeight * transform.lossyScale.y;
        float width = ColliderWidth * transform.lossyScale.x;

        pointArray[0] = transform.rotation * new Vector2(width / 2, height / 2) + transform.position;
        pointArray[1] = transform.rotation * new Vector2(-width / 2, height / 2) + transform.position;
        pointArray[2] = transform.rotation * new Vector2(-width / 2, -height / 2) + transform.position;
        pointArray[3] = transform.rotation * new Vector2(width / 2, -height / 2) + transform.position;

        return pointArray;
    }

    // Use this for initialization
    void Start()
    {
        height = ColliderHeight * transform.lossyScale.y;
        width = ColliderWidth * transform.lossyScale.x;

        tc = GetComponent<TargetCube>();

        inertia = mass / 12.0f * (height * height + width * width);
        gravityVector = new Vector2(0, -gravityConstant);
        linearVelocity = new Vector2(0, 0);
        angularVelocity = 0.0f;
    }

    private void Update()
    {
        if (underSimulation == false && IsDragable == true)
        {
            if (dragging == true)
            {
                Vector3 dpos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - prevMousePosition;
                this.transform.position += new Vector3(dpos.x, dpos.y);
                prevMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButtonDown(0) == true)
            {
                Debug.Log("Click");
                Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
                
                if (CustomPhysics.InsidePolygon(this.ToPointArray(), mouseWorldPosition))
                {
                    Debug.Log("Under drag!!");
                    dragging = true;
                    prevMousePosition = mouseWorldPosition;
                }
            }
            if (Input.GetMouseButtonUp(0) == true)
                dragging = false;
        }
    }
    void FixedUpdate()
    {
        if (underSimulation == false)
            return;

        float dt = Time.fixedDeltaTime;

        Vector2 worldForce = Vector2.zero;
        float worldTorque = 0;

        //gravity
        Vector2 gravity = mass * gravityVector;
        worldForce += gravity;

        //penalty method

        float degreeAngle = transform.rotation.eulerAngles.z;
        RigidBodySimulation[] simArray = GameObject.FindObjectsOfType<RigidBodySimulation>();
        // Make some overlapboxall

        bool collided = false;
        foreach (RigidBodySimulation sim in simArray)
        {
            if (sim == this) continue;

            float depth;
            Vector2 deepestPoint;
            Vector2 direction;

            CustomPhysics.GetPenetrationDepth(
               this.ToPointArray(), sim.ToPointArray(),
               out depth, out deepestPoint, out direction);
            if (depth != 0)
            {
                if (tc != null && sim.IsEndDomino == true)
                    tc.Collided();

                Vector2 f = penaltyConstant * direction * depth;
                worldForce += f;

                Vector2 r = deepestPoint - (Vector2)transform.position;
                worldTorque += r.x * f.y - r.y * f.x;

                collided = true;

                float dotproduct = Vector2.Dot(linearVelocity, direction);
                Vector2 directionwiseVelocity = dotproduct * direction;

                linearVelocity =
                    (linearVelocity - directionwiseVelocity) * (1 - linearDampingCoeffX * dt) //perpendicular to normal
                    + directionwiseVelocity * (1 - linearDampingCoeffY * dt); //normal
            }

            /* DUPLICATED...
            CustomPhysics.GetPenetrationDepth(
                sim.ToPointArray(), this.ToPointArray(),
                out depth, out deepestPoint, out direction);
            if (depth != 0)
            {
                if (tc != null && sim.IsEndDomino)
                    tc.Collided();

                Vector2 f = -penaltyConstant * direction * depth;
                worldForce += f;

                Vector2 r = deepestPoint - (Vector2)transform.position;
                worldTorque += r.x * f.y - r.y * f.x;

                collided = true;

                float dotproduct = Vector2.Dot(linearVelocity, -direction);
                Vector2 directionwiseVelocity = dotproduct * -direction;

                linearVelocity =
                    (linearVelocity - directionwiseVelocity) * (1 - linearDampingCoeffX * dt) //perpendicular to normal
                    + directionwiseVelocity * (1 - linearDampingCoeffY * dt); //normal
            }
            */
        }

        if (collided == true)
        {
            angularVelocity *= (1 - angularDampingCoeff * dt);
        }

        linearVelocity += dt * angularVelocity * new Vector2(linearVelocity.y, -linearVelocity.x);
        linearVelocity += dt * worldForce / mass; //normal term

        angularVelocity += dt * worldTorque / inertia;

        linearVelocity *= (1 - linearDrag * dt);
        angularVelocity *= (1 - angularDrag * dt);

        if (IsStatic == false)
        {
            transform.Translate(transform.InverseTransformDirection(linearVelocity) * dt);
            transform.Rotate(0, 0, angularVelocity * dt * 180 / Mathf.PI);
        }
    }
}
