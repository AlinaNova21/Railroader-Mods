<script lang="ts">
  import Markdown from '@magidoc/plugin-svelte-marked'
  import { Container, Col, Row, Card, CardGroup, CardHeader, CardTitle, CardBody, CardText, Button } from '@sveltestrap/sveltestrap'
  import type { PageData } from './$types';
	export let data: PageData;
</script>
{ JSON.stringify(data.mod) }