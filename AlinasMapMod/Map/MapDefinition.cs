using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Character;
using Game.Progression;
using UnityEngine;

namespace AlinasMapMod.Map
{
  internal struct MapDefinition
  {
    public string Identifier { get; set; }
    public string MapName { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ProgressionId { get; set; }
    public int InitialMoney { get; set; }
    public Vector3 SpawnPosition { get; set; }
    public Vector3 SpawnRotation { get; set; }
    public List<SetupDescriptor.CarPlacement> CarPlacements { get; set; }
    public bool ShowTutorial { get; set; }
  }
}
