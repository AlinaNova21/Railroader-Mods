import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

const scene = new THREE.Scene();
const camera = new THREE.PerspectiveCamera( 75, window.innerWidth / window.innerHeight, 0.1, 50000 );

const renderer = new THREE.WebGLRenderer();
renderer.setSize( window.innerWidth, window.innerHeight );
document.body.appendChild( renderer.domElement );

const geometry = new THREE.BoxGeometry( 1, 1, 1 );
const material = new THREE.MeshBasicMaterial( { color: 0x00ff00 } );
const cube = new THREE.Mesh( geometry, material );
scene.add( cube );

// camera.position.z = 5;
window.camera = camera

const controls = new OrbitControls( camera, renderer.domElement );
// controls.target.set( 0, 0, 0 )

function animate() {
	requestAnimationFrame( animate );
  cube.rotation.x += 0.01;
  cube.rotation.y += 0.01;
	renderer.render( scene, camera );
}
animate();

load()

async function load() {
  const container = new THREE.Group()
  scene.add(container)
  container.scale.z = -1

  const tracks = await fetch('tracks.json').then(req => req.json())
  const gameGraph = await fetch('game-graph.json').then(req => req.json())
  for (const [name,node] of Object.entries(gameGraph.nodes)) {
    const n = tracks.nodes[name] || {}
    const position = Object.assign(n.position || {}, node.position || {})
    const rotation = Object.assign(n.rotation || {}, node.rotation || {})
    Object.assign(n, node, { position, rotation })
    tracks.nodes[name] = n
    // if(exists) console.log(tracks.nodes[name])
  }
  for (const [name,segment] of Object.entries(gameGraph.segments)) {
    tracks.segments[name] = Object.assign(tracks.segments[name] || {}, segment)
  }
  
  const geometry = new THREE.BoxGeometry( 0.2, 0.2, 0.2 );
  const material = new THREE.MeshBasicMaterial( { color: 0x00ff00 } );
  for (const [name,node] of Object.entries(tracks.nodes)) {
    const cube = new THREE.Mesh( geometry, material );
    container.add( cube );
    cube.position.x = node.position.x
    cube.position.y = node.position.y
    cube.position.z = node.position.z
    
    cube.rotation.x = node.rotation.x || 0
    cube.rotation.y = node.rotation.y || 0
    cube.rotation.z = node.rotation.z || 0
  }
  const count = Object.keys(tracks.nodes).length
  console.log(count)
  camera.position.set(13100, 565.25, -4420)
  controls.target.set(13100, 561.25, -4440)
  // camera.position.x = Object.values(tracks.nodes).map(n => n.position.x).reduce((l,v) => l+v,0) / count
  // camera.position.y = Object.values(tracks.nodes).map(n => n.position.y).reduce((l,v) => l+v,0) / count
  // camera.position.z = Object.values(tracks.nodes).map(n => n.position.z).reduce((l,v) => l+v,0) / count

  camera.position.y += 5
  for (const id of Object.keys(tracks.segments)) {
    createSegment(tracks, id)
  }
  
function createSegment(graph, segmentId) {
  const segment = graph.segments[segmentId]
  
  const material = new THREE.LineBasicMaterial( { color: 0x0000ff } );
  const points = [];
  points.push( graph.nodes[segment.startId].position );
  points.push( graph.nodes[segment.endId].position );

  const geometry = new THREE.BufferGeometry().setFromPoints( points );
  const line = new THREE.Line( geometry, material );
  container.add( line );
}
}