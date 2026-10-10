<script lang="ts">
    let { data } = $props();
</script>

{#if data.user}
    <h1>Your display name: {data.user.display_name}</h1>
    <h1>Your class: {data.user.class}</h1>
    <h1>Your color: {data.user.color}</h1>
{:else}
    <p>You are not logged in.</p>
{/if}

