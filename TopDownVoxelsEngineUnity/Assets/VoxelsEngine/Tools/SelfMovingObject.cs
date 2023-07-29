using UnityEngine;

public class SelfMovingObject : MonoBehaviour {
    public Vector3 Velocity;
    public Bounds Boundaries;

    void Update() {
        var t = transform;
        var p = t.position;
        p = new Vector3(p.x + Velocity.x * Time.deltaTime, p.y + Velocity.y * Time.deltaTime, p.z + Velocity.z * Time.deltaTime);
        var overX = p.x - Boundaries.max.x;
        if (overX > 0) p.x = Boundaries.min.x + overX;
        var overY = p.y - Boundaries.max.y;
        if (overY > 0) p.y = Boundaries.min.y + overY;
        var overZ = p.z - Boundaries.max.z;
        if (overZ > 0) p.z = Boundaries.min.z + overZ;
        var underX = p.x - Boundaries.min.x;
        if (underX < 0) p.x = Boundaries.max.x + underX;
        var underY = p.y - Boundaries.min.y;
        if (underY < 0) p.y = Boundaries.max.y + underY;
        var underZ = p.z - Boundaries.min.z;
        if (underZ < 0) p.z = Boundaries.max.z + underZ;
        t.position = p;
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = new Color(0.95f, 0.8f, 0.56f);
        Gizmos.DrawWireCube(Boundaries.center, Boundaries.size);
    }
}