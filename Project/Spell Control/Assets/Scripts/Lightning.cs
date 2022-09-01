using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
//<PLR>

public class Lightning : NetworkBehaviour {

    public GameObject lightningObj;

    int key = 0;
    IDictionary<int, GameObject> lightningDict;

    public Color c1 = Color.blue;
    public Color c2 = Color.white;

    private void Start()
    {
        lightningDict = new Dictionary<int, GameObject>();
    }

    [Command]
    void CmdSpawnLineRender(int key)
    {
        GameObject lightning = Instantiate(lightningObj, Vector3.zero, Quaternion.identity);
        lightningDict.Add(key, lightning);
        NetworkServer.Spawn(lightning);
    }

    [Command]
    void CmdUpdateLineRender(int id, Vector3[] points, int pointsCount)
    {
        RpcUpdateLineRender(lightningDict[id], points, pointsCount);
    }

    [ClientRpc]
    void RpcUpdateLineRender(GameObject light, Vector3[] points, int pointsCount)
    {
        light.GetComponent<LineRenderer>().positionCount = pointsCount;
        light.GetComponent<LineRenderer>().SetPositions(points);
    }


    [Command]
    void CmdDestroyLineRender(int id)
    {
        NetworkServer.Destroy(lightningDict[id]);
        lightningDict.Remove(id);
    }

    IEnumerator Draw(Vector3 startPoint, Vector3 endPoint, Vector3 hitTransPos, Vector3 direction, float length, int id)
    {
        List<Vector3> points = new List<Vector3>();
        List<Vector3> pointsOrigin = new List<Vector3>();

        float timeIntervals = 0.1f;
        for (float duration = 1f; duration >= 0; duration -= timeIntervals)
        {
            points.Clear();
            points.Add(startPoint);
            pointsOrigin.Add(startPoint); float distance = 0;
            int sign = -1;
            Vector3 slopeVector = new Vector3(direction.y, -direction.x, 0);
            float pointsSway;
            for (int i = 1; length - distance > 0.3f; i++)
            {
                pointsSway = Random.Range(0.1f, 0.2f);
                float distanceHop = Random.Range(0.05f, 0.2f);
                distance += distanceHop;
                Vector3 pointOnLine = pointsOrigin[i - 1] + (distanceHop * direction.normalized);
                Vector3 point = points[i - 1] + (distanceHop * direction.normalized) + (slopeVector.normalized * pointsSway);
                if (Vector3.Distance(point, pointOnLine) > 0.2f)
                {
                    slopeVector = slopeVector * sign;
                    point = points[i - 1] + (distanceHop * direction.normalized) + (slopeVector.normalized * pointsSway);
                }
                pointsOrigin.Add(pointOnLine);
                points.Add(point);
            }
            points.Add(endPoint);
            points.Add(hitTransPos);

            CmdUpdateLineRender(id, points.ToArray(),points.Count);

            yield return new WaitForSeconds(timeIntervals);
        }
        CmdDestroyLineRender(id);
    }

    public void DrawLightning(Vector3 startPoint, Vector3 endPoint, Vector3 hitTransPos, Vector3 direction, float length)
    {
        LineRenderer lineRenderer = lightningObj.GetComponent<LineRenderer>();
        lineRenderer.widthMultiplier = 0.03f;

        float alpha = 1.0f;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(c1, 0.0f), new GradientColorKey(c2, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(alpha, 0.0f), new GradientAlphaKey(alpha, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;


        key++;
        CmdSpawnLineRender(key);
        StartCoroutine(Draw(startPoint, endPoint, hitTransPos, direction, length, key));

    }

    void Update()
    {

    }
}
