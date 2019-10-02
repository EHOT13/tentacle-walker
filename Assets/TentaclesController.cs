using System.Collections.Generic;
using UnityEngine;

public class TentaclesController : MonoBehaviour
{
    bool useNewAlgoritm
    {
        get { return mode == Mode.Spider; }
    }
    
    public enum Mode { Spider, Ebaka }
    public Mode mode;

    public GameObject tentaclePrefab;
    public Transform bodyTransform;
    public int tentaclesCount;
    List<Tentacle> tentacles;
    List<Vector3> offsets;
    float[] randomCoefficients;
    Vector3 moveDirection;
    Vector3 normalToDirection;

    public float accel = 0.7f;
    private float velocity=0.0f;
    public float maxSpeed = 5.0f;

    public Vector3 floorNormal;
    Vector3 onFloorPosition;
    Vector3 targetNormal;
    Quaternion normalRotation;

    int nextLegIndex;
    float nextTimeCanMoveLeg;

    float height
    {
        get
        {
            if (mode == Mode.Ebaka)
                return 2.4f;
            else
                return 1.7f;
        }
    }

    private void Start()
    {
        floorNormal = Vector3.up;
        tentacles = new List<Tentacle>();
        offsets = new List<Vector3>();
        randomCoefficients = new float[tentaclesCount];
        for (int i = 0; i < tentaclesCount; i++)
        {
            GameObject go = Instantiate(tentaclePrefab);
            Tentacle tentacle = go.GetComponent<Tentacle>();
            tentacle.body = bodyTransform;
            tentacle.controller = this;
            Vector2 random = Random.insideUnitCircle * 2;
            tentacle.tipTarget = new Vector3(transform.position.x, 0, transform.position.z) + new Vector3(random.x, 0, random.y);
            offsets.Add(tentacle.tipTarget);
            randomCoefficients[i] = Mathf.Lerp(0.8f, 1.2f, (float)i / (tentaclesCount - 1));
            tentacles.Add(tentacle);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            mode = 1 - mode;
        }

        RaycastHit hit;

        if (Physics.SphereCast(transform.position + floorNormal, 0.1f, -transform.up, out hit))
        {
            targetNormal = hit.normal;
            onFloorPosition = hit.point;
            normalRotation = Quaternion.FromToRotation(Vector3.up, floorNormal);
            transform.rotation = normalRotation;
        }
        floorNormal = Vector3.Lerp(floorNormal, targetNormal, 0.05f);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        moveDirection = new Vector3(x, 0, z);

        //moveDirection = normalRotation * moveDirection;
        bodyTransform.position = Vector3.Lerp(bodyTransform.position, transform.position + floorNormal * height, 0.2f);

        transform.position = onFloorPosition;

        if (moveDirection.magnitude > 0f)
        {
            velocity += accel;
            if (velocity > maxSpeed)
                velocity = maxSpeed;
        }
        else if (velocity > 0)
        {
            velocity -= accel;
            if (velocity < 0)
                velocity = 0;
        }
        if (velocity > 0)
        {
            moveDirection *= velocity;
            moveDirection = normalRotation * moveDirection;
            moveDirection = Vector3.ClampMagnitude(moveDirection, velocity);
            moveDirection *= Time.deltaTime;
            transform.position += moveDirection;
            //transform.position += 5 * moveDirection * Time.deltaTime;

            moveDirection.Normalize();
            normalToDirection = Vector3.Cross(moveDirection, floorNormal);

            if (useNewAlgoritm)
            { 
            for (int i = 0; i < tentacles.Count; i++)
            {
                bool isForwardLeg = IsLegForward(i, moveDirection);
                float distance = (tentacles[i].tipTarget - moveDirection * 2f - transform.position).magnitude;
                if (isForwardLeg || distance > 4)
                {
                    nextLegIndex = (i + 1) % tentacles.Count;

                    Vector3 dir;
                    if (isForwardLeg)
                        dir = (moveDirection * 2.5f + normalRotation * offsets[i] - height * floorNormal).normalized;
                    else
                        dir = (moveDirection * 1f + normalRotation * offsets[i] - height * floorNormal).normalized;

                    Vector3 target;
                    if (Physics.Raycast(transform.position  + height * floorNormal, dir, out hit, 5))
                    {
                        target = hit.point;
                    }
                    else
                    {
                        target = transform.position + moveDirection * Random.Range(0.5f, 1.5f) + floorNormal * Random.Range(-1f, -0.5f) + normalRotation * offsets[i];
                    }
                    if ((target - tentacles[i].tipTarget).magnitude > 3 * randomCoefficients[i])
                    {
                        tentacles[i].SetTipTarget(target, isForwardLeg);
                    }
                }

            }
            }
            else
            
            for (int i = 0; i < tentacles.Count; i++)
            {

                float distance = (tentacles[i].tipTarget - moveDirection * 2 - transform.position).magnitude;
                if (distance > 4 &&
                    (nextTimeCanMoveLeg < Time.time || distance > 5))
                {
                    nextLegIndex = (i + 1) % tentacles.Count;
                    nextTimeCanMoveLeg = Time.time + 0.1f;
                    float proj = Vector3.Dot(tentacles[i].tipTarget - transform.position, normalToDirection);
                    proj = Mathf.Sign(proj) * Random.value * 2;
                    Vector3 dir = (moveDirection * 2.5f + proj * normalToDirection - height * floorNormal).normalized;
                    //Vector3 dir = (moveDirection * 2.5f + normalRotation * offsets[i] - height * floorNormal).normalized;

                    if (Physics.Raycast(transform.position + height * floorNormal, dir, out hit, 5))
                    {
                        Vector3 target = hit.point;
                        tentacles[i].SetTipTarget(target, false);
                    }
                    else
                    {
                        tentacles[i].SetTipTarget(transform.position + dir * 3 + floorNormal * 1f, false);
                    }
                }

            }
            

            
        }
    }

    bool IsLegForward(int ind, Vector3 dir)
    {
        //float proj = Vector3.Dot((tentacles[ind].tipTarget - transform.position).normalized, dir);
        float proj = Vector3.Dot(offsets[ind], dir);
        return proj > -0.1f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + floorNormal * 2);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + moveDirection * 2);
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        //Gizmos.DrawSphere(moveDirection * 2 + transform.position, 4);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + normalToDirection * 2);
    }
}
