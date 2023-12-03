using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeUnit
{
    public static string getMinute(float second)
    {
        int value = 0;
        value = (int)Mathf.FloorToInt(second / 60);
        if(value < 10)
        {
            return "0" + value;
        } else
        return value.ToString();
    }

    public static string getRemainSecond(float second)
    {
        int value = 0;
        value = (int)(second % 60);

        if (value < 10)
        {
            return "0" + value;
        }
        else
            return value.ToString();
    }
    public static string getTimeUnit(float second)
    {
        return getMinute(second) + ":" + getRemainSecond(second);
    }
}
