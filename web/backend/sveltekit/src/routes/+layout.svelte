<script lang="ts">
	import favicon from '$lib/assets/favicon.svg';

	let { children, data } = $props();
</script>

<svelte:head>
	<link rel="icon" href={favicon} />
</svelte:head>

{#if data.user}
    <p>Welcome {data.user.display_name}</p>
{:else}
    <p>You are not logged in.</p>
{/if}

{@render children()}
