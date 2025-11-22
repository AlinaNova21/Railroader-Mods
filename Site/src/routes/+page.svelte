<script lang="ts">
  import { Button, Col, Container, Row } from '@sveltestrap/sveltestrap'
  import type { PageData } from './$types'
	export let data: PageData;
</script>
<Container fluid>
  <h1 class="text-center">Alina's Railroader Mods:</h1>
  <div>All mods are installed by and require <a href="https://railroader.stelltis.ch/">Railloader</a> 1.8+.</div>
  <div>For AlinasMapMod, <a href="https://railroader.stelltis.ch/mods/strange-customs">StrangeCustoms</a> 1.6+ is required.</div>
  <Container md>
    <Row>
      <hr />
    </Row>
    {#each data.mods as mod}
    <Row>
      <Col>
        <h2>{ mod.name }</h2>
      </Col>
      <Col>{ mod.desc }</Col>
      <Col xs="auto">
        <div>Version: v{ mod.version }</div>
        <div>
          <Button href={`/mods/${mod.id}/changelog`} color="primary">Changelog</Button>
          <Button href={mod.downloadUrl} color="success">Download</Button>
        </div>
      </Col>
    </Row>
    <Row>
      <hr />
    </Row>
    {/each}
  </Container>
</Container>