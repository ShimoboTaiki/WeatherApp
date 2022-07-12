using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class Weather
{
    public string publishingOffice;
    public DateTime reportDatetime;
    public string targetArea;
    public string headlineText;
    public string text;

    public string ToString()
    {
        return text.Replace("\n\n",Environment.NewLine);
    }
}
