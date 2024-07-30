// Simple class to map a tag's MAC address to an integer ID. 
// This is used to identify tags in the system.


using System.Collections.Generic;
using UnityEngine;

public class TagMapper
{
    private Dictionary<string, int> tagMap;
    private int nextId = 0;

    public TagMapper()
    {
        tagMap = new Dictionary<string, int>();
    }

    public int GetTagIdFromIdentifier(string macAddress)
    {
        if (tagMap.TryGetValue(macAddress, out int id))
        {
            return id;
        }
        else
        {
            tagMap.Add(macAddress, nextId);
            return nextId++;
        }
    }
}
