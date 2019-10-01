using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TentaclesController : MonoBehaviour
{
    public GameObject tentaclePrefab;
    public Transform bodyTransform;
    public int tentaclesCount;
    List<Tentacle> tentacles;
    List<float> offsets;
    Vector3 moveDirection;
    Vector3 normalToDirection;

    public Vector3 floorNormal;
    Vector3 onFloorPosition;
    Vector3 targetNormal;
    Quaternion normalRotation;

    float nextTimeCanMoveLeg;
    int nextLegIndex;

    float height;

    private void Start()
    {
        height = bodyTransform.position.y;
        floorNormal = Vector3.up;
        tentacles = new List<Tentacle>();
        offsets = new List<float>();
        for (int i = 0; i < tentaclesCount; i++)
        {
            GameObject go = Instantiate(tentaclePrefab);
            Tentacle tentacle = go.GetComponent<Tentacle>();
            tentacle.body = bodyTransform;
            tentacle.controller = this;
            Vector2 random = Random.insideUnitCircle * 2;
            tentacle.tipTarget = new Vector3(transform.position.x, 0, transform.position.z) + new Vector3(random.x, 0, random.y);
            offsets.Add(tentacle.tipTarget.z);
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

        moveDirection = normalRotation * moveDirection;
        bodyTransform.position = Vector3.Lerp(bodyTransform.position, transform.position + floorNormal * height, 0.2f);

        transform.position = onFloorPosition;

        if (moveDirection.magnitude > 0f)
        {
            transform.position += 5 * moveDirection * Time.deltaTime;

            moveDirection.Normalize();
            normalToDirection = Vector3.Cross(moveDirection, floorNormal); //new Vector3(-moveDirection.z, 0, moveDirection.x);


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
        }
        
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position, transform.position + floorNormal * 2);
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position, transform.position + moveDirection * 2);
        Gizmos.color = new Color(0, 0, 1, 0.3f);
        Gizmos.DrawSphere(moveDirection * 2 + transform.position, 4);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + normalToDirection * 2);
    }
}
