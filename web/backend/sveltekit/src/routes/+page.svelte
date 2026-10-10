<script>
	import { goto } from '$app/navigation';
</script>

<h1>Welcome</h1>

<button onclick={() => goto('/login')}>
	Login
</button>

<button onclick={() => goto('/register')}>
	Register
</button>

<button onclick={() => goto('/game')}>
	Game
</button>

<button onclick={() => goto('/setting')}>
	Setting
</button>

<button onclick={() => goto('/project')}>
	Transcendence
</button>

<button onclick={() => goto('/team')}>
	Team
</button>