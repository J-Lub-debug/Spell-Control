using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


public class FieldOfView : NetworkBehaviour
{
    Vector2 PlayerPosition;
    Vector2 CursorPosition;
    Vector3 direction;

    public float angle;
    

    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacletMask;

    public List<Transform> visibleTargets;

    public float meshResolution;

    public MeshFilter viewMeshFilter;

    public GameObject vieshMeshFilterOnline;
    IDictionary<int, GameObject> aoeDict;
    int key = 1;

    Mesh viewMesh;
    Mesh viewMeshOnline;



    public void Awake()
    {
        viewMeshFilter = Instantiate(viewMeshFilter, Vector3.zero, Quaternion.identity);
        viewMeshFilter.transform.SetParent(transform.parent);
    }


    private void Start()
    {
        aoeDict = new Dictionary<int, GameObject>();



        viewMesh = new Mesh();
        viewMesh.name = "View Mesh";
        viewMeshFilter.mesh = viewMesh;
        DrawFileOfView();
        CmdSpawnAoe(key);

    }

    private void LateUpdate()
    {
        DrawFileOfView();
    }

    [Command]
    void CmdPetrify(GameObject gameObject)
    {
        RpcPetrify(gameObject);
    }

    [ClientRpc]
    void RpcPetrify(GameObject gameObject)
    {
        gameObject.GetComponent<PlayerStatus>().Petrification();
    }

    [Command]
    void CmdSpawnAoe(int key)
    {

        aoeDict = new Dictionary<int, GameObject>();
        GameObject meshOnline = Instantiate(vieshMeshFilterOnline, Vector3.zero, Quaternion.identity);
        aoeDict.Add(key, meshOnline);
        NetworkServer.Spawn(meshOnline);
    }

    [Command]
    void CmdShowAoe(Vector3[] vertices, int[] triangles, int key)
    {
        RpcShowAoe(vertices, triangles, aoeDict[key]);
    }

    [ClientRpc]
    void RpcShowAoe(Vector3[] vertices, int[] triangles, GameObject meshOnline)
    {
        viewMeshOnline = new Mesh();
        meshOnline.GetComponent<MeshFilter>().mesh = viewMeshOnline;
        viewMeshOnline.Clear();
        viewMeshOnline.vertices = vertices;
        viewMeshOnline.triangles = triangles;
        viewMeshOnline.RecalculateNormals();
    }

    [Command]
    void CmdClearAoe(int key)
    {
        RpcClearAoe(aoeDict[key]);
    }

    [ClientRpc]
    void RpcClearAoe(GameObject meshOnline)
    {
        viewMeshOnline = new Mesh();
        meshOnline.GetComponent<MeshFilter>().mesh = viewMeshOnline;
        viewMeshOnline.Clear();
    }

    IEnumerator ShowAoe(Vector3[] vertices, int[] triangles, int key)
    {
        float splitTimePostOverlap = 1f;
        float time = 0;

        while (time < splitTimePostOverlap)
        {
            time += Time.deltaTime;
            yield return new WaitForEndOfFrame();
            CmdShowAoe(viewMesh.vertices, viewMesh.triangles, key);
        }

        CmdClearAoe(key);
    }

    public void FindVisibleTargets()
    {
        StartCoroutine(ShowAoe(viewMesh.vertices, viewMesh.triangles, key));
        //CmdShowAoe(viewMesh.vertices, viewMesh.triangles, key);
        visibleTargets.Clear();
        List<Collider2D> targetsInViewRadius = new List<Collider2D>(Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y), viewRadius, targetMask));
        targetsInViewRadius.Remove(transform.GetComponent<Collider2D>());

        for (int i = 0; i < targetsInViewRadius.Count; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(direction, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics2D.Raycast(transform.position, dirToTarget, dstToTarget, obstacletMask))
                {
                    visibleTargets.Add(target);

                    CmdPetrify(target.gameObject);
                }
            }
        }
    }

    void DrawFileOfView()
    {
        int stepCount = Mathf.RoundToInt(viewAngle * meshResolution);
        float stepAngleSize = viewAngle / stepCount;
        List<Vector3> viewPoints = new List<Vector3>();
        for(int i = 0; i <= stepCount; i++)
        {
            DirFromAngle(viewAngle / 2, false);

            float currentAngle = angle - viewAngle / 2 + stepAngleSize * i;
            ViewCastInfo newViewCast = ViewCast(currentAngle);
            viewPoints.Add(newViewCast.point);
        }

        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];
        vertices[0] = transform.position;

        for(int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = viewPoints[i];

            if (i < vertexCount - 2)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }

        viewMesh.Clear();
        viewMesh.vertices = vertices;
        viewMesh.triangles = triangles;
        viewMesh.RecalculateNormals();
    }

    private void OnDisable()
    {
        viewMesh.Clear();
    }

    public struct ViewCastInfo
    {
        public bool hit;
        public Vector3 point;
        public float dst;
        public float angle;

        public ViewCastInfo(bool _hit, Vector3 _point, float _dst, float _angle)
        {
            hit = _hit;
            point = _point;
            dst = _dst;
            angle = _angle;
        }
    }

    ViewCastInfo ViewCast(float globalAngle)
    {
        Vector3 dir = DirFromAngle(globalAngle, true);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, viewRadius, obstacletMask);
        if(hit)
            return new ViewCastInfo(true, hit.point, hit.distance, globalAngle);
        else
        {
            return new ViewCastInfo(false, transform.position + dir * viewRadius, viewRadius, globalAngle);
        }
    }
    

    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)

        {
            PlayerPosition = transform.position;
            CursorPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            direction = (CursorPosition - PlayerPosition).normalized;


            angle = Vector3.Angle(direction, transform.up);
            if ((Vector3.Dot(direction, transform.right)) < 0)
            {
                angle = -angle;
            }
            angleInDegrees += angle;

        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }
}
