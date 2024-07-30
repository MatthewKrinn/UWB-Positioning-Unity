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
    
    public void movePlayer(double x, double y)
    {
        if (double.IsNaN(x))
            x = 0;
        if (double.IsNaN(y))
            y = 0;

        // Think I don't need to send over offset information to DataHandler, as Player class automatically takes it into account
        // rather than needing it for the filter. If theory is correct, already implemented correctly.
        transform.position = new Vector3((float)y * meterToSpace * -1 + x_offset, 0, (float)x * meterToSpace + z_offset);
    }
}