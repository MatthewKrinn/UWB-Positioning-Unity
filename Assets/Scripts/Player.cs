/////////////////////////////////////////////////////////////////
/*
  ESP32 + UWB | Indoor Positioning + Unity Visualization
  For More Information: https://youtu.be/c8Pn7lS5Ppg
  Created by Eric N. (ThatProject)
*/
/////////////////////////////////////////////////////////////////

using UnityEngine;

public class Player : MonoBehaviour
{
    // My Offsets
    [SerializeField] int meterToSpace = 20;
    [SerializeField] int x_offset = 20;
    [SerializeField] int z_offset = -20;

    [SerializeField] bool useFilter = true;

    [SerializeField] float dt = 0.1f;
    [SerializeField] float processNoise = 0.1f;
    [SerializeField] float measurementNoise = 1.0f;

    // Kalman Filter
    private KalmanFilter kalmanFilter;

    private void Awake()
    {
        kalmanFilter = new KalmanFilter(dt, processNoise, measurementNoise);
    }
    
    public void movePlayer(double x, double y)
    {
        if (double.IsNaN(x))
            x = 0;
        if (double.IsNaN(y))
            y = 0;

        if (useFilter)
        {
            var result = kalmanFilter.UpdateFilter(new Vector2((float)x, (float)y));
            
            x = result[0];
            y = result[1];
        }

        transform.position = new Vector3((float)y * meterToSpace * -1 + x_offset, 0, (float)x * meterToSpace + z_offset);
    }
}