<script lang="ts">
  import Markdown from '@magidoc/plugin-svelte-marked'
  import { Col, Container, Row } from '@sveltestrap/sveltestrap'
  import type { PageData } from './$types'
	export let data: PageData;
</script>
<div>
WIP, check back later.
</div>
Changelog:
<Container>
  {@const mod = data.mod}
  {#each mod.changelog as c}
  <Row class="changelog">
    <Col md="2">{ c.version }:</Col>
    <Col md="10" class="text-left">
      <Markdown source={ c.desc } />
    </Col>
  </Row>
  {/each}
</Container>