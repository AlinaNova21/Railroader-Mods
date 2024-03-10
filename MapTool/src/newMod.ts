import { mkdir, readFile, writeFile } from "fs/promises"

const [name] = process.argv.slice(4).filter(v => !v.startsWith('-'))

if (!name) {
  throw new Error('Name required')
}

const id = name.replace(/ /g, '')
console.log(id)
const mod = {
  name,
  id,
  files: {
    'Definition.json': {
      manifestVersion: 5,
      id: `AlinaNova21.${id}`,
      name,
      version: '1.0',
      requires: [
        'Zamu.StrangeCustoms'
      ],
      mixintos: {
        'game-graph': 'file(game-graph.json)'
      }
    },
    'game-graph.json': {
      nodes: {},
      segments: {},
    }
  }
}

await mkdir(`../${mod.id}`, { recursive: true })
for(const [name, file] of Object.entries(mod.files)) {
  await writeFile(`../${mod.id}/${name}`, typeof file == 'string' ? file : JSON.stringify(file, null, 2))
}
const workspaceFile = './AlinasSandbox.code-workspace'
const workspace = JSON.parse(await readFile(workspaceFile, 'utf8'))
if (!workspace.folders.find((f: { path: string }) => f.path == `../${mod.id}`)) {
  workspace.folders.push({ path: `../${mod.id}`})
  await writeFile(workspaceFile, JSON.stringify(workspace, null, 2))
}