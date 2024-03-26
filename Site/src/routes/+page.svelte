<script lang="ts">
  import { Button, Card, CardBody, CardGroup, CardHeader, CardText, CardTitle, Container } from '@sveltestrap/sveltestrap'
  import type { PageData } from './$types'
	export let data: PageData;
</script>
<Container fluid>
  <h1 class="text-center">Alina's Railroader Mods:</h1>
  <div>All mods are installed by and require <a href="https://railroader.stelltis.ch/">Railloader</a> 1.6+.</div>
  <div>For AlinasMapMod, <a href="https://railroader.stelltis.ch/mods/strange-customs">StrangeCustoms</a> 1.6+ (Beta) is required.</div>
  <Container fluid>
    <CardGroup>
      {#each data.mods as mod}
      <Card>
        <CardHeader>
          <CardTitle>{ mod.name }</CardTitle>
        </CardHeader>
        <CardBody>
          <CardText>
            { mod.desc }
          </CardText>

          <Button href={`/mods/${mod.id}`} color="primary">Details</Button>
          <Button href={mod.downloadUrl} color="success">Download</Button>
        </CardBody>
      </Card>
      <!-- <h3>Changelog</h3>
      <div class="container">
        {#each mod.changelog as c}
        <Row class="changelog">
          <Col md="2">{ c.version }:</Col>
          <Col md="10" text-left>
            <Markdown source={ c.desc } />
          </Col>
        </Row>
        {/each}
      </div> -->
      {/each}
    </CardGroup>
  </Container>
</Container>