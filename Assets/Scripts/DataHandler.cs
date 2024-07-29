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
using System.Security.Cryptography.X509Certificates;
using TMPro;
using UnityEngine;

public class DataHandler : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI mLeftText;
    [SerializeField] TextMeshProUGUI mRightText;

    [SerializeField] float distanceBetweenTwoAnchors;
    [SerializeField] String leftAnchorShortName = "83";
    [SerializeField] String rightAnchorShortName = "84";

    private List<float> leftAnchorAVG = new List<float>();
    private List<float> rightAnchorAVG = new List<float>();
    private double[] anchor_ranges = new double[2];


    // for Rolling Average Filter:
    [SerializeField] int samplingDataSize = 5;

    // for Kalman Filter:
    // Don't know how to access initial position from player...will have to add later
    [SerializeField] bool useFilter = true;

    [SerializeField] float dt = 0.1f;
    [SerializeField] float processNoise = 0.1f;
    [SerializeField] float measurementNoise = 1.0f;

    // Kalman Filter
    private KalmanFilter kalmanFilter;

    private void Awake()
    {
        // REMEMBER TO ADD INTIAL LOCATION FIRST.
        // hmm, maybe I don't even need to technically, if the offset is just visual...

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


            // data comes from either left or right anchor, add to rolling average list
            if (data[0] == leftAnchorShortName)
            {
                leftAnchorAVG.Add(range);

                if (leftAnchorAVG.Count >= samplingDataSize)
                {
                    leftAnchorAVG.RemoveAt(0);
                    anchor_ranges[0] = leftAnchorAVG.Average();
                }
            }
            else
            {
                rightAnchorAVG.Add(range);

                if (rightAnchorAVG.Count >= samplingDataSize)
                {
                    rightAnchorAVG.RemoveAt(0);
                    anchor_ranges[1] = rightAnchorAVG.Average();
                }
            }


            // finally, compute the position of the player
            if (anchor_ranges[0] != 0.00f && anchor_ranges[1] != 0.00f)
            {
                mRightText.text = "Right Anchor\n" + RoundUp((float)anchor_ranges[0], 1) + " m";
                mLeftText.text = "Left Anchor\n" + RoundUp((float)anchor_ranges[1], 1) + " m";
                
                Vector2 newPosition = calcTag((float)anchor_ranges[0], (float)anchor_ranges[1], distanceBetweenTwoAnchors);
                var x = newPosition.x;
                var y = newPosition.y;

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