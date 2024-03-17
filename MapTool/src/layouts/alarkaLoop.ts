

import { Graph, Id, createNode, createSegment, createSwitch, getNode, getSegment } from '../lib/index.js'


export default async function alarkaLoop(graph: Graph) {
  const zone = `AN_Alarka_Loop`
  const { nid, sid, pid } = Graph.Shared.pushIdGenerator('Alarka_Loop')

  const start = getNode(Id('N9mx'))
  // start.rotation.y = 159
  // start.flipSwitchStand = false

  const groupId = '' //`${zone}`

  const begin1 = getNode(Id('Nt99'))
  begin1.rotation.x = 0

  const segLength = 60
  const segAng = -20

  const n1 = getNode(Id('Ne2z'))
  const n2 = getNode(Id('Nhph'))
  const mp1 = n2.position.clone()
    .sub(n1.position)
    .multiplyScalar(0.9)
    .add(n1.position)
  const mls1 = getSegment(Id('Sk7x'))
  const n3 = createNode(mp1, n1.rotation.clone())
  // n3.offset(10)
  mls1.endId = n3.id
  const mls2 = createSegment(n2.id, n3.id)

  createSwitch(n3.id, 35, 180+5, 0, groupId, true)
    .node.position.y += 0.10

  // const sw1 = createSwitch(Id('Nt99'), 50, 0, 8, groupId, false)
  // sw1.node.flipSwitchStand = true
  // sw1.node
  //   .extend(nid(), sid(), segLength, segAng, segAng, groupId)
  //   .extend(nid(), sid(), segLength, segAng, segAng, groupId)
  //   .extend(nid(), sid(), segLength, segAng, segAng, groupId)
  //   .extend(nid(), sid(), segLength, segAng, segAng, groupId)


  // const ext1 = getNode(Id('Noac'))
  //   .extend(nid(), sid(), 100, 20, 20, groupId)
  //   .extend(nid(), sid(), 500, 60, 60, groupId)
  //   .extend(nid(), sid(), 500, 60, 60, groupId)
  
  // getSegment(sid.last()).style = SegmentStyle.Tunnel

    // ext1.position.y += 2


  // const node1 = createNode(new Vector3(4281, 646.336, -3211.92), new Euler(0.5729387, 311.825165, -1.33408813E-08))
  // // node1.rotation.y = 311.825165
  // const node2 = createNode(new Vector3(4260.89, 646.099, -3193.92), new Euler(0.5729387, 311.825165, -1.33408813E-08))
  // const node3 = createNode(new Vector3(4287.71, 646.444, -3223.74), new Euler(0.640295, 325.825, 0.022371))
  // const node4 = createNode(new Vector3(4030.19, 645, -3178.91), new Euler(0, 159, 0))
  // const node5 = createNode(new Vector3(4072.61, 645, -3290.36), new Euler(0, 159, 0))
  // const node6 = createNode(new Vector3(4237.1, 648, -3378.63), new Euler(0, 247.946, 0))

  // const seg1 = createSegment(Id('N9mx'), node4.id)
  // const seg2 = createSegment(Id('Ne2z'), node1.id)
  // const seg3 = createSegment(Id('Nhph'), node2.id)
  // const seg4 = createSegment(node1.id, node2.id)
  // const seg5 = createSegment(node2.id, node3.id)
  // // const seg6 = createSegment(node4.id, Id('Nt99'))
  // const seg7 = createSegment(node4.id, node5.id)
  // const seg8 = createSegment(node5.id, node6.id)
  // const seg9 = createSegment(node6.id, node3.id)
  const json = {
    "nodes": {
      "N9mx": {
        "rotation": { "x": 0.0, "y": 159, "z": 0.0 },
        "flipSwitchStand": false
      },
      "AL_balloon_001": {
        "position": { "x": 4281, "y": 646.336, "z": -3211.92 },
        "rotation": { "x": 0.5729387, "y": 311.825165, "z": -1.33408813E-08 },
        "flipSwitchStand": true
      },
      "AL_balloon_002": {
        "position": { "x": 4260.89, "y": 646.099, "z": -3193.92 },
        "rotation": { "x": 0.5729387, "y": 311.825165, "z": -1.33408813E-08 },
        "flipSwitchStand": true
      },
      "AL_balloon_003": {
        "position": { "x": 4287.71, "y": 646.444, "z": -3223.74 },
        "rotation": { "x": 0.640295, "y": 325.825, "z": 0.022371 },
        "flipSwitchStand": true
      },
      "AL_balloon_004": {
        "position": { "x": 4030.19, "y": 645, "z": -3178.91 },
        "rotation": { "x": 0, "y": 159, "z": 0 },
        "flipSwitchStand": true
      },
      "AL_balloon_005": {
        "position": { "x": 4072.61, "y": 645, "z": -3290.36 },
        "rotation": { "x": 0, "y": 159, "z": 0 },
        "flipSwitchStand": true
      },
      "AL_balloon_006": {
        "position": { "x": 4237.1, "y": 648, "z": -3378.63 },
        "rotation": { "x": 0, "y": 247.946, "z": 0 },
        "flipSwitchStand": true
      }
    },
    "segments": {
      "Sehz": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "N9mx",
        "endId": "AL_balloon_004",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "Sk7x": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "Ne2z",
        "endId": "AL_balloon_001",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_nhph_b002": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "Nhph",
        "endId": "AL_balloon_002",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_b001_b002": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "AL_balloon_001",
        "endId": "AL_balloon_002",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_b002_b003": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "AL_balloon_002",
        "endId": "AL_balloon_003",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_b004_nt99": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "AL_balloon_004",
        "endId": "Nt99",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_b004_b005": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "AL_balloon_004",
        "endId": "AL_balloon_005",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_b005_b006": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "AL_balloon_005",
        "endId": "AL_balloon_006",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      },
      "NAN_b006_b003": {
        "style": "Standard",
        "trackClass": "Mainline",
        "startId": "AL_balloon_006",
        "endId": "AL_balloon_003",
        "priority": 0,
        "speedLimit": 0,
        "groupId": ""
      }
    }
  }

  return {
    name: 'Alarka Loop',
    mixins: {
      // whistles: [
      //   {
      //     "name": "Thomas Whistle 2",
      //     "clip": "file(thomas-whistle.ogg)",
      //     "model": {
      //       "assetPackIdentifier": "ttte",
      //       "assetIdentifier": "mdl-thomas-whistle"
      //     }
      //   }
      // ]
    }
  }
}
