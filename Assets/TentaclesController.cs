using System.Collections.Generic;
using UnityEngine;

public class TentaclesController : MonoBehaviour
{
    public GameObject tentaclePrefab;
    public Transform bodyTransform;
    public int tentaclesCount;
    List<Tentacle> tentacles;
    List<Vector3> offsets;
    float[] nextTimesToMove;
    float nextTimeCanMoveLeg;
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

    float height;

    private void Start()
    {
        height = bodyTransform.position.y;
        floorNormal = Vector3.up;
        tentacles = new List<Tentacle>();
        offsets = new List<Vector3>();
        nextTimesToMove = new float[tentaclesCount];
        for (int i = 0; i < tentaclesCount; i++)
        {
            GameObject go = Instantiate(tentaclePrefab);
            Tentacle tentacle = go.GetComponent<Tentacle>();
            tentacle.body = bodyTransform;
            tentacle.controller = this;
            Vector2 random = Random.insideUnitCircle * 2;
            tentacle.tipTarget = new Vector3(transform.position.x, 0, transform.position.z) + new Vector3(random.x, 0, random.y);
            offsets.Add(tentacle.tipTarget);
            tentacles.Add(tentacle);
        }
    }

    private void Update()
    {
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

            
            for (int i = 0; i < tentacles.Count; i++)
            {
                bool isForwardLeg = IsLegForward(i, moveDirection);
                float distance = (tentacles[i].tipTarget - moveDirection * 2f - transform.position).magnitude;
                if ((nextTimesToMove[i] < Time.time && nextTimeCanMoveLeg < Time.time && isForwardLeg) ||
                    (distance > 4 && nextTimeCanMoveLeg < Time.time) || distance > 5)
                {
                    nextLegIndex = (i + 1) % tentacles.Count;

                    Vector3 dir;
                    if (isForwardLeg)
                        dir = (offsets[i] + moveDirection * 2f - height * floorNormal).normalized;
                    else
                        dir = (offsets[i] + moveDirection * 0.8f - height * floorNormal).normalized;

                    if (Physics.Raycast(transform.position  + height * floorNormal, dir, out hit, 5))
                    {
                        Vector3 target = hit.point;
                        if ((target - tentacles[i].tipTarget).magnitude > 0)
                        {
                            nextTimesToMove[i] = Time.time + 0.5f;
                            //if (isForwardLeg)
                            nextTimeCanMoveLeg = Time.time + 0.5f;
                            tentacles[i].SetTipTarget(target, isForwardLeg);
                        }
                    }
                    else
                    {
                        nextTimesToMove[i] = Time.time + 0.2f;
                        if (isForwardLeg)
                            nextTimeCanMoveLeg = Time.time + 0.5f;
                        tentacles[i].SetTipTarget(transform.position + dir * 3 + floorNormal * 1f, isForwardLeg);
                    }
                }

            }
            /*
            
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
                    
                    if (Physics.Raycast(transform.position + height * floorNormal, dir, out hit, 5))
                    {
                        Vector3 target = hit.point;
                        tentacles[i].SetTipTarget(target);
                    }
                    else
                    {
                        tentacles[i].SetTipTarget(transform.position + dir * 3 + floorNormal * 1f);
                    }
                }

            }
            */
            
        }
    }

    bool IsLegForward(int ind, Vector3 dir)
    {
        float proj = Vector3.Dot((tentacles[ind].tipTarget - transform.position).normalized, dir);
        //float proj = Vector3.Dot(offsets[ind], dir);
        return proj > -0.2f;
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
