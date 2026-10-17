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

            await goto('/', { invalidateAll: true });
        } catch {
            error = 'Could not reach the server, please try again.';
        } finally {
            loading = false;
        }
    }
</script>

<section class="auth-card">
    <p class="eyebrow">WELCOME BACK</p>
    <h1>Log in</h1>
    <p class="description">Pick up where your last match left off.</p>

    <form onsubmit={submit}>
        <label for="identifier">Email or display name</label>
        <input id="identifier" type="text" bind:value={identifier} placeholder="Your email or name"
               autocomplete="username" required />
        <label for="password">Password</label>
        <input id="password" type="password" bind:value={password} placeholder="Your password"
               autocomplete="current-password" required />

        {#if error}
            <p class="error" role="alert">{error}</p>
        {/if}

        <button class="button" type="submit" disabled={loading}>
            {loading ? 'Logging in...' : 'Log in'}
        </button>
    </form>
    <p class="switch">New around here? <a href="/register">Create an account</a></p>
</section>

<style>
    .auth-card {
        width: min(100%, 440px);
        margin: 2rem auto;
        padding: clamp(1.5rem, 5vw, 2.5rem);
        border: 1px solid rgba(189, 204, 242, 0.13);
        border-radius: 1.25rem;
        background: linear-gradient(145deg, rgba(29, 37, 65, 0.92), rgba(19, 24, 43, 0.92));
        box-shadow: 0 24px 80px rgba(0, 0, 0, 0.24);
    }

    .eyebrow {
        margin: 0 0 0.65rem;
        color: #f5b85c;
        font-size: 0.72rem;
        font-weight: 800;
        letter-spacing: 0.15em;
    }

    h1 {
        margin: 0;
        font-size: clamp(2.2rem, 8vw, 3rem);
        letter-spacing: -0.05em;
    }

    .description, .switch {
        color: #aeb8d3;
        line-height: 1.6;
    }

    .description { margin: 0.55rem 0 1.8rem; }

    form {
        display: grid;
        gap: 0.7rem;
    }

    label {
        margin-top: 0.35rem;
        color: #d6def3;
        font-size: 0.88rem;
        font-weight: 650;
    }

    input {
        width: 100%;
        min-height: 3rem;
        padding: 0.8rem 0.9rem;
        border: 1px solid rgba(189, 204, 242, 0.18);
        border-radius: 0.65rem;
        outline: none;
        color: #f3f5ff;
        background: rgba(9, 13, 27, 0.55);
    }

    input:focus {
        border-color: #f5b85c;
        box-shadow: 0 0 0 3px rgba(245, 184, 92, 0.13);
    }

    input::placeholder { color: #737f9c; }

    form :global(.button) {
        width: 100%;
        margin-top: 0.6rem;
    }

    .error {
        color: #ff9eaa;
        font-size: 0.9rem;
        margin: 0.25rem 0;
    }

    .switch {
        margin: 1.5rem 0 0;
        text-align: center;
        font-size: 0.9rem;
    }

    .switch a {
        color: #f5b85c;
        text-decoration: none;
    }

    .switch a:hover { text-decoration: underline; }
</style>
