using UnityEngine;

namespace AlinasRailTools.Scripting.Turntables;

/// <summary>
/// MonoBehaviour component that represents a roundhouse instance
/// This component contains only data and identification - build logic is in RoundhouseInstanceBuilder
/// </summary>
public class RoundhouseInstance : MonoBehaviour
{
  public string identifier;
  public int subdivisions;
  public int stalls;
  public int trackLength = 46; // Length of track from turntable edge to end of stall

  // Individual piece templates - will be assembled during Build()
  public GameObject stallTemplate;   // Vanilla stall piece
  public GameObject sideTemplate;    // Vanilla side wall piece
  public GameObject betweenTemplate; // Vanilla between connector piece

  public TurntableInstance turntable; // Optional reference to parent turntable
}
