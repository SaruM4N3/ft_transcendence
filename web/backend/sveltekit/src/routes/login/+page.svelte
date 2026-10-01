<script>
    let identifier = $state('');
    let password = $state('');

    let error = $state('');
    let success = $state('');

    async function login() {
        error = '';
        success = '';

        const response = await fetch('/api/login', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                identifier,
                password
            })
        });

        const data = await response.json();

        if (!response.ok) {
            error = data.error;
            return;
        }

        success = 'Login successful!';
    }
</script>

<h2>Login</h2>

<form onsubmit={(event) => {
    event.preventDefault();
    login();
}}>
    <label for="identifier">Email or display name:</label><br>
    <input
        type="text"
        id="identifier"
        bind:value={identifier}
        autocomplete="username"
        required
    ><br>

    <label for="password">Password:</label><br>
    <input
        type="password"
        id="password"
        bind:value={password}
        autocomplete="current-password"
        required
    ><br>

    <button type="submit">Login</button>
</form>

{#if error}
    <p>{error}</p>
{/if}

{#if success}
    <p>{success}</p>
{/if}

