<script lang="ts">
    import { goto } from '$app/navigation';

    let identifier = $state('');
    let password = $state('');
    let error = $state('');
    let loading = $state(false);

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        error = '';
        loading = true;

        try {
            const res = await fetch('/api/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ identifier, password })
            });

            if (res.status === 429) {
                error = 'Too many attempts, please wait a moment.';
                return;
            }

            const data = await res.json();

            if (!res.ok) {
                error = data.error ?? 'Something went wrong';
                return;
            }

            await goto('/game', { invalidateAll: true });
        } catch {
            error = 'Could not reach the server, please try again.';
        } finally {
            loading = false;
        }
    }
</script>

<form onsubmit={submit}>
    <input type="text" bind:value={identifier} placeholder="Email or display name"
           autocomplete="username" required />
    <input type="password" bind:value={password} placeholder="Password"
           autocomplete="current-password" required />

    {#if error}
        <p class="error" role="alert">{error}</p>
    {/if}

    <button type="submit" disabled={loading}>
        {loading ? 'Logging in...' : 'Log in'}
    </button>
</form>

<style>
    .error {
        color: #b00020;
        font-size: 0.9rem;
        margin: 0.5rem 0;
    }
</style>