using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CustomPhysics
{
    public static void DebugVector2(IEnumerable<Vector2> a)
    {
        return;
        Debug.Log("Start Debug");
        foreach (Vector2 x in a)
            Debug.Log(String.Format("({0}, {1})", x.x, x.y));
        Debug.Log("End Debug");
    }
    public static void DebugVector2(Vector2 a)
    {
        Debug.Log(String.Format("Single: ({0}, {1})", a.x, a.y));
    }
    private static bool LineIntersection(Vector2 v1, Vector2 v2, Vector2 v3, Vector2 v4, out Vector2 res)
    {
        const float eps = 1e-3f;
        res = Vector2.zero;
        float x1, x2, x3, x4, y1, y2, y3, y4;
        x1 = v1.x; x2 = v2.x; x3 = v3.x; x4 = v4.x;
        y1 = v1.y; y2 = v2.y; y3 = v3.y; y4 = v4.y;
        float determinant = (y2 - y1) * (x4 - x3) - (y4 - y3) * (x2 - x1);
        if (Mathf.Abs(determinant) < eps) return false;
        float xcoord = ((x4 * y3 - x3 * y4) * (x2 - x1) - (x2 * y1 - x1 * y2) * (x4 - x3)) / determinant;
        float ycoord = -((y4 * x3 - y3 * x4) * (y2 - y1) - (y2 * x1 - y1 * x2) * (y4 - y3)) / determinant;
        res = new Vector2(xcoord, ycoord);
        bool ret =
            ((x1 < xcoord + eps && xcoord - eps < x2) || (x2 < xcoord + eps && xcoord - eps < x1)) &&
            ((x3 < xcoord + eps && xcoord - eps < x4) || (x4 < xcoord + eps && xcoord - eps < x3)) &&
            ((y1 < ycoord + eps && ycoord - eps < y2) || (y2 < ycoord + eps && ycoord - eps < y1)) &&
            ((y3 < ycoord + eps && ycoord - eps < y4) || (y4 < ycoord + eps && ycoord - eps < y3));
        if (!ret) res = Vector2.zero;
        return ret;
    }
    public static bool InsidePolygon(Vector2[] a, Vector2 p)
    {
        const float eps = 1e-9f;
        float integratedRotationField = 0.0f;
        for (int i = 0; i < a.Length; ++i)
        {
            Vector2 v1 = a[i];
            Vector2 v2 = a[(i + 1) % a.Length];
            float v2ang = Mathf.Atan2((v2 - p).y, (v2 - p).x);
            float v1ang = Mathf.Atan2((v1 - p).y, (v1 - p).x);
            float insangle = v2ang - v1ang;
            if (float.IsNaN(insangle)) return false;
            if (insangle > Mathf.PI) insangle -= Mathf.PI;
            if (insangle < -Mathf.PI) insangle += Mathf.PI;
            if (insangle < eps - Mathf.PI || insangle > Mathf.PI - eps) return false;
            integratedRotationField += insangle;
        }
        if (Mathf.Abs(integratedRotationField) < 1) return false;
        return true;
    }
    private static float triangleArea(Vector2 x, Vector2 y, Vector2 z)
    {
        x -= z;
        y -= z;
        float triangleArea = x.x * y.y - x.y * y.x;
        return triangleArea;
    }
    private static float triangleHeight(Vector2 x, Vector2 y, Vector2 z)
    {
        x -= z;
        y -= z;
        float triangleArea = x.x * y.y - x.y * y.x;
        float hyp = (x - y).magnitude;
        return Mathf.Abs(triangleArea / hyp);
    }
    public static Vector2 CalculateCoMofPolygon(List<Vector2> A)
    {
        if (A.Count == 0) return Vector2.zero;
        if (A.Count == 1) return A[0];
        if (A.Count == 2) return (A[0] + A[1]) / 2;
        List<Vector2> As = A.OrderBy(k => k.x).ToList();
        Vector2 pivot = As[As.Count - 1];
        As.RemoveAt(As.Count - 1);
        List<Vector2> Ass = As.OrderBy(k => Mathf.Atan2(k.y - pivot.y, k.x - pivot.x)).ToList();
        float totalArea = 0.0f;
        Vector2 totalV2 = Vector2.zero;
        
        for (int i=0; i<Ass.Count-1; ++i)
        {
            int j = i + 1;
            Vector2 a = Ass[i] - pivot;
            Vector2 b = Ass[j] - pivot;

            float Area = Mathf.Abs(a.x * b.y - a.y * b.x)/2;
            totalArea += Area;
            totalV2 += Area * (Ass[i] + Ass[j] + pivot) / 3;
        }
        if(totalArea < 1e-3)
        {
            Vector2 totalV = Vector2.zero;
            foreach (Vector2 v in A) totalV += v;
            return totalV / A.Count;
        }
        return totalV2 / totalArea;
        //return Vector2.zero;
    }
    public static Vector2 GetCollisionCoM(Vector2[] A, Vector2[] B)
    {
        List<Vector2> IntersectionPolygon = new List<Vector2>();

        for (int i = 0; i < A.Length; ++i)
        {
            for (int j = 0; j < B.Length; ++j)
            {
                Vector2 res;
                if (LineIntersection(A[i], A[(i + 1) % A.Length], B[j], B[(j + 1) % B.Length], out res))
                {
                    IntersectionPolygon.Add(res);
                }
            }
        }
        foreach (Vector2 b in B)
            if (InsidePolygon(A, b))
                IntersectionPolygon.Add(b);
        foreach (Vector2 a in A)
            if (InsidePolygon(B, a))
                IntersectionPolygon.Add(a);
        //TODO: idk i don't have idea because i have to implement wheter point is in polygon or not wtf!!!!!! 
        //Debug.Log(A[0].ToString() + A[1].ToString() + A[2].ToString() + A[3].ToString());
        //Debug.Log(B[0].ToString() + B[1].ToString() + B[2].ToString() + B[3].ToString());
        //Debug.Assert(IntersectionPolygon.Count != 0);
        DebugVector2(IntersectionPolygon);
        return CalculateCoMofPolygon(IntersectionPolygon);
    }
    public static void GetPenetrationDepth(Vector2[] A, Vector2[] B, out float depth, out Vector2 collisionCoM, out Vector2 direction)
    {
        depth = float.PositiveInfinity;
        collisionCoM = Vector2.zero;
        direction = Vector2.zero;

        Vector2[] possibleDirection = new Vector2[A.Length + B.Length];
        for (int i = 0; i < A.Length; ++i)
        {
            int j = (i + 1) % A.Length;
            possibleDirection[i] = A[j] - A[i];
        }
        for (int i = 0; i < B.Length; ++i)
        {
            int j = (i + 1) % B.Length;
            possibleDirection[i + A.Length] = B[j] - B[i];
        }
        foreach (Vector2 v in possibleDirection)
        {
            Debug.Assert(v.magnitude > 0.001);
            float minA = float.PositiveInfinity;
            float maxA = float.NegativeInfinity;
            float minB = float.PositiveInfinity;
            float maxB = float.NegativeInfinity;
            foreach (Vector2 a in A)
            {
                float value = Vector2.Dot(a, v);
                minA = Mathf.Min(minA, value);
                maxA = Mathf.Max(maxA, value);
            }
            foreach (Vector2 b in B)
            {
                float value = Vector2.Dot(b, v);
                minB = Mathf.Min(minB, value);
                maxB = Mathf.Max(maxB, value);
            }
            if (minA < minB)
            {
                if (maxB < maxA)
                {
                    //minA-minB-maxB-maxA order
                    //minA+maxB , minB+maxA 
                    if (maxB - minA < maxA - minB)
                    {
                        if (depth > (maxB - minA) / v.magnitude)
                        {
                            depth = (maxB - minA) / v.magnitude;
                            direction = v;
                        }
                        else if (depth > (maxA - minB) / v.magnitude)
                        {
                            depth = (maxA - minB) / v.magnitude;
                            direction = -v;
                        }
                    }
                }
                else
                {
                    //minA-minB-maxA-maxB order or minA-maxA-minB-maxB order
                    if (maxA < minB)
                    {
                        depth = 0;
                        direction = Vector2.zero;
                        //Debug.Log(string.Format("maxA: {0}, minB: {1}", maxA, minB));
                        return;
                    }
                    else
                    {
                        if (depth > (maxA - minB) / v.magnitude)
                        {
                            depth = (maxA - minB) / v.magnitude;
                            direction = -v;
                        }
                    }
                }
            }
            else
            {
                if (maxA < maxB)
                {
                    if (maxA - minB < maxB - minA)
                    {
                        if (depth > (maxA - minB) / v.magnitude)
                        {
                            depth = (maxA - minB) / v.magnitude;
                            direction = -v;
                        }
                        else if (depth > (maxB - minA) / v.magnitude)
                        {
                            depth = (maxB - minA) / v.magnitude;
                            direction = v;
                        }
                    }
                }
                else
                {
                    if (maxB < minA)
                    {
                        depth = 0;
                        direction = Vector2.zero;
                        return;
                    }
                    else
                    {
                        if (depth > (maxB - minA) / v.magnitude)
                        {
                            depth = (maxB - minA) / v.magnitude;
                            direction = v;
                        }
                    }
                }
            }
        }

        direction = direction.normalized;
        //direction = -direction;
        collisionCoM = GetCollisionCoM(A, B);
        //if(false){
        //   Debug.Log(A[0].ToString() + A[1].ToString() + A[2].ToString() + A[3].ToString());
        //  Debug.Log(B[0].ToString() + B[1].ToString() + B[2].ToString() + B[3].ToString());
        // Debug.Log(depth);
        // Debug.Log(collisionCoM.ToString());
        //  Debug.LogError(direction.ToString());
        //
        //       }
    }

    public static void GetPenetrationDepth(Collider2D lhs, Collider2D opposite, out float depth, out Vector2 deepestPoint, out Vector2 direction)
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
