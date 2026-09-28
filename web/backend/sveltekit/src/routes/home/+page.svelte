<script>
	import { goto } from '$app/navigation';
</script>

<h1>Main Page</h1>

<button onclick={() => goto('/game')}>
	Play Game
</button>