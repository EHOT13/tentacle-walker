using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour
{
    public AnimationCurve yCurve;
    public float smoothTime = 0.1f;
    public float upMagnitude = 1;
    [HideInInspector]
    public Transform body;
    [HideInInspector]
    public TentaclesController controller;
    public Vector3 tipTarget;
    Vector3 prevTipTarget;
    public List<Transform> controlNodes;
    public int segmentsCount;
    LineRenderer lineRenderer;
    Vector3[] points;
    int segmentPointsCount;
    Vector3 vel;

    Vector3 startPos;
    float t;



    private void Start()
    {
        segmentPointsCount = segmentsCount + 1;
        points = new Vector3[controlNodes.Count];
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points.Length;
        prevTipTarget = tipTarget;
    }

    private void Update()
    {
        controlNodes[controlNodes.Count - 1].position = body.position;
        controlNodes[1].position = Vector3.Lerp(body.position, controlNodes[0].position, 0.7f);
        LerpTip();
        CalculatePoints();
        lineRenderer.SetPositions(points);
    }

    public void SetTipTarget(Vector3 targ)
    {
        prevTipTarget = tipTarget;
        tipTarget = targ;
        startPos = controlNodes[0].position;
    }

    private void LerpTip()
    {
        t = 1 - (controlNodes[0].position - tipTarget).magnitude / (tipTarget - startPos).magnitude;
        controlNodes[0].position = Vector3.SmoothDamp(controlNodes[0].position, tipTarget, ref vel, smoothTime);
    }

    void CalculatePoints()
    {
        for (int i = 0; i < controlNodes.Count; i++)
        {
            switch (i)
            {
                case 2:
                    points[i] = controlNodes[i].position;
                    break;
                case 1:
                    points[i] = controlNodes[i].position + controller.floorNormal * (yCurve.Evaluate(t) * upMagnitude + 1);
                    break;
                case 0:
                    points[i] = controlNodes[i].position + controller.floorNormal * yCurve.Evaluate(t) * upMagnitude;
                    break;
                default:
                    break;
            }
            
            
        }
    }

    void CalculateSpline()
    {
        for (int i = 1; i < controlNodes.Count - 2; i++)
        {
            for (int j = 0; j < segmentPointsCount; j++)
            {
                points[(i - 1) * segmentPointsCount + j] = GetCatmullRomPosition(j / (float)(segmentPointsCount), controlNodes[i - 1].position,
                                                                                      controlNodes[i].position,
                                                                                      controlNodes[i + 1].position,
                                                                                      controlNodes[i + 2].position);
            }
        }
    }

    Vector3 QuadraticCurve(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        return Vector3.Lerp(Vector3.Lerp(p0, p1, t), Vector3.Lerp(p1, p2, t), t);
    }

    Vector3 CubicCurve(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        return Vector3.Lerp(QuadraticCurve(p0, p1, p2, t), QuadraticCurve(p1, p2, p3, t), t);
    }

    Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        //The coefficients of the cubic polynomial (except the 0.5f * which I added later for performance)
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        //The cubic polynomial: a + b * t + c * t^2 + d * t^3
        Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

        return pos;
    }
}
