<script lang="ts">
    import { goto } from '$app/navigation';

    let email = $state('');
    let display_name = $state('');
    let password = $state('');
    let error = $state('');
    let loading = $state(false);

    async function submit(event: SubmitEvent) {
        event.preventDefault();
        error = '';
        loading = true;

        try {
            const res = await fetch('/api/register', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email, display_name, password })
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
    <input type="email" bind:value={email} placeholder="Email"
           autocomplete="email" required />
    <input type="text" bind:value={display_name} placeholder="Display name"
           autocomplete="nickname" required />
    <input type="password" bind:value={password} placeholder="Password (8-128 characters)"
           autocomplete="new-password" required />

    {#if error}
        <p class="error" role="alert">{error}</p>
    {/if}

    <button type="submit" disabled={loading}>
        {loading ? 'Creating account...' : 'Sign up'}
    </button>
</form>