/////////////////////////////////////////////////////////////////
/*
  ESP32 + UWB | Indoor Positioning + Unity Visualization
  For More Information: https://youtu.be/c8Pn7lS5Ppg
  Created by Eric N. (ThatProject)
  Adapted by Matthew K. (Booz Allen Hamilton)
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



    // private double[] anchor_ranges = new double[2];
    private List<List<double>> anchor_ranges_list = new List<List<double>>();


    // for Rolling Average Filter:
    [SerializeField] bool useRollingFilter = true; 
    [SerializeField] int rollingFilterSize = 20;

    // private Queue<float> xPositionRollingQueue = new Queue<float>();
    private List<Queue<float>> xPositionRollingQueueList = new List<Queue<float>>();


    // private Queue<float> yPositionRollingQueue = new Queue<float>();
    private List<Queue<float>> yPositionRollingQueueList = new List<Queue<float>>();
    

    // for Kalman Filter:
    // Don't know how to access initial position from player...will have to add later
    [SerializeField] bool useKalmanFilter = true;

    [SerializeField] float dt = 0.1f;
    [SerializeField] float processNoise = 0.1f;
    [SerializeField] float measurementNoise = 1.0f;

    // Kalman Filter List
    private List<KalmanFilter> kalmanFilterList = new List<KalmanFilter>();
        // Think I don't need to add intial position because it's taken
        // into account on the Player Visualization side, not the anchor side...
        // COULD BE WRONG, HOWEVER!!!


    private TagMapper tagMapper;

    private void Awake()
    {

        tagMapper = new TagMapper();

        // in multitag implementation, elements are initialized as new tags appear
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
        {
            dt = Mathf.Max(0, dt);
            processNoise = Mathf.Max(0, processNoise);
            measurementNoise = Mathf.Max(0, measurementNoise);

            rollingFilterSize = Mathf.Max(1, rollingFilterSize);
        }
    }

    
    /*notes:

    seems like rawData is a string, data[0] is identifier of anchor, data[1] is numeric value

    NEW FOR MULTITAG BRANCH:
    - Assume that rawData that comes from anchor now contains the tag identifier, split by comma, so data.Length should be 3 optimally
    - Assume that Tag Identifier is the first two numbers of MAC address or something, add a dictionary to map them to integers for easier processing
      - Handle tag dictionary for new tags and for obtaining identifier of old tags
    - Assume two anchors for simplicity, will build that functionality differently
    - Make different Kalman Filters for each player, and different rolling filters for each player
      - Tag ID's will be ints starting at 0 so you can index directly into each List of Filters

    */
    
    public void setData(string rawData)
    {
        string[] data = rawData.Split(',');

        // data: data[0] = range left anchor, data[1] = range right anchor, data[2] = tag identifier
        if (data.Length == 3)
        {
            // get tag identifier
            string macAddress = data[2];

            int tagId;
            bool isNew = tagMapper.GetTagIdFromIdentifier(macAddress, out tagId);

            // if new tag, add new filter, queues, and anchor range list
            if (isNew)
            {
                kalmanFilterList.Add(new KalmanFilter(dt, processNoise, measurementNoise));
                xPositionRollingQueueList.Add(new Queue<float>());
                yPositionRollingQueueList.Add(new Queue<float>());

                anchor_ranges_list.Add(new List<double>());
            }

            // tagId is the index into each list


            // range is data[1] if valid, else 0
            float range = float.TryParse(data[1], out range) ? range : 0;


            // data comes from either left or right anchor
            if (data[0] == leftAnchorShortName)
            {
                anchor_ranges_list[tagId][0] = range;
            }
            else
            {
                anchor_ranges_list[tagId][1] = range;
            }

            // finally, compute the position of the player.
            // Don't know if I want to add to rolling average queue in the sensor insertion area or down below, which is what I have now


            if (anchor_ranges_list[tagId][0] != 0.00f && anchor_ranges_list[tagId][1] != 0.00f)
            {
                mRightText.text = "Right Anchor\n" + RoundUp((float)anchor_ranges_list[tagId][0], 1) + " m";
                mLeftText.text = "Left Anchor\n" + RoundUp((float)anchor_ranges_list[tagId][1], 1) + " m";
                
                // next position is calculated only off of sensor readings, filterering is done after
                Vector2 nextPosition = calcTag((float)anchor_ranges_list[tagId][0], (float)anchor_ranges_list[tagId][1], distanceBetweenTwoAnchors);
                

                // This way, information is always added to the filter, and user can choose to display the filtered position or not
                Vector2 filteredPosition = kalmanFilterList[tagId].UpdateFilter(nextPosition);
                if (useKalmanFilter)
                {
                    nextPosition = filteredPosition;
                }



                // Changed from if to while in case user changes samplingDataSize in editor
                while (xPositionRollingQueueList[tagId].Count > rollingFilterSize)
                {
                    xPositionRollingQueueList[tagId].Dequeue();
                    yPositionRollingQueueList[tagId].Dequeue();
                }
                xPositionRollingQueueList[tagId].Enqueue(nextPosition.x);
                yPositionRollingQueueList[tagId].Enqueue(nextPosition.y);

                if (useRollingFilter)
                {
                    nextPosition = new Vector2(xPositionRollingQueueList[tagId].Average(), yPositionRollingQueueList[tagId].Average());
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