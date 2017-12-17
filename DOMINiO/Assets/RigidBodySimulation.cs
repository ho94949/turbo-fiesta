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

    private float linearDampingCoeffY = 15f; //damping term
    private float linearDampingCoeffX = 0.25f; //0.25f; //friction term
    private float angularDampingCoeff = 0.0005f;

    public Vector2 LinearVelocity { get; private set; }
    public float AngularVelocity { get; private set; }
    private float height;
    private float width;

    private Vector3 initialPosition;
    private Quaternion initialRotation;

    public float ColliderWidth;
    public float ColliderHeight;
    public bool IsStatic;
    public bool IsDragable;

    public bool IsEndDomino;

    private bool SimulationHalted = false;

    public bool UnderSimulation;

    private TargetCube tc;
    private bool dragging;
    private Vector3 prevMousePosition;

    // For less memory allocation/garbage collecting
    private Vector2[] pointArray;

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
    }

    public void StartSimulation()
    {
        if (UnderSimulation == true)
            return;
        UnderSimulation = true;
        SimulationHalted = false;
        initialPosition = this.transform.position;
        initialRotation = this.transform.rotation;
    }
    public void StopSimulation()
    {
        SimulationHalted = true;
        UnderSimulation = false;
    }
    public void ResetSimulation()
    {
        UnderSimulation = false;
        SimulationHalted = false;
        this.transform.position = initialPosition;
        this.transform.rotation = initialRotation;

        LinearVelocity = Vector2.zero;
        AngularVelocity = 0;
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
        LinearVelocity = new Vector2(0, 0);
        AngularVelocity = 0.0f;
    }

    private void Update()
    {
        if (SimulationHalted) return;
        if (UnderSimulation == false && IsDragable == true)
        {
            if (dragging == true)
            {
                Vector3 dpos = Camera.main.ScreenToWorldPoint(Input.mousePosition) - prevMousePosition;
                this.transform.position += new Vector3(dpos.x, dpos.y);
                prevMousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButtonDown(0) == true)
            {
                Vector2 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);

                if (CustomPhysics.InsidePolygon(this.ToPointArray(), mouseWorldPosition))
                {
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
        if (SimulationHalted) return;
        if (!UnderSimulation && dragging)
            return;
        if (!UnderSimulation && !IsDragable)
            return;
        //if (underSimulation == false)
        //    return;

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
            if (this.IsEndDomino == false && sim.gameObject.GetComponent<TargetCube>() != null)
                continue;


            float depth;
            Vector2 deepestPoint;
            Vector2 direction;

            CustomPhysics.GetPenetrationDepth(
               this.ToPointArray(), sim.ToPointArray(),
               out depth, out deepestPoint, out direction);
            if (depth != 0)
            {
                if (!UnderSimulation)
                {
                    if (sim.dragging) return;
                    LinearVelocity = Vector2.zero;
                    AngularVelocity = 0;
                    sim.LinearVelocity = Vector2.zero;
                    sim.AngularVelocity = 0;

                    transform.Translate(transform.InverseTransformDirection((depth) * direction));
                    return;
                }
                else
                {
                    if (tc != null && sim.IsEndDomino == true)
                        tc.Collided();

                    Vector2 f = penaltyConstant * direction * depth;
                    worldForce += f;

                    Vector2 r = deepestPoint - (Vector2)transform.position;
                    if (Mathf.Abs(worldTorque) > 0.01 || Mathf.Abs(r.x * f.y - r.y * f.x) > 0.01)
                        worldTorque += r.x * f.y - r.y * f.x;

                    collided = true;

                    float dotproduct = Vector2.Dot(LinearVelocity, direction);
                    Vector2 directionwiseVelocity = dotproduct * direction;

                    LinearVelocity =
                        (LinearVelocity - directionwiseVelocity) * (1 - linearDampingCoeffX * dt) //perpendicular to normal
                        + directionwiseVelocity * (1 - linearDampingCoeffY * dt); //normal
                }
                //CustomPhysics.DebugVector2(deepestPoint);

            }

        }


        if (collided == true)
        {
            AngularVelocity *= (1 - angularDampingCoeff * dt);
        }

        LinearVelocity += dt * AngularVelocity * new Vector2(LinearVelocity.y, -LinearVelocity.x);
        LinearVelocity += dt * worldForce / mass; //normal term

        AngularVelocity += dt * worldTorque / inertia;

        LinearVelocity *= (1 - linearDrag * dt);
        AngularVelocity *= (1 - angularDrag * dt);
        if (!UnderSimulation)
            AngularVelocity = 0;
        if (IsStatic == false)
        {
            transform.Translate(transform.InverseTransformDirection(LinearVelocity) * dt);
            transform.Rotate(0, 0, AngularVelocity * dt * 180 / Mathf.PI);
        }
    }
}
