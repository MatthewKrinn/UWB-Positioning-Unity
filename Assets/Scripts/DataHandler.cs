/////////////////////////////////////////////////////////////////
/*
  ESP32 + UWB | Indoor Positioning + Unity Visualization
  For More Information: https://youtu.be/c8Pn7lS5Ppg
  Created by Eric N. (ThatProject)
*/
/////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class DataHandler : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI mLeftText;
    [SerializeField] TextMeshProUGUI mRightText;

    [SerializeField] float distanceBetweenTwoAnchors;
    [SerializeField] String leftAnchorShortName = "83";
    [SerializeField] String rightAnchorShortName = "84";



    private double[] anchor_ranges = new double[2];


    // for Rolling Average Filter:
    [SerializeField] bool useRollingFilter = true; 
    [SerializeField] int rollingFilterSize = 20;
    private Queue<float> xPositionRollingQueue = new Queue<float>();
    private Queue<float> yPositionRollingQueue = new Queue<float>();
    

    // for Kalman Filter:
    // Don't know how to access initial position from player...will have to add later
    [SerializeField] bool useKalmanFilter = true;

    [SerializeField] float dt = 0.1f;
    [SerializeField] float processNoise = 0.1f;
    [SerializeField] float measurementNoise = 1.0f;

    // Kalman Filter
    private KalmanFilter kalmanFilter;

    private void Awake()
    {
        // REMEMBER TO ADD INTIAL LOCATION FIRST.
        // hmm, maybe I don't even need to technically, if the offset is just visual
        // and taken into account on the Player side...

        kalmanFilter = new KalmanFilter(dt, processNoise, measurementNoise);
    }

    
    /*notes:

    seems like rawData is a string, data[0] is identifier of anchor, data[1] is numeric value

    */
    
    public void setData(string rawData)
    {
        string[] data = rawData.Split(',');

        if (data.Length == 2)
        {
            // range is data[1] if valid, else 0
            float range = float.TryParse(data[1], out range) ? range : 0;


            // data comes from either left or right anchor
            if (data[0] == leftAnchorShortName)
            {
                anchor_ranges[0] = range;
            }
            else
            {
                anchor_ranges[1] = range;
            }


            // finally, compute the position of the player.
            // Don't know if I want to add to rolling average queue in the sensor insertion area or down below, which is what I have now


            if (anchor_ranges[0] != 0.00f && anchor_ranges[1] != 0.00f)
            {
                mRightText.text = "Right Anchor\n" + RoundUp((float)anchor_ranges[0], 1) + " m";
                mLeftText.text = "Left Anchor\n" + RoundUp((float)anchor_ranges[1], 1) + " m";
                
                Vector2 nextPosition = calcTag((float)anchor_ranges[0], (float)anchor_ranges[1], distanceBetweenTwoAnchors);
                

                // This way, information is always added to the filter, and user can choose to display the filtered position or not
                Vector2 filteredPosition = kalmanFilter.UpdateFilter(nextPosition);
                if (useKalmanFilter)
                {
                    nextPosition = filteredPosition;
                }


                // saw that in original branch, data is not displayed until rolling average list COUNT > samplingSize
                // IDK if I want to copy that...
                if (useRollingFilter)
                {
                    // Changed from if to while in case user changes samplingDataSize in editor
                    while (xPositionRollingQueue.Count > rollingFilterSize)
                    {
                        xPositionRollingQueue.Dequeue();
                        yPositionRollingQueue.Dequeue();
                    }
                    

                    xPositionRollingQueue.Enqueue(nextPosition.x);
                    yPositionRollingQueue.Enqueue(nextPosition.y);

                    nextPosition = new Vector2(xPositionRollingQueue.Average(), yPositionRollingQueue.Average());
                }
                

                var x = nextPosition.x;
                var y = nextPosition.y;
                FindObjectOfType<Player>().movePlayer(RoundUp(x, 2), RoundUp(y, 2));
            }
        }
    }

    //Using the algorithm from Makerfabs
    //https://www.makerfabs.cc/article/esp32-uwb-indoor-positioning-test.html
    private Vector2 calcTag(float a, float b, float c)
    {
        float cos_a = (b * b + c * c - a * a) / (2 * b * c);
        float x = b * cos_a;
        float y = b * Mathf.Sqrt(1 - cos_a * cos_a);

        return new Vector2(x, y);
    }

    static double RoundUp(float input, int places)
    {
        double multiplier = Math.Pow(10, Convert.ToDouble(places));
        return Math.Ceiling(input * multiplier) / multiplier;
    }
}