using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tentacle : MonoBehaviour
{
    public AnimationCurve yCurve;
    public float kneeCurvature = 1;
    public float kneeDistance = 1;
    public float smoothTime = 0.1f;
    [HideInInspector]
    public Transform body;
    [HideInInspector]
    public TentaclesController controller;
    public Vector3 tipTarget;
    Vector3 prevTipTarget;
    public List<Transform> referencePoints;
    public int pointsPerSegment = 10;
    LineRenderer lineRenderer;
    Vector3[] controlNodes;
    Vector3[] tangentControlNodes;
    Vector3[] points;
    Vector3 vel;

    Vector3 startPos;
    float t;

    bool isForward;



    private void Start()
    {
        controlNodes = new Vector3[3];
        tangentControlNodes = new Vector3[4];
        points = new Vector3[pointsPerSegment * 2 - 1];

        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = points.Length;
        prevTipTarget = tipTarget;
    }

    private void Update()
    {
        referencePoints[referencePoints.Count - 1].position = body.position;
        referencePoints[1].position = Vector3.Lerp(body.position, referencePoints[0].position, kneeDistance);
        LerpTip();
        CalculateNodesPositions();

        if (controller.mode == TentaclesController.Mode.Ebaka)
            CalculateSpline();
        else
            CalculateStraight();
        //lineRenderer.SetPositions(controlNodes);
        lineRenderer.SetPositions(points);
        lineRenderer.widthMultiplier = controller.mode == TentaclesController.Mode.Ebaka ? 0.25f : 0.15f;
    }

    private void OnDrawGizmos()
    {
        /*
        if (isForward)
            Gizmos.color = Color.red;
        else
            Gizmos.color = Color.green;
        Gizmos.DrawSphere(tipTarget, 0.1f);
        */
        Gizmos.color = Color.red;
        for (int i = 0; i < 4; i++)
        {
            Gizmos.DrawSphere(tangentControlNodes[i], 0.1f);
        }


    }

    public void SetTipTarget(Vector3 targ, bool moveAsForward)
    {
        prevTipTarget = tipTarget;
        tipTarget = targ;
        startPos = referencePoints[0].position;
        isForward = moveAsForward;
    }

    private void LerpTip()
    {
        t = 1 - (referencePoints[0].position - tipTarget).magnitude / (tipTarget - startPos).magnitude;
        referencePoints[0].position = Vector3.SmoothDamp(referencePoints[0].position, tipTarget, ref vel, smoothTime);
    }

    void CalculateNodesPositions()
    {

        float upMagnitude = controller.mode == TentaclesController.Mode.Ebaka ? 2 : 1;
        for (int i = 0; i < referencePoints.Count; i++)
        {
            switch (i)
            {
                case 0:
                    controlNodes[i] = referencePoints[i].position + controller.floorNormal * yCurve.Evaluate(t) * upMagnitude;
                    break;
                case 1:
                    if (controller.mode == TentaclesController.Mode.Ebaka)
                        controlNodes[i] = Vector3.Lerp(controlNodes[i],
                            referencePoints[i].position + controller.floorNormal * (yCurve.Evaluate(t) * upMagnitude - 0.0f), 0.05f);
                    else
                        controlNodes[i] = referencePoints[i].position + controller.floorNormal * (yCurve.Evaluate(t) * upMagnitude + 1);
                    break;
                case 2:
                    controlNodes[i] = referencePoints[i].position;
                    break;
            }  
        }

        Vector3 dir = (controlNodes[2] - controlNodes[0]).normalized;
        Vector3 tangent = (dir - Vector3.Dot(dir, controller.floorNormal) * controller.floorNormal).normalized;

        for (int i = 0; i < 4; i++)
        {
            switch (i)
            {
                case 0:
                    tangentControlNodes[i] = controlNodes[0] + controller.floorNormal;
                    break;
                case 1:
                    tangentControlNodes[i] = controlNodes[1] - tangent * kneeCurvature;
                    break;
                case 2:
                    tangentControlNodes[i] = controlNodes[1] + tangent * kneeCurvature;
                    break;
                case 3:
                    tangentControlNodes[i] = controlNodes[2];
                    break;

            }
        }
    }

    void CalculateSpline()
    {
        float t;
        for (int i = 0; i < pointsPerSegment; i++)
        {
            t = (float)i / (pointsPerSegment - 1);
            points[i] = CubicCurve(controlNodes[0], tangentControlNodes[0], tangentControlNodes[1], controlNodes[1], t);
        }

        for (int i = 0; i < pointsPerSegment; i++)
        {
            t = (float)i / (pointsPerSegment - 1);
            points[i + pointsPerSegment - 1] = CubicCurve(controlNodes[1], tangentControlNodes[2], tangentControlNodes[3], controlNodes[2], t);
        }
    }

    void CalculateStraight()
    {
        float t;
        for (int i = 0; i < pointsPerSegment; i++)
        {
            t = (float)i / (pointsPerSegment - 1);
            points[i] = Vector3.Lerp(controlNodes[0], controlNodes[1], t);
        }

        for (int i = 0; i < pointsPerSegment; i++)
        {
            t = (float)i / (pointsPerSegment - 1);
            points[i + pointsPerSegment - 1] = Vector3.Lerp(controlNodes[1], controlNodes[2], t);
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
}
