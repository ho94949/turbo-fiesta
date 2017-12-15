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

    private BoxCollider2D c2d;

    private Vector2 linearVelocity;
    private float angularVelocity;
    private float height;
    private float width;

    // Use this for initialization
    void Start()
    {
        c2d = GetComponent<BoxCollider2D>();

        height = c2d.size.y * transform.lossyScale.y;
        width = c2d.size.x * transform.lossyScale.x;

        inertia = mass / 12.0f * (height * height + width * width);
        gravityVector = new Vector2(0, -gravityConstant);
        linearVelocity = new Vector2(0, 0);
        angularVelocity = 0.0f;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        Vector2 worldForce = Vector2.zero;
        float worldTorque = 0;

        //gravity
        Vector2 gravity = mass * gravityVector;
        worldForce += gravity;

        //penalty method

        float degreeAngle = transform.rotation.eulerAngles.z;
        Collider2D[] overlapped = Physics2D.OverlapBoxAll(transform.position, new Vector2(width, height), degreeAngle);

        bool collided = false;
        foreach (Collider2D c in overlapped)
        {
            if (c == c2d) continue;

            float depth;
            Vector2 deepestPoint;
            Vector2 direction;

            GetPenetraionDepth(c2d, c, out depth, out deepestPoint, out direction);
            if (depth != 0)
            {
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

            GetPenetraionDepth(c, c2d, out depth, out deepestPoint, out direction);
            if (depth != 0)
            {
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
        
        transform.Translate(transform.InverseTransformDirection(linearVelocity) * dt);
        transform.Rotate(0, 0, angularVelocity * dt * 180 / Mathf.PI);
    }

    static float triangleHeight(Vector2 x, Vector2 y, Vector2 z)
    {
        x -= z;
        y -= z;
        float triangleArea = x.x * y.y - x.y * y.x;
        float hyp = (x - y).magnitude;
        return Mathf.Abs(triangleArea / hyp);
    }

    public static void GetPenetraionDepth(Collider2D lhs, Collider2D opposite, out float depth, out Vector2 deepestPoint, out Vector2 direction)
    {
        depth = 0;
        deepestPoint = Vector2.zero;
        direction = Vector2.zero;

        Vector2 pA, pB, pC, pD;
        float width = lhs.GetComponent<BoxCollider2D>().size.x * lhs.transform.lossyScale.x;
        float height = lhs.GetComponent<BoxCollider2D>().size.y * lhs.transform.lossyScale.y;

        pA = lhs.transform.position + lhs.transform.rotation * new Vector3(width / 2, height / 2);
        pB = lhs.transform.position + lhs.transform.rotation * new Vector3(-width / 2, height / 2);
        pC = lhs.transform.position + lhs.transform.rotation * new Vector3(-width / 2, -height / 2);
        pD = lhs.transform.position + lhs.transform.rotation * new Vector3(width / 2, -height / 2);

        Vector2 cA, cB, cC, cD;
        float oppositeWidth = opposite.GetComponent<BoxCollider2D>().size.x * opposite.transform.lossyScale.x;
        float oppositeHeight = opposite.GetComponent<BoxCollider2D>().size.y * opposite.transform.lossyScale.y;
        cA = opposite.transform.position + opposite.transform.rotation * new Vector3(oppositeWidth / 2, oppositeHeight / 2);
        cB = opposite.transform.position + opposite.transform.rotation * new Vector3(-oppositeWidth / 2, oppositeHeight / 2);
        cC = opposite.transform.position + opposite.transform.rotation * new Vector3(-oppositeWidth / 2, -oppositeHeight / 2);
        cD = opposite.transform.position + opposite.transform.rotation * new Vector3(oppositeWidth / 2, -oppositeHeight / 2);

        Vector2[] vertexArray = new Vector2[] { pA, pB, pC, pD };
        Vector2[] oppositeVertexArray = new Vector2[] { cA, cB, cC, cD, cA };
        Vector2[] Direction = new Vector2[] { new Vector2(0, 1), new Vector2(-1, 0), new Vector2(0, -1), new Vector2(1, 0) };
        foreach (Vector2 v in vertexArray)
        {
            if (Physics2D.RaycastAll(v, Vector2.zero).ToList().Exists(_ => _.collider == opposite))
            {
                float vdepth = Mathf.Infinity;
                Vector2 vdirection = new Vector2();
                for (int i = 0; i < 4; ++i)
                {
                    Vector2 cdirection = Direction[i];
                    float cdepth = triangleHeight(oppositeVertexArray[i], oppositeVertexArray[i + 1], v);
                    if (vdepth > cdepth)
                    {
                        vdepth = cdepth;
                        vdirection = cdirection;
                    }
                }

                if (depth < vdepth)
                {
                    depth = vdepth;
                    deepestPoint = v;
                    direction = opposite.transform.rotation * vdirection;
                }
            }
        }
    }
}
