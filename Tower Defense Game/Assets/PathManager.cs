using UnityEngine;

public class PathManager : MonoBehaviour
{
    public static PathManager Instance { get; private set; }
    
    private Transform[] waypoints;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        LoadWaypoints();
    }
    
    void LoadWaypoints()
    {
        // Find all GameObjects with tag "Waypoint" and sort by name
        GameObject[] waypointObjects = GameObject.FindGameObjectsWithTag("Waypoint");
        
        if (waypointObjects.Length == 0)
        {
            Debug.LogWarning("No waypoints found! Make sure to tag your waypoint GameObjects with 'Waypoint'");
            return;
        }
        
        // Sort waypoints by name (e.g., "Waypoint_0", "Waypoint_1", etc.)
        System.Array.Sort(waypointObjects, (a, b) => 
            string.Compare(a.name, b.name, System.StringComparison.Ordinal));
        
        // Extract transforms
        waypoints = new Transform[waypointObjects.Length];
        for (int i = 0; i < waypointObjects.Length; i++)
        {
            waypoints[i] = waypointObjects[i].transform;
        }
        
        Debug.Log($"PathManager loaded {waypoints.Length} waypoints");
    }
    
    public Transform[] GetWaypoints()
    {
        return waypoints;
    }
    
    public Transform GetWaypoint(int index)
    {
        if (index >= 0 && index < waypoints.Length)
        {
            return waypoints[index];
        }
        return null;
    }
    
    public int GetWaypointCount()
    {
        return waypoints != null ? waypoints.Length : 0;
    }
}