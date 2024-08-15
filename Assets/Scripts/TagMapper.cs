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

    /// <summary>
    /// Puts tag ID into id argument and returns true if the tag is new.
    /// </summary>
    /// <param name="macAddress"></param>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool GetTagIdFromIdentifier(string macAddress, out int id)
    {
        if (tagMap.TryGetValue(macAddress, out id))
        {
            
            return false;
        }
        else
        {
            tagMap.Add(macAddress, nextId);
            nextId++;
            return true;
        }
    }
}
