<script lang="ts">
    import { goto } from '$app/navigation';

    let { data, children } = $props();

    async function logout() {
        const response = await fetch('/api/logout', {
            method: 'POST'
        });

        if (response.ok) {
            await goto('/login', { invalidateAll: true });
        }
    }
</script>

{#if data.user}
    <p>Welcome {data.user.display_name}</p>

    <button onclick={logout}>
        Logout
    </button>
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
{:else}
    <p>You are not logged in.</p>
{/if}

{@render children()}