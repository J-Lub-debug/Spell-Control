using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(FieldOfView))]
public class FieldOfViewEditor : Editor
{
    void OnSceneGUI()
    {
        
        FieldOfView fow = (FieldOfView)target;
        /*
        Handles.color = Color.red;
        Handles.DrawWireArc(fow.transform.position, Vector3.forward, Vector3.up, 360, fow.viewRadius);

        //Since it's not the global angle it should rotate with the character (if it would be a global angle [true] it wouldn't rotate)
        Vector3 viewAngleA = fow.DirFromAngle(-fow.viewAngle / 2, false);
        Vector3 viewAngleB = fow.DirFromAngle(fow.viewAngle / 2, false);

        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleA * fow.viewRadius);
        Handles.DrawLine(fow.transform.position, fow.transform.position + viewAngleB * fow.viewRadius);
        */
        
        //--------------------------------------------
        Handles.color = Color.green;

        //Detect visible target in cone test
        /*
        Handles.color = Color.green;
        foreach (Transform visibleTarget in fow.visibleTargets)
        {
            Handles.DrawLine(fow.transform.position, visibleTarget.position);
        }
        */

        Vector3 viewAngle = fow.DirFromAngle(fow.angle, true);

        //Lightning "real" position
        //>>> The line length is not the viewRadius but the viewRadius - startingPoint (Raycasting)
        Handles.DrawLine(fow.transform.position + viewAngle * 0.1f, fow.transform.position + viewAngle * fow.viewRadius);
        Vector3 normalAngle = new Vector3(viewAngle.y, -viewAngle.x, viewAngle.z);
        Handles.color = Color.blue;
        Handles.DrawLine(fow.transform.position + normalAngle * 0.1f, fow.transform.position + normalAngle * fow.viewRadius);
        //>>>
        //Debug.Log("LineStart: " + (fow.transform.position + viewAngle * 0.3f) + " LineLength: " + ((fow.transform.position + viewAngle * fow.viewRadius) - (fow.transform.position + viewAngle * 0.3f)) + " lineHitPosition: " + (fow.transform.position + viewAngle * fow.viewRadius));
    }
}
